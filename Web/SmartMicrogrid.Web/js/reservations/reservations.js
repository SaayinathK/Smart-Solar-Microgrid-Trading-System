(() => {
  if (!['Backoffice', 'GridOperator', 'Admin', 'MicrogridOperator'].includes(SessionManager.getUserRole())) {
    window.location.replace('/dashboard.html');
    return;
  }
  const body = document.getElementById('reservations-body');
  const esc = value => String(value ?? '').replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));
  const idOf = r => r.id || r._id || '';
  const dateOf = value => value ? new Date(value) : null;
  const validDate = d => d && !Number.isNaN(d.getTime());
  const fmt = (value, options) => { const d = dateOf(value); return validDate(d) ? d.toLocaleString([], options) : '—'; };
  const localDate = value => { const d = dateOf(value); return validDate(d) ? `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}` : ''; };
  const unwrap = response => { const data = response?.data ?? response; return Array.isArray(data) ? data : (data?.items || data?.reservations || []); };
  const statusClass = status => `status-${String(status || 'unknown').toLowerCase().replace(/[^a-z]+/g,'-')}`;

  function render(rows) {
    const term = document.getElementById('reservation-search').value.trim().toLowerCase();
    const status = document.getElementById('reservation-status').value.toLowerCase();
    const date = document.getElementById('reservation-date').value;
    const filtered = rows.filter(r => {
      const haystack = [r.prosumerName, r.prosumerId, r.microgridName, r.microgridNodeName, r.microgridNodeId, idOf(r)].join(' ').toLowerCase();
      return (!term || haystack.includes(term)) && (!status || String(r.status).toLowerCase() === status) && (!date || localDate(r.startTime || r.reservationDate) === date);
    });
    document.getElementById('reservation-count').textContent = `${filtered.length} reservation${filtered.length === 1 ? '' : 's'}`;
    if (!filtered.length) { body.innerHTML = '<tr><td colspan="7" class="reservation-message">No reservations match these filters.</td></tr>'; return; }
    body.innerHTML = filtered.map(r => {
      const statusName = String(r.status || 'Pending');
      const id = esc(idOf(r));
      const staff = ['Backoffice','GridOperator','Admin','MicrogridOperator'].includes(SessionManager.getUserRole());
      const editable = statusName === 'Approved' || statusName === 'Pending';
      const actions = `${statusName === 'Pending' && staff ? `<button class="btn btn-primary btn-sm" data-action="approve" data-id="${id}">Approve</button><button class="btn btn-danger btn-sm" data-action="reject" data-id="${id}">Reject</button>` : ''}${editable ? `<button class="btn btn-secondary btn-sm" data-action="modify" data-id="${id}" data-amount="${esc(r.energyAmount)}">Modify</button><button class="btn btn-secondary btn-sm" data-action="cancel" data-id="${id}">Cancel</button>` : ''}${editable ? '' : '<span class="muted">—</span>'}`;
      return `<tr><td class="prosumer-cell"><strong>${esc(r.prosumerName || 'Prosumer')}</strong><span>NIC ${esc(r.prosumerId || '—')}</span></td><td class="node-cell">${esc(r.microgridName || r.microgridNodeName || 'Microgrid node')}<small>${esc(r.microgridNodeId || '')}</small></td><td class="energy-cell">${esc(r.energyAmount ?? '—')} kWh</td><td class="schedule-cell"><strong>${esc(fmt(r.startTime || r.reservationDate,{month:'short',day:'numeric',year:'numeric'}))}</strong><span>${esc(fmt(r.startTime,{hour:'2-digit',minute:'2-digit'}))}${r.endTime ? ` – ${esc(fmt(r.endTime,{hour:'2-digit',minute:'2-digit'}))}` : ''}</span></td><td>${esc(fmt(r.createdAt,{month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'}))}</td><td><span class="status-badge ${statusClass(statusName)}">${esc(statusName)}</span></td><td><div class="reservation-actions">${actions}</div></td></tr>`;
    }).join('');
  }

  let reservations = [];
  async function load() {
    body.innerHTML = '<tr><td colspan="7" class="reservation-message">Loading reservations…</td></tr>';
    const refresh = document.getElementById('refresh-reservations'); refresh.disabled = true;
    try {
      const [listResult, summaryResult] = await Promise.allSettled([ReservationApi.list(), ReservationApi.summary()]);
      if (listResult.status === 'rejected') throw listResult.reason;
      reservations = unwrap(listResult.value); render(reservations);
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
    } catch (err) { body.innerHTML = `<tr><td colspan="7" class="reservation-message error">${esc(err?.message || 'Unable to load reservations. Check that the M2 API is available.')}</td></tr>`; ApiClient.showToast(err?.message || 'Could not load reservations', 'error'); }
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

  document.getElementById('new-reservation').addEventListener('click', () => document.getElementById('reservation-dialog').showModal());
  document.getElementById('reservation-form').addEventListener('submit', async event => {
    event.preventDefault(); const form = new FormData(event.currentTarget); const submit = document.getElementById('save-reservation'); submit.disabled = true;
    try {
      await ReservationApi.create({prosumerId:form.get('prosumerId').trim(),energySlotId:form.get('energySlotId').trim(),energyAmount:Number(form.get('energyAmount'))});
      document.getElementById('reservation-dialog').close(); event.currentTarget.reset(); ApiClient.showToast('Reservation submitted for review.', 'success'); await load();
    } catch (err) { ApiClient.showToast(err?.message || 'Unable to create reservation', 'error'); }
    finally { submit.disabled = false; }
  });
  ['reservation-search','reservation-status','reservation-date'].forEach(id => document.getElementById(id).addEventListener(id === 'reservation-search' ? 'input' : 'change', () => render(reservations)));
  document.getElementById('refresh-reservations').addEventListener('click', load);
  document.addEventListener('DOMContentLoaded', load);
})();
