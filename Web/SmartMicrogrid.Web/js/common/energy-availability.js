document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth()) return;
  renderAppLayout('availability', 'Energy Availability');
  const V = EnergyVisuals;
  const results = document.getElementById('availability-results');
  const schedule = document.getElementById('availability-schedule');
  const comparison = document.getElementById('availability-comparison');
  const button = document.getElementById('search-supply');
  const canPublish = ['Admin', 'MicrogridOperator'].includes(SessionManager.getUserRole());
  let hasFilters = false;
  let requestVersion = 0, current = null;
  const booking = new EnergyReservationDialog(async slotId => {
    const confirmation = document.getElementById('availability-booking-success');
    confirmation.hidden = false;
    await load();
    const buttons = Array.from(results.querySelectorAll('[data-reserve-slot]'));
    (buttons.find(item => item.dataset.reserveSlot === slotId) || buttons[0] || button).focus({preventScroll:true});
  });

  function emptyOffers() {
    const title = hasFilters ? 'No energy offers match your filters' : 'No bookable energy offers yet';
    const detail = hasFilters
      ? 'Try another location or a lower minimum energy amount.'
      : 'Registering a microgrid adds the site to your network. Energy appears here after an energy slot is published with remaining supply and a delivery window that has not ended.';
    const action = hasFilters
      ? '<button type="button" class="btn btn-secondary" data-clear-supply-filters>Clear filters</button>'
      : canPublish
        ? `<a class="btn btn-primary" href="${V.escape(getWebPageUrl('energy-slots.html'))}">Open Energy Slots</a><small>Choose Publish New Energy Slot, select an Active microgrid, and enter the energy amount, price, and future delivery times. Publish it as Available.</small>`
        : '<small>Check back after an operator publishes a new energy offer.</small>';
    return `<section class="availability-empty availability-publish-help"><strong>${title}</strong><p>${detail}</p>${action}</section>`;
  }

  function render(list) {
    const sort = document.getElementById('sort-by').value;
    const sorted = [...list].sort((a, b) => sort === 'price' ? Number(a.pricePerUnit) - Number(b.pricePerUnit) : sort === 'energy' ? Number(b.availableAmount) - Number(a.availableAmount) : new Date(a.startTime) - new Date(b.startTime));
    const available = V.sum(list, 'availableAmount');
    const prices = list.map(slot => V.number(slot.pricePerUnit)).filter(price => price !== null);
    document.getElementById('availability-total').textContent = list.length;
    document.getElementById('summary-energy').textContent = `${V.format(available)} kWh`;
    document.getElementById('summary-price').textContent = prices.length ? `LKR ${Math.min(...prices).toFixed(2)} / kWh` : '—';
    document.getElementById('summary-locations').textContent = new Set(list.map(slot => slot.location).filter(Boolean)).size;
    schedule.innerHTML = V.scheduleChart(list);
    const groups = new Map();
    list.forEach(slot => {
      const key = slot.microgridNodeId || slot.microgridName || 'Unnamed microgrid';
      if (!groups.has(key)) groups.set(key, {name: slot.microgridName || 'Unnamed microgrid', total: 0, values: [0]});
      const group = groups.get(key), amount = V.number(slot.availableAmount);
      if (amount === null) {group.total = null; group.values[0] = null;}
      else if (group.total !== null) {group.total += amount; group.values[0] += amount;}
    });
    comparison.innerHTML = V.comparisonChart([...groups.values()], [{label: 'Available energy', color: '#3b82f6'}], 'Available energy offers grouped by microgrid');
    results.innerHTML = sorted.length ? sorted.map(slot => {
      const remaining = V.percent(slot.availableAmount, slot.energyAmount);
      const validDates = slot.startTime && slot.endTime && Number.isFinite(new Date(slot.startTime).getTime()) && Number.isFinite(new Date(slot.endTime).getTime());
      return `<article class="energy-offer-card" data-energy-enter>
        <div class="energy-offer-photo"><div><span class="offer-location">${V.escape(slot.location || 'Location unavailable')}</span><h3>${V.escape(slot.microgridName || 'Unnamed microgrid')}</h3></div>${V.gauge(remaining, 'Supply remaining')}</div>
        <div class="energy-offer-body"><div class="offer-metrics"><div><span>Available energy</span><strong>${V.format(slot.availableAmount)} <small>kWh</small></strong></div><div><span>Unit price</span><strong class="price-value">LKR ${V.number(slot.pricePerUnit) === null ? '—' : Number(slot.pricePerUnit).toFixed(2)} <small>/ kWh</small></strong></div></div>
          ${V.meter(remaining, 'Remaining slot energy')}<div class="offer-meter-label"><span>${remaining === null ? 'Reading unavailable' : `${V.format(remaining)}% remains`}</span><span>Total ${V.format(slot.energyAmount)} kWh</span></div>
          <div class="offer-window"><span>Delivery window</span><strong>${validDates ? `${V.escape(V.date(slot.startTime))} to ${V.escape(V.date(slot.endTime))}` : 'Schedule unavailable'}</strong></div>
          <button type="button" class="btn btn-primary energy-offer-reserve" data-reserve-slot="${V.escape(slot.energySlotId || slot.id || '')}" aria-label="Reserve energy from ${V.escape(slot.microgridName || 'this microgrid')}, ${V.format(slot.availableAmount)} kWh available at LKR ${V.escape(slot.pricePerUnit)} per kWh">Reserve energy <span aria-hidden="true">&rarr;</span></button>
        </div></article>`;
    }).join('') : emptyOffers();
    if (!list.length) {
      schedule.innerHTML = V.empty('No delivery windows to chart', 'Published energy offers with an unexpired delivery window appear here.');
      comparison.innerHTML = V.empty('No energy offers to compare', 'This chart compares published slot energy, rather than microgrid generation capacity.');
    }
    V.animate(document.querySelector('.main-content'));
  }

  async function load() {
    const version = ++requestVersion;
    button.disabled = true;
    button.textContent = 'Searching...';
    results.setAttribute('aria-busy', 'true');
    const params = {};
    const location = document.getElementById('filter-location').value.trim();
    const minimum = document.getElementById('filter-min-energy').value;
    if (location) params.location = location;
    if (minimum !== '') {
      if (V.number(minimum) === null) {ApiClient.showToast('Minimum energy must be zero or greater.', 'error'); button.disabled = false; button.textContent = 'Search supply'; results.setAttribute('aria-busy', 'false'); return;}
      params.minimumEnergy = minimum;
    }
    try {
      const response = await MicrogridApi.getEnergyAvailability(params);
      if (version !== requestVersion) return;
      if (!response?.success || !Array.isArray(response.data)) throw new Error(response?.message || 'Energy availability is unavailable.');
      current = response.data;
      hasFilters = Boolean(location || minimum !== '');
      render(current);
    } catch (error) {
      if (version !== requestVersion) return;
      current = null;
      for (const id of ['availability-total', 'summary-energy', 'summary-price', 'summary-locations']) document.getElementById(id).textContent = '—';
      schedule.innerHTML = comparison.innerHTML = V.empty('Chart data unavailable', 'Search again to refresh available supply.');
      results.innerHTML = V.empty('Could not load energy availability', error.message || 'Please try again.');
    } finally {
      if (version === requestVersion) {button.disabled = false; button.textContent = 'Search supply'; results.setAttribute('aria-busy', 'false');}
    }
  }
  document.getElementById('availability-filters').addEventListener('submit', event => {event.preventDefault(); load();});
  results.addEventListener('click', event => {
    if (event.target.closest('[data-clear-supply-filters]')) document.getElementById('clear-filters').click();
    const reserve = event.target.closest('[data-reserve-slot]');
    if (reserve) {
      const slot = current?.find(item => (item.energySlotId || item.id) === reserve.dataset.reserveSlot);
      if (slot) booking.open(slot, reserve);
    }
  });
  document.getElementById('sort-by').addEventListener('change', () => {if (current) render(current);});
  document.getElementById('clear-filters').addEventListener('click', () => {
    document.getElementById('filter-location').value = '';
    document.getElementById('filter-min-energy').value = '';
    document.getElementById('sort-by').value = 'soonest';
    load();
  });
  load();
});
