// Battery and capacity pages share current microgrid readings and visual components.
document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth()) return;
  const kind = document.body.dataset.energyPage;
  if (!['battery', 'capacity'].includes(kind)) return;
  const V = EnergyVisuals;
  renderAppLayout(kind, kind === 'battery' ? 'Battery Storage' : 'Energy Capacity');
  const container = document.getElementById(kind === 'battery' ? 'battery-cards-container' : 'capacity-list-container');
  const summary = document.getElementById('energy-resource-summary');
  const chart = document.getElementById('energy-comparison-chart');
  const refresh = document.getElementById('refresh-energy');
  const status = document.getElementById('energy-refresh-status');
  const canUpdate = ['Admin', 'MicrogridOperator'].includes(SessionManager.getUserRole());
  let microgrids = [];

  const stat = (label, value, detail) => `<article class="energy-summary" data-energy-enter><span>${label}</span><strong>${value}</strong><small>${detail}</small></article>`;
  const row = (label, value) => `<div><dt>${label}</dt><dd>${value}</dd></div>`;
  const detailLink = m => getWebPageUrl(`microgrid-details.html?id=${encodeURIComponent(m.id || '')}`);
  const head = (m, badge) => `<header class="energy-node-head"><div><h3>${V.escape(m.name || 'Unnamed microgrid')}</h3><p>${V.escape(m.location || 'Location unavailable')}</p></div>${badge}</header>`;

  function batteryCard(m) {
    const capacity = V.number(m.batteryCapacity), level = V.number(m.currentBatteryLevel);
    const percentage = V.percent(level, capacity);
    const warning = percentage === null || percentage < 20 || percentage > 100;
    const label = percentage === null ? 'Not configured' : percentage > 100 ? 'Check reading' : percentage < 20 ? 'Low charge' : 'Stored energy';
    const updated = m.updatedAt && Number.isFinite(new Date(m.updatedAt).getTime()) ? V.date(m.updatedAt) : 'Not available';
    return `<article class="energy-node-card energy-battery-card" data-energy-enter>
      ${head(m, `<span class="energy-state-pill ${warning ? 'warning' : ''}">${label}</span>`)}
      <div class="energy-battery-overview">${V.gauge(percentage, 'Charge', warning ? 'amber' : 'green')}
        <div class="energy-battery-stored"><span>Stored energy</span><strong>${V.format(level)} <small>kWh</small></strong>
          ${V.battery(percentage)}
        </div></div>
      <dl class="energy-storage-metrics">${row('Storage capacity', `${V.format(capacity)} kWh`)}${row('Headroom', capacity !== null && level !== null ? `${V.format(Math.max(0, capacity - level))} kWh` : '—')}</dl>
      ${percentage > 100 ? '<p class="energy-data-note">Reported energy exceeds storage capacity.</p>' : ''}
      <footer class="energy-node-footer"><span>Updated ${V.escape(updated)}</span>
        ${canUpdate && capacity > 0 && level !== null ? `<button type="button" class="btn btn-secondary btn-sm" data-update-battery="${V.escape(m.id)}" aria-label="Update battery charge for ${V.escape(m.name || 'microgrid')}">Update charge</button>` : `<a class="btn btn-secondary btn-sm" href="${detailLink(m)}">View microgrid</a>`}
      </footer>
    </article>`;
  }

  function capacityCard(m) {
    const total = V.number(m.capacity);
    const values = [m.availableCapacity, m.reservedCapacity, m.usedCapacity].map(V.number);
    const known = values.every(value => value !== null);
    const allocated = known ? values[1] + values[2] : null;
    const colors = ['#10b981', '#f59e0b', '#6366f1'];
    const labels = ['Available', 'Reserved', 'Used'];
    const exceeds = total !== null && known && values.reduce((a, b) => a + b, 0) > total + .001;
    return `<article class="energy-node-card energy-capacity-card" data-energy-enter>
      ${head(m, `<a class="btn btn-secondary btn-sm" href="${detailLink(m)}">View details</a>`)}
      <div class="energy-capacity-overview"><div class="energy-capacity-total"><span>Total capacity</span><strong>${V.format(total)} kWh</strong></div>${V.gauge(V.percent(allocated, total), 'Allocated', 'blue')}</div>
      <div class="energy-capacity-stack" role="img" aria-label="${labels.map((label, i) => `${label} ${V.format(values[i])} kWh`).join(', ')}">${values.map((value, i) => `<span style="width:${V.clamp(V.percent(value, total))}%;background:${colors[i]}" data-energy-scale="1"></span>`).join('')}</div>
      <dl class="energy-capacity-breakdown">${labels.map((label, i) => `<div><dt>${label}</dt><dd>${V.format(values[i])} kWh</dd></div>`).join('')}</dl>
      ${exceeds ? '<p class="energy-data-note">Reported allocations exceed the total capacity.</p>' : ''}
    </article>`;
  }

  function render(list) {
    document.getElementById('energy-node-count').textContent = `${list.length} ${list.length === 1 ? 'microgrid' : 'microgrids'}`;
    if (kind === 'battery') {
      const stored = V.sum(list, 'currentBatteryLevel'), total = V.sum(list, 'batteryCapacity');
      const charge = V.percent(stored, total);
      const low = list.filter(m => V.percent(m.currentBatteryLevel, m.batteryCapacity) !== null && V.percent(m.currentBatteryLevel, m.batteryCapacity) < 20).length;
      summary.innerHTML = stat('Stored energy', `${V.format(stored)} kWh`, 'Current battery reserves') + stat('Storage capacity', `${V.format(total)} kWh`, `${list.length} microgrid systems`) + stat('Fleet charge', charge === null ? '—' : `${V.format(charge)}%`, 'Weighted by storage capacity') + stat('Low-charge systems', V.format(low), 'Below 20% of storage capacity');
      const rows = list.filter(m => V.number(m.batteryCapacity) > 0 && V.number(m.currentBatteryLevel) !== null && Number(m.currentBatteryLevel) <= Number(m.batteryCapacity)).map(m => ({name: m.name, total: Number(m.batteryCapacity), values: [Number(m.currentBatteryLevel), Number(m.batteryCapacity) - Number(m.currentBatteryLevel)]}));
      chart.innerHTML = V.comparisonChart(rows, [{label: 'Stored energy', color: '#10b981'}, {label: 'Storage headroom', color: '#bfdbfe'}], 'Stored battery energy and remaining storage by microgrid', {compact: true});
      if (rows.length < list.length) chart.insertAdjacentHTML('beforeend', '<p class="energy-chart-note">Systems without valid configured storage are excluded from this comparison.</p>');
      container.innerHTML = list.length ? list.map(batteryCard).join('') : V.empty('No battery systems registered', 'Battery cards appear when microgrids have storage readings.');
    } else {
      summary.innerHTML = stat('Total capacity', `${V.format(V.sum(list, 'capacity'))} kWh`, 'Reported generation capacity') + stat('Available', `${V.format(V.sum(list, 'availableCapacity'))} kWh`, 'Ready to allocate') + stat('Reserved', `${V.format(V.sum(list, 'reservedCapacity'))} kWh`, 'Committed to reservations') + stat('Used', `${V.format(V.sum(list, 'usedCapacity'))} kWh`, 'Reported used capacity');
      chart.innerHTML = V.comparisonChart(list.map(m => ({name: m.name, total: m.capacity, values: [m.availableCapacity, m.reservedCapacity, m.usedCapacity]})), [{label: 'Available', color: '#10b981'}, {label: 'Reserved', color: '#f59e0b'}, {label: 'Used', color: '#6366f1'}], 'Current available, reserved and used capacity by microgrid', {compact: true});
      container.innerHTML = list.length ? list.map(capacityCard).join('') : V.empty('No microgrids registered', 'Capacity charts appear when generation resources are available.');
    }
    V.animate(document.querySelector('.main-content'));
  }

  async function load() {
    refresh.disabled = true;
    refresh.textContent = 'Refreshing...';
    status.textContent = 'Refreshing current readings...';
    container.setAttribute('aria-busy', 'true');
    try {
      const response = await MicrogridApi.getMicrogrids();
      if (!response?.success || !Array.isArray(response.data)) throw new Error(response?.message || 'Readings are unavailable.');
      microgrids = response.data;
      render(microgrids);
      status.textContent = `Readings refreshed at ${new Date().toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'})}.`;
    } catch (error) {
      document.getElementById('energy-node-count').textContent = 'Readings unavailable';
      summary.innerHTML = '';
      chart.innerHTML = V.empty('Chart data unavailable', 'Refresh to retry the current readings.');
      container.innerHTML = V.empty(`Could not load ${kind === 'battery' ? 'battery' : 'capacity'} readings`, error.message || 'Please try again.');
      status.textContent = 'Current readings could not be refreshed.';
    } finally {
      refresh.disabled = false;
      refresh.textContent = 'Refresh readings';
      container.setAttribute('aria-busy', 'false');
    }
  }

  container.addEventListener('click', async event => {
    const button = event.target.closest('[data-update-battery]');
    if (!button || !canUpdate || kind !== 'battery') return;
    const m = microgrids.find(row => String(row.id) === button.dataset.updateBattery);
    if (!m) return;
    const input = prompt(`Enter a battery charge level between 0 and ${m.batteryCapacity} kWh:`, m.currentBatteryLevel);
    if (input === null) return;
    const level = input.trim() ? V.number(input) : null;
    if (level === null || level > Number(m.batteryCapacity)) {ApiClient.showToast('Enter a valid charge level within the battery capacity.', 'error'); return;}
    button.disabled = true;
    try {
      const response = await MicrogridApi.updateBattery(m.id, {batteryCapacity: m.batteryCapacity, currentBatteryLevel: level});
      if (!response?.success) throw new Error(response?.message || 'Battery update failed.');
      ApiClient.showToast('Battery charge level updated.', 'success');
      await load();
    } catch (error) {ApiClient.showToast(error.message || 'Battery update failed.', 'error');}
    finally {button.disabled = false;}
  });
  refresh.addEventListener('click', load);
  load();
});
