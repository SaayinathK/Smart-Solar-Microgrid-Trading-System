(() => {
  const initialRole = SessionManager.getUserRole();
  if (!['Backoffice', 'GridOperator', 'Admin', 'MicrogridOperator', 'TransactionVerifier'].includes(initialRole)) {
    window.location.replace('/dashboard.html');
    return;
  }
  let isVerifier = initialRole === 'TransactionVerifier';
  let canManageReservations = false;
  let restrictSlotsToAssignedNodes = false;
  let assignedMicrogridIds = [];
  document.getElementById('new-reservation').hidden = true;
  const body = document.getElementById('reservations-body');
  const esc = value => String(value ?? '').replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));
  const idOf = r => r.id || r._id || '';
  const dateOf = value => value ? new Date(value) : null;
  const validDate = d => d && !Number.isNaN(d.getTime());
  const fmt = (value, options) => { const d = dateOf(value); return validDate(d) ? d.toLocaleString([], options) : '—'; };
  const localDate = value => { const d = dateOf(value); return validDate(d) ? `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}` : ''; };
  const unwrap = response => { const data = response?.data ?? response; return Array.isArray(data) ? data : (data?.items || data?.reservations || []); };
  const statusClass = status => `status-${String(status || 'unknown').toLowerCase().replace(/[^a-z]+/g,'-')}`;
  const slotSelect = document.getElementById('reservation-slot');
  const energyInput = document.getElementById('reservation-energy');
  const slotDetails = document.getElementById('slot-details');
  let availableSlots = [];

  function unwrapList(response) {
    let value = response?.data ?? response;
    if (value?.data !== undefined) value = value.data;
    if (Array.isArray(value)) return value;
    return value?.items || value?.slots || [];
  }

  async function loadAvailableSlots() {
    slotSelect.innerHTML = '<option value="">Loading M1 slots…</option>';
    slotSelect.disabled = true;
    energyInput.value = '';
    energyInput.disabled = true;
    try {
      const response = await ReservationApi.availableSlots();
      const now = Date.now(), lastBookable = now + 7 * 24 * 60 * 60 * 1000;
      availableSlots = unwrapList(response).filter(slot => {
        const start = new Date(slot.startTime).getTime();
        return (!restrictSlotsToAssignedNodes || assignedMicrogridIds.includes(slot.microgridNodeId)) && Number(slot.availableAmount ?? slot.energyAmount) >= 0.1 && start > now && start <= lastBookable && new Date(slot.endTime).getTime() > now;
      });
      slotSelect.innerHTML = '<option value="">Choose an available slot</option>' + availableSlots.map((slot, index) => `<option value="${esc(slot.energySlotId || slot.id)}" data-index="${index}">${esc(`${slot.microgridName || 'Microgrid'} · ${fmt(slot.startTime,{month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'})} · ${slot.availableAmount} kWh available`)}</option>`).join('');
      slotSelect.disabled = availableSlots.length === 0;
      slotDetails.textContent = availableSlots.length ? 'Availability comes from M1 and is checked again by the reservation service.' : 'No eligible published slots with remaining energy are available within seven days.';
    } catch (err) {
      availableSlots = [];
      slotSelect.innerHTML = '<option value="">M1 availability unavailable</option>';
      slotDetails.textContent = err?.message || 'Unable to load published slots.';
    }
  }

  slotSelect.addEventListener('change', () => {
    const slot = availableSlots[Number(slotSelect.selectedOptions[0]?.dataset.index)];
    energyInput.disabled = !slot;
    if (!slot) { energyInput.removeAttribute('max'); return; }
    const available = Number(slot.availableAmount ?? slot.energyAmount);
    energyInput.max = String(available);
    energyInput.placeholder = `Up to ${available} kWh`;
    slotDetails.textContent = `${slot.microgridName || 'Microgrid'} · ${slot.location || 'Location not listed'} · ${fmt(slot.startTime,{month:'short',day:'numeric',year:'numeric',hour:'2-digit',minute:'2-digit'})}–${fmt(slot.endTime,{hour:'2-digit',minute:'2-digit'})} · ${available} kWh remaining · Rs ${slot.pricePerUnit ?? '—'}/kWh`;
  });

  function render(rows) {
    const term = document.getElementById('reservation-search').value.trim().toLowerCase();
    const status = document.getElementById('reservation-status').value.toLowerCase();
    const date = document.getElementById('reservation-date').value;
    const nodeId = document.getElementById('reservation-node').value;
    const filtered = rows.filter(r => {
      const haystack = [r.prosumerName, r.prosumerId, r.microgridName, r.microgridNodeName, r.microgridNodeId, idOf(r)].join(' ').toLowerCase();
      return (!term || haystack.includes(term)) && (!nodeId || r.microgridNodeId === nodeId) && (!status || String(r.status).toLowerCase() === status) && (!date || localDate(r.startTime || r.reservationDate) === date);
    });
    document.getElementById('reservation-count').textContent = `${filtered.length} reservation${filtered.length === 1 ? '' : 's'}`;
    if (!filtered.length) { body.innerHTML = '<tr><td colspan="7" class="reservation-message">No reservations match these filters.</td></tr>'; return; }
    body.innerHTML = filtered.map(r => {
      const statusName = String(r.status || 'Pending');
      const id = esc(idOf(r));
      const staff = canManageReservations;
      const editable = canManageReservations && (statusName === 'Approved' || statusName === 'Pending');
      const actions = `${statusName === 'Pending' && staff ? `<button class="btn btn-primary btn-sm" data-action="approve" data-id="${id}">Approve</button><button class="btn btn-danger btn-sm" data-action="reject" data-id="${id}">Reject</button>` : ''}${editable ? `<button class="btn btn-secondary btn-sm" data-action="modify" data-id="${id}" data-amount="${esc(r.energyAmount)}">Modify</button><button class="btn btn-secondary btn-sm" data-action="cancel" data-id="${id}">Cancel</button>` : ''}${editable ? '' : '<span class="muted">—</span>'}`;
      return `<tr><td class="prosumer-cell"><strong>${esc(r.prosumerName || 'Prosumer')}</strong><span>NIC ${esc(r.prosumerId || '—')}</span></td><td class="node-cell">${esc(r.microgridName || r.microgridNodeName || 'Microgrid node')}<small>${esc(r.microgridNodeId || '')}</small></td><td class="energy-cell">${esc(r.energyAmount ?? '—')} kWh</td><td class="schedule-cell"><strong>${esc(fmt(r.startTime || r.reservationDate,{month:'short',day:'numeric',year:'numeric'}))}</strong><span>${esc(fmt(r.startTime,{hour:'2-digit',minute:'2-digit'}))}${r.endTime ? ` – ${esc(fmt(r.endTime,{hour:'2-digit',minute:'2-digit'}))}` : ''}</span></td><td>${esc(fmt(r.createdAt,{month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'}))}</td><td><span class="status-badge ${statusClass(statusName)}">${esc(statusName)}</span></td><td><div class="reservation-actions">${actions}</div></td></tr>`;
    }).join('');
  }

  let reservations = [];
  async function load(silent = false) {
    if (!silent) body.innerHTML = '<tr><td colspan="7" class="reservation-message">Loading reservations…</td></tr>';
    const refresh = document.getElementById('refresh-reservations'); refresh.disabled = true;
    try {
      const accessResponse = await ReservationApi.access();
      const access = accessResponse?.data || accessResponse || {};
      isVerifier = access.approvedReservationsOnly === true;
      canManageReservations = access.canManageReservations === true;
      restrictSlotsToAssignedNodes = access.microgridScoped === true;
      assignedMicrogridIds = access.assignedMicrogridIds || [];
      document.getElementById('new-reservation').hidden = access.canCreateForProsumer !== true;
      if (isVerifier) {
        document.querySelector('.reservation-toolbar-heading h2').textContent = 'Approved reservations';
        document.querySelector('.reservation-toolbar-heading p').textContent = 'Read-only reservations ready for transaction verification.';
        document.querySelector('.reservation-stats').hidden = true;
        document.getElementById('reservation-status').disabled = true;
      }
      const [listResult, summaryResult] = await Promise.allSettled([ReservationApi.list(isVerifier ? {status:'Approved'} : {}), isVerifier ? Promise.reject(new Error('Read-only verifier role')) : ReservationApi.summary()]);
      if (listResult.status === 'rejected') throw listResult.reason;
      reservations = unwrap(listResult.value);
      const nodeFilter = document.getElementById('reservation-node');
      const selectedNode = nodeFilter.value;
      const nodes = [...new Map(reservations.map(r => [r.microgridNodeId, r.microgridName || r.microgridNodeId]).filter(([id]) => id)).entries()];
      nodeFilter.innerHTML = '<option value="">All microgrids</option>' + nodes.map(([id,name]) => `<option value="${esc(id)}">${esc(name)}</option>`).join('');
      if (nodes.some(([id]) => id === selectedNode)) nodeFilter.value = selectedNode;
      render(reservations);
      document.getElementById('reservation-service-status').innerHTML = '<span class="live-dot"></span> Live · API connected';
      if (summaryResult.status === 'fulfilled') {
        const s = summaryResult.value?.data || summaryResult.value || {};
        document.getElementById('stat-pending').textContent = s.pendingCount ?? reservations.filter(r => r.status === 'Pending').length;
        document.getElementById('stat-approved').textContent = s.approvedFutureCount ?? reservations.filter(r => r.status === 'Approved' && new Date(r.startTime) > new Date()).length;
        document.getElementById('stat-completed').textContent = s.completedThisMonthCount ?? '—';
        document.getElementById('stat-energy').textContent = `${Number(s.totalEnergyReserved ?? reservations.filter(r => ['Pending','Approved'].includes(r.status)).reduce((n,r) => n + Number(r.energyAmount || 0),0)).toLocaleString()} kWh`;
      } else {
        document.getElementById('stat-pending').textContent = reservations.filter(r => r.status === 'Pending').length;
        document.getElementById('stat-approved').textContent = reservations.filter(r => r.status === 'Approved').length;
        document.getElementById('stat-completed').textContent = reservations.filter(r => r.status === 'Completed').length;
        document.getElementById('stat-energy').textContent = `${reservations.filter(r => ['Pending','Approved'].includes(r.status)).reduce((n,r) => n + Number(r.energyAmount || 0),0).toLocaleString()} kWh`;
      }
    } catch (err) {
      if (!silent || !reservations.length) {
        body.innerHTML = `<tr><td colspan="7" class="reservation-message error">${esc(err?.message || 'Unable to load reservations. Check that the M2 API is available.')} <button class="btn btn-secondary btn-sm" id="retry-reservations">Try again</button></td></tr>`;
        document.getElementById('retry-reservations')?.addEventListener('click', () => load(), {once:true});
      }
      document.getElementById('reservation-service-status').textContent = 'API unavailable';
      if (!silent) ApiClient.showToast(err?.message || 'Could not load reservations', 'error');
    }
    finally { refresh.disabled = false; }
  }

  body.addEventListener('click', async event => {
    const button = event.target.closest('[data-action]'); if (!button) return;
    const {action,id} = button.dataset; if (!id) return;
    if (action === 'modify') {
      const amount = prompt('New energy amount (kWh):', button.dataset.amount);
      if (amount === null) return;
      const parsed = Number(amount); if (!Number.isFinite(parsed) || parsed <= 0) { ApiClient.showToast('Enter an energy amount greater than zero.', 'error'); return; }
      button.disabled = true;
      try { await ReservationApi.update(id, {energyAmount:parsed}); ApiClient.showToast('Reservation updated.', 'success'); await load(); }
      catch (err) { ApiClient.showToast(err?.message || 'Unable to update reservation', 'error'); button.disabled = false; }
      return;
    }
    let reason;
    if (action === 'reject') { reason = prompt('Reason for rejection (optional):'); if (reason === null) return; }
    if (action === 'cancel' && !confirm('Cancel this reservation?')) return;
    button.disabled = true;
    try {
      if (action === 'approve') await ReservationApi.approve(id);
      else if (action === 'reject') await ReservationApi.reject(id, reason);
      else await ReservationApi.cancel(id);
      ApiClient.showToast(`Reservation ${action}d successfully.`, 'success'); await load();
    } catch (err) { ApiClient.showToast(err?.message || `Unable to ${action} reservation`, 'error'); button.disabled = false; }
  });

  document.getElementById('new-reservation').addEventListener('click', async () => {
    document.getElementById('reservation-dialog').showModal();
    await loadAvailableSlots();
  });
  document.getElementById('reservation-form').addEventListener('submit', async event => {
    event.preventDefault(); const form = new FormData(event.currentTarget); const submit = document.getElementById('save-reservation'); submit.disabled = true;
    try {
      const slotId = String(form.get('energySlotId') || '').trim();
      const amount = Number(form.get('energyAmount'));
      const slot = availableSlots.find(item => (item.energySlotId || item.id) === slotId);
      if (!slot) throw new Error('Choose a currently available M1 energy slot.');
      if (!Number.isFinite(amount) || amount < 0.1 || amount > Number(slot.availableAmount ?? slot.energyAmount)) throw new Error('Enter an amount within the slot’s remaining energy.');
      await ReservationApi.create({prosumerId:String(form.get('prosumerId') || '').trim(),energySlotId:slotId,energyAmount:amount});
      document.getElementById('reservation-dialog').close(); event.currentTarget.reset(); ApiClient.showToast('Reservation submitted for review.', 'success'); await load();
    } catch (err) { ApiClient.showToast(err?.message || 'Unable to create reservation', 'error'); }
    finally { submit.disabled = false; }
  });
  ['reservation-search','reservation-status','reservation-date','reservation-node'].forEach(id => document.getElementById(id).addEventListener(id === 'reservation-search' ? 'input' : 'change', () => render(reservations)));
  document.getElementById('refresh-reservations').addEventListener('click', load);
  setInterval(() => { if (!document.hidden) load(true); }, 30000);
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', load, {once:true});
  else load();
})();
