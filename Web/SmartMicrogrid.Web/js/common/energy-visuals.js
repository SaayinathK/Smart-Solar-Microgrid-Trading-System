// Dependency-free SVG charts and one-shot animations for actual energy readings.
const EnergyVisuals = (() => {
  let sequence = 0;
  const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;'}[c]));
  const number = value => {
    if (value === null || value === undefined || (typeof value === 'string' && !value.trim()) || typeof value === 'boolean') return null;
    const result = Number(value);
    return Number.isFinite(result) && result >= 0 ? result : null;
  };
  const format = value => number(value) === null ? '—' : new Intl.NumberFormat(undefined, {maximumFractionDigits: 1}).format(Number(value));
  const percent = (part, total) => number(part) !== null && number(total) > 0 ? Number(part) / Number(total) * 100 : null;
  const clamp = value => Math.min(100, Math.max(0, Number(value) || 0));
  const sum = (rows, key) => rows.some(row => number(row[key]) === null) ? null : rows.reduce((total, row) => total + Number(row[key]), 0);
  const date = value => new Date(value).toLocaleString(undefined, {month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'});

  function gauge(value, label = 'Charge level', tone = 'green') {
    const valid = number(value) !== null;
    const text = valid ? `${format(value)}%` : '—';
    return `<div class="energy-gauge energy-tone-${tone}" role="img" aria-label="${escape(label)}: ${escape(valid ? text : 'not available')}">
      <svg viewBox="0 0 120 120" aria-hidden="true"><circle class="energy-ring-track" cx="60" cy="60" r="51"/>
        <circle class="energy-ring-fill" cx="60" cy="60" r="51" pathLength="100" stroke-dasharray="100" stroke-dashoffset="${100 - clamp(value)}" data-energy-ring="${100 - clamp(value)}"/></svg>
      <div class="energy-gauge-value"><span aria-hidden="true">ϟ</span><strong>${text}</strong><small>${escape(label)}</small></div>
    </div>`;
  }

  function battery(value, label = 'Stored energy') {
    const valid = number(value) !== null;
    const cells = Array.from({length: 12}, (_, index) => {
      const fill = Math.max(0, Math.min(1, clamp(value) / 100 * 12 - index));
      return `<span><i style="transform:scaleX(${fill})" data-energy-scale="${fill}"></i></span>`;
    }).join('');
    return `<div class="energy-battery" role="img" aria-label="${escape(label)}: ${valid ? escape(format(value)) + '%' : 'not available'}"><div class="energy-battery-cells">${cells}</div><span class="energy-battery-terminal" aria-hidden="true"></span></div>`;
  }

  function meter(value, label, tone = 'green') {
    return `<div class="energy-meter energy-tone-${tone}" role="img" aria-label="${escape(label)}: ${number(value) === null ? 'not available' : escape(format(value)) + '%'}"><span style="transform:scaleX(${clamp(value) / 100})" data-energy-scale="${clamp(value) / 100}"></span></div>`;
  }

  // Time is derived from delivery windows; values represent overlapping available kWh.
  function scheduleSeries(slots) {
    const events = new Map();
    let validCount = 0;
    for (const slot of slots) {
      const start = slot.startTime ? new Date(slot.startTime).getTime() : NaN;
      const end = slot.endTime ? new Date(slot.endTime).getTime() : NaN;
      const energy = number(slot.availableAmount);
      if (!Number.isFinite(start) || !Number.isFinite(end) || start >= end || energy === null) continue;
      validCount++;
      events.set(start, (events.get(start) || 0) + energy);
      events.set(end, (events.get(end) || 0) - energy);
    }
    let current = 0, peak = 0;
    const points = [];
    for (const [time, delta] of [...events].sort((a, b) => a[0] - b[0])) {
      points.push({time, value: current});
      current = Math.max(0, current + delta);
      points.push({time, value: current});
      peak = Math.max(peak, current);
    }
    return {points, peak, validCount, excludedCount: slots.length - validCount};
  }

  function scheduleChart(slots) {
    const {points, peak, validCount, excludedCount} = scheduleSeries(slots);
    if (!points.length) return empty('No delivery windows to chart', 'Supply graphs appear when offers with valid delivery windows are available.');
    const start = points[0].time, end = points[points.length - 1].time;
    const ceiling = peak || 1;
    const x = time => 44 + (time - start) / (end - start) * 576;
    const y = value => 188 - value / ceiling * 164;
    const line = points.map((point, index) => `${index ? 'L' : 'M'}${x(point.time).toFixed(2)},${y(point.value).toFixed(2)}`).join(' ');
    const id = `energy-area-${++sequence}`;
    const ticks = [0, .5, 1].map(fraction => `<line class="energy-chart-grid" x1="44" x2="620" y1="${y(ceiling * fraction)}" y2="${y(ceiling * fraction)}"/><text class="energy-chart-label" x="36" y="${y(ceiling * fraction) + 4}" text-anchor="end">${format(peak * fraction)}</text>`).join('');
    return `<div class="energy-schedule"><div class="energy-chart-highlight"><span>Peak scheduled supply</span><strong>${format(peak)} <small>kWh</small></strong></div>
      <svg class="energy-svg-chart" viewBox="0 0 640 205" role="img" aria-label="Available scheduled energy from ${escape(date(start))} to ${escape(date(end))}; peak ${format(peak)} kWh">
        <defs><linearGradient id="${id}" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#10b981" stop-opacity=".25"/><stop offset="1" stop-color="#10b981" stop-opacity=".02"/></linearGradient></defs>${ticks}
        <path d="${line} L620,188 L44,188 Z" fill="url(#${id})" data-energy-fade/>
        <path class="energy-schedule-line" d="${line}" fill="none" pathLength="1" stroke-dasharray="1" stroke-dashoffset="0" data-energy-line/>
      </svg><div class="energy-chart-axis"><span>${escape(date(start))}</span><span>${escape(date(end))}</span></div>
      <p class="energy-chart-note">Available kWh across overlapping delivery windows.${excludedCount ? ` ${validCount} of ${slots.length} offers have valid schedule data.` : ''}</p></div>`;
  }

  function comparisonChart(rows, segments, label, options = {}) {
    if (!rows.length) return empty('No readings to compare', 'Refresh after microgrid readings become available.');
    const selected = [...rows].sort((a, b) => (number(b.total) || 0) - (number(a.total) || 0)).slice(0, 6);
    const maximum = Math.max(1, ...selected.map(row => Math.max(number(row.total) || 0, row.values.reduce((n, value) => n + (number(value) || 0), 0))));
    const legend = `<div class="energy-chart-legend">${segments.map(s => `<span><i style="background:${s.color}"></i>${escape(s.label)}</span>`).join('')}</div>`;
    const note = `<p class="energy-chart-note">${rows.length > 6 ? `Largest 6 of ${rows.length} microgrids. ` : ''}Current readings in kWh.</p>`;
    if (options.compact) {
      const items = selected.map(row => {
        const name = String(row.name || 'Unnamed microgrid');
        const extent = Math.max(number(row.total) || 0, row.values.reduce((total, value) => total + (number(value) || 0), 0));
        const description = `${name}; total ${format(row.total)} kWh; ${segments.map((segment, i) => `${segment.label} ${format(row.values[i])} kWh`).join('; ')}`;
        return `<div class="energy-comparison-row" role="img" aria-label="${escape(description)}">
          <div class="energy-comparison-label"><strong title="${escape(name)}">${escape(name)}</strong><span>${format(row.total)} kWh</span></div>
          <div class="energy-comparison-track" style="width:${extent / maximum * 100}%">${row.values.map((value, index) => `<span style="width:${extent ? (number(value) || 0) / extent * 100 : 0}%;background:${segments[index].color}" data-energy-scale="1" title="${escape(segments[index].label)}: ${format(value)} kWh"></span>`).join('')}</div>
        </div>`;
      }).join('');
      return `<div class="energy-comparison-compact" role="group" aria-label="${escape(label)}">${items}</div>${legend}${note}`;
    }
    const svgRows = selected.map((row, index) => {
      const top = index * 58 + 6;
      let offset = 0;
      const bars = row.values.map((value, segment) => {
        const width = (number(value) || 0) / maximum * 600;
        const rect = `<rect x="${20 + offset}" y="${top + 26}" width="${width}" height="12" rx="3" fill="${segments[segment].color}" data-energy-bar="${width}"><title>${escape(segments[segment].label)}: ${format(value)} kWh</title></rect>`;
        offset += width;
        return rect;
      }).join('');
      const shortName = String(row.name || 'Unnamed microgrid');
      return `<g><title>${escape(shortName)}; ${segments.map((s, i) => `${escape(s.label)} ${format(row.values[i])} kWh`).join('; ')}</title>
        <text class="energy-chart-name" x="20" y="${top + 13}">${escape(shortName.length > 40 ? shortName.slice(0, 39) + '…' : shortName)}</text>
        <text class="energy-chart-label" x="620" y="${top + 13}" text-anchor="end">${format(row.total)} kWh</text>
        <rect class="energy-chart-track" x="20" y="${top + 26}" width="${(number(row.total) || 0) / maximum * 600}" height="12" rx="3"/>${bars}</g>`;
    }).join('');
    return `<svg class="energy-svg-chart" viewBox="0 0 640 ${selected.length * 58 + 8}" role="img" aria-label="${escape(label)}">${svgRows}</svg>
      ${legend}${note}`;
  }

  function empty(title, detail = '') {
    return `<div class="energy-empty"><strong>${escape(title)}</strong>${detail ? `<span>${escape(detail)}</span>` : ''}</div>`;
  }

  const active = new Set(), pending = new Set();
  const motion = typeof window !== 'undefined' && window.matchMedia ? window.matchMedia('(prefers-reduced-motion: reduce)') : {matches: true};
  function play(element) {
    if (motion.matches || !element.animate) return;
    let frames;
    if (element.hasAttribute('data-energy-ring')) frames = [{strokeDashoffset: 100}, {strokeDashoffset: Number(element.dataset.energyRing)}];
    else if (element.hasAttribute('data-energy-scale')) frames = [{transform: 'scaleX(0)'}, {transform: `scaleX(${Number(element.dataset.energyScale)})`}];
    else if (element.hasAttribute('data-energy-line')) frames = [{strokeDashoffset: 1}, {strokeDashoffset: 0}];
    else if (element.hasAttribute('data-energy-bar')) frames = [{width: '0px'}, {width: `${Number(element.dataset.energyBar)}px`}];
    else if (element.hasAttribute('data-energy-fade')) frames = [{opacity: 0}, {opacity: 1}];
    else frames = [{opacity: 0, transform: 'translateY(8px)'}, {opacity: 1, transform: 'translateY(0)'}];
    const animation = element.animate(frames, {duration: 850, easing: 'cubic-bezier(.2,.7,.2,1)'});
    active.add(animation);
    const clean = () => active.delete(animation);
    animation.onfinish = clean;
    animation.oncancel = clean;
  }
  const observer = typeof IntersectionObserver !== 'undefined' ? new IntersectionObserver(entries => {
    entries.filter(entry => entry.isIntersecting).forEach(entry => {observer.unobserve(entry.target); pending.delete(entry.target); play(entry.target);});
  }, {threshold: .12}) : null;
  function animate(container) {
    for (const old of pending) if (!old.isConnected) {observer?.unobserve(old); pending.delete(old);}
    container.querySelectorAll('[data-energy-ring], [data-energy-scale], [data-energy-line], [data-energy-bar], [data-energy-fade], [data-energy-enter]').forEach(element => {
      if (element.dataset.energyPrepared) return;
      element.dataset.energyPrepared = 'true';
      if (observer && !motion.matches) {pending.add(element); observer.observe(element);} else play(element);
    });
  }
  motion.addEventListener?.('change', () => {
    if (motion.matches) {active.forEach(animation => animation.cancel()); pending.forEach(element => observer?.unobserve(element)); pending.clear();}
  });
  return {escape, number, format, percent, clamp, sum, date, gauge, battery, meter, scheduleSeries, scheduleChart, comparisonChart, empty, animate};
})();
