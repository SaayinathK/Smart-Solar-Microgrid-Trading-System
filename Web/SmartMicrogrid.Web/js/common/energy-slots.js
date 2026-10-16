document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth()) return;
  renderAppLayout('slots', 'Energy Slots Management');
  const allowed = () => ['Admin', 'MicrogridOperator'].includes(SessionManager.getUserRole());
  const tbody = document.getElementById('slots-table-body');
  const trigger = document.getElementById('publish-slot');
  const refresh = document.getElementById('refresh-slots');
  const dialog = document.getElementById('publish-slot-dialog');
  const form = document.getElementById('publish-slot-form');
  const body = dialog.querySelector('.slot-dialog-body');
  const select = document.getElementById('slot-microgrid');
  const amount = document.getElementById('slot-amount');
  const start = document.getElementById('slot-start');
  const end = document.getElementById('slot-end');
  const submit = document.getElementById('confirm-publish-slot');
  const error = document.getElementById('slot-form-error');
  let microgrids = [], saving = false, loadVersion = 0, openVersion = 0;
  trigger.hidden = !allowed();
  const escape = value => String(value ?? '').replace(/[&<>"']/g, char => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
  const number = value => Number(value).toLocaleString(undefined, {maximumFractionDigits: 2});
  const localInput = date => {
    const pad = value => String(value).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth()+1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  };
  function isEnded(slot) { return new Date(slot.endTime).getTime() <= Date.now(); }
  function displayStatus(slot) {
    return isEnded(slot) && ['Available', 'PartiallyReserved'].includes(slot.status) ? 'Expired' : slot.status;
  }

  async function load() {
    const version = ++loadVersion;
    refresh.disabled = true;
    tbody.setAttribute('aria-busy', 'true');
    try {
      const response = await MicrogridApi.getEnergySlots();
      if (version !== loadVersion) return;
      if (!response?.success || !Array.isArray(response.data)) throw new Error(response?.message || 'Could not load energy slots.');
      const slots = [...response.data].sort((a,b) => new Date(b.startTime) - new Date(a.startTime));
      const open = slots.filter(slot => !isEnded(slot) && ['Available','PartiallyReserved'].includes(slot.status) && Number(slot.availableAmount)>0);
      document.getElementById('slot-summary').textContent = `${slots.length} published slots · ${open.length} with an open delivery window and remaining energy`;
      document.getElementById('slot-expiry-note').hidden = !slots.some(slot => displayStatus(slot) === 'Expired');
      tbody.innerHTML = slots.length ? slots.map(slot => {
        const status = displayStatus(slot);
        const badge = status === 'Expired' ? 'status-inactive' : status === 'Cancelled' ? 'status-offline' : status === 'FullyReserved' ? 'status-warning' : 'status-active';
        const label = status === 'PartiallyReserved' ? 'Partially reserved' : status === 'FullyReserved' ? 'Fully reserved' : status;
        return `<tr><td><strong>${escape(slot.microgridName)}</strong><small class="slot-table-note">${escape(slot.location)}</small></td><td>${number(slot.energyAmount)} kWh</td><td>${number(slot.availableAmount)} kWh${status === 'Expired' ? '<small class="slot-table-note">Delivery window closed</small>' : ''}</td><td>${escape(new Date(slot.startTime).toLocaleString())}</td><td>${escape(new Date(slot.endTime).toLocaleString())}</td><td>LKR ${number(slot.pricePerUnit)}</td><td><span class="status-badge ${badge}">${escape(label)}</span></td><td>${allowed() && slot.status === 'Available' && !isEnded(slot) ? `<button type="button" class="btn btn-danger btn-sm" data-cancel-slot="${escape(slot.id)}">Cancel slot</button>` : '<span class="slot-table-note">No actions</span>'}</td></tr>`;
      }).join('') : '<tr><td colspan="8" style="text-align:center;padding:32px">No energy slots published. Publish a slot to offer energy for booking.</td></tr>';
    } catch (err) {
      if (version !== loadVersion) return;
      document.getElementById('slot-summary').textContent = 'Energy slots could not be loaded.';
      document.getElementById('slot-expiry-note').hidden = true;
      tbody.innerHTML = `<tr><td colspan="8" style="text-align:center;padding:32px">${escape(err.message || 'Could not load energy slots. Please refresh.')}</td></tr>`;
    } finally {
      if (version === loadVersion) { refresh.disabled = false; tbody.removeAttribute('aria-busy'); }
    }
  }

  function showError(message) { error.textContent = message; error.hidden = false; error.focus(); }
  function updateCapacity() {
    const node = microgrids.find(node => node.id === select.value);
    if (node) {
      amount.max = node.capacity;
      document.getElementById('slot-capacity-note').textContent = `${node.location} · Total generation capacity: ${number(node.capacity)} kW`;
    } else { amount.removeAttribute('max'); document.getElementById('slot-capacity-note').textContent = 'Choose an Active microgrid.'; }
  }
  function validateDates() {
    start.setCustomValidity(start.value && new Date(start.value).getTime() <= Date.now() ? 'Choose a future delivery start time.' : '');
    end.setCustomValidity(start.value && end.value && new Date(end.value) <= new Date(start.value) ? 'End time must be later than start time.' : '');
  }
  async function open() {
    if (!allowed() || dialog.open) return;
    const version = ++openVersion;
    const selectedId = select.value;
    error.hidden = true;
    submit.disabled = true;
    select.disabled = true;
    select.replaceChildren(new Option('Loading active microgrids...', ''));
    if (!start.value) start.value = localInput(new Date(Date.now()+3600000));
    if (!end.value) end.value = localInput(new Date(Date.now()+14400000));
    start.min = localInput(new Date());
    document.getElementById('slot-timezone').textContent = `Times use your local timezone: ${Intl.DateTimeFormat().resolvedOptions().timeZone}.`;
    dialog.showModal();
    body.scrollTop = 0;
    try {
      const response = await MicrogridApi.getMicrogrids({status:'Active'});
      if (version !== openVersion || !dialog.open) return;
      if (!response?.success || !Array.isArray(response.data)) throw new Error(response?.message || 'Could not load microgrids. Close this form and retry.');
      microgrids = response.data.filter(node => node.status === 'Active' && node.isActive !== false);
      select.replaceChildren(new Option('Select a microgrid', ''), ...microgrids.map(node => new Option(`${node.name} — ${node.location}`, node.id)));
      if (microgrids.some(node => node.id === selectedId)) select.value = selectedId;
      select.disabled = !microgrids.length;
      submit.disabled = !microgrids.length;
      if (!microgrids.length) { showError('No Active microgrids are available. Activate a microgrid in Microgrid Nodes, then try again.'); return; }
      updateCapacity();
      select.focus();
    } catch (err) { if (version === openVersion && dialog.open) showError(err.message || 'Could not load microgrids. Close this form and retry.'); }
  }
  trigger.addEventListener('click', open);
  refresh.addEventListener('click', load);
  select.addEventListener('change', updateCapacity);
  [start,end].forEach(input => input.addEventListener('input', validateDates));
  dialog.querySelectorAll('[data-close-slot]').forEach(button => button.addEventListener('click', () => { if (!saving) dialog.close(); }));
  dialog.addEventListener('cancel', event => { if (saving) event.preventDefault(); });
  dialog.addEventListener('close', () => { ++openVersion; trigger.focus({preventScroll:true}); });
  form.addEventListener('submit', async event => {
    event.preventDefault();
    if (!allowed() || saving) return;
    error.hidden = true;
    validateDates();
    if (!form.reportValidity()) return;
    if (!microgrids.some(node => node.id === select.value)) { showError('Choose an Active microgrid.'); return; }
    const payload = {microgridNodeId:select.value, energyAmount:Number(amount.value), availableAmount:Number(amount.value), pricePerUnit:Number(document.getElementById('slot-price').value), startTime:new Date(start.value).toISOString(), endTime:new Date(end.value).toISOString(), status:'Available'};
    saving = true;
    form.setAttribute('aria-busy','true'); body.inert = true;
    dialog.querySelectorAll('button').forEach(button => {button.disabled = true;});
    submit.textContent = 'Publishing…';
    try {
      const response = await MicrogridApi.createEnergySlot(payload);
      if (!response?.success) throw new Error(response?.message || 'Could not publish the energy slot.');
      dialog.close(); form.reset(); validateDates();
      ApiClient.showToast('Energy slot published. It is now listed on Availability.', 'success');
      await load();
    } catch (err) { showError(err.message || 'Publishing failed. Your entries have been kept; please try again.'); }
    finally {
      saving = false; body.inert = false; form.removeAttribute('aria-busy');
      dialog.querySelectorAll('button').forEach(button => {button.disabled = false;});
      submit.textContent = 'Publish energy slot';
      if (!error.hidden && dialog.open) error.focus();
    }
  });
  tbody.addEventListener('click', async event => {
    const button = event.target.closest('[data-cancel-slot]');
    if (!button || !allowed() || !confirm('Cancel this energy slot?')) return;
    button.disabled = true;
    try {
      const response = await MicrogridApi.updateSlotStatus(button.dataset.cancelSlot, 'Cancelled');
      if (!response?.success) throw new Error(response?.message || 'Cancellation failed.');
      ApiClient.showToast('Energy slot cancelled.', 'info'); await load();
    } catch (err) { ApiClient.showToast(err.message || 'Could not cancel this slot.', 'error'); button.disabled = false; }
  });
  load();
});
