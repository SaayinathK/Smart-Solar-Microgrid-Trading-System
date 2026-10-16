/* In-page node registration. The native dialog provides focus trapping and Escape. */
(() => {
  const dialog = document.getElementById('microgrid-registration-dialog');
  const trigger = document.getElementById('register-microgrid');
  const form = document.getElementById('create-microgrid-form');
  const body = dialog.querySelector('.registration-body');
  const error = document.getElementById('registration-error');
  const submit = document.getElementById('registration-submit');
  const permitted = () => SessionManager.isAuthenticated()
    && ['Admin', 'MicrogridOperator'].includes(SessionManager.getUserRole());
  let picker;
  let saving = false;

  function open() {
    if (!permitted() || dialog.open) return;
    error.hidden = true;
    dialog.showModal();
    if (!picker) picker = new LocationPicker(document.getElementById('location-picker'));
    requestAnimationFrame(() => picker.mapView?.map.invalidateSize());
    body.scrollTop = 0;
    document.getElementById('name').focus({ preventScroll: true });
  }

  function showError(message) {
    error.textContent = message;
    error.hidden = false;
    error.focus();
  }

  function validateBattery() {
    const capacity = document.getElementById('batteryCapacity');
    const level = document.getElementById('currentBatteryLevel');
    level.setCustomValidity(level.value !== '' && capacity.value !== '' && Number(level.value) > Number(capacity.value)
      ? 'Initial battery level cannot exceed battery capacity.' : '');
  }

  trigger.addEventListener('click', open);
  dialog.querySelectorAll('[data-registration-close]').forEach(button => {
    button.addEventListener('click', () => { if (!saving) dialog.close(); });
  });
  dialog.addEventListener('cancel', event => { if (saving) event.preventDefault(); });
  dialog.addEventListener('close', () => trigger.focus({ preventScroll: true }));
  ['batteryCapacity', 'currentBatteryLevel'].forEach(id => document.getElementById(id).addEventListener('input', validateBattery));
  document.getElementById('name').addEventListener('input', event => {
    event.target.setCustomValidity(event.target.value.trim() ? '' : 'Enter a microgrid name.');
  });

  form.addEventListener('submit', async event => {
    event.preventDefault();
    if (saving || !permitted()) return;
    error.hidden = true;
    validateBattery();
    if (!form.reportValidity()) return;
    const location = picker.getSelection();
    if (!location) { showError('Choose a site on the map or enter an address and valid coordinates.'); return; }
    const value = id => document.getElementById(id).value;
    const payload = {
      name: value('name').trim(),
      ...location,
      status: value('status'),
      isActive: value('status') === 'Active',
      capacity: Number(value('capacity')),
      batteryStorageSlots: Number(value('batteryStorageSlots')),
      batteryCapacity: Number(value('batteryCapacity')),
      currentBatteryLevel: Number(value('currentBatteryLevel')),
      description: value('description').trim()
    };
    saving = true;
    form.setAttribute('aria-busy', 'true');
    body.inert = true;
    dialog.querySelectorAll('button').forEach(button => { button.disabled = true; });
    submit.textContent = 'Creating microgrid…';
    try {
      const response = await MicrogridApi.createMicrogrid(payload);
      if (!response?.success) throw new Error(response?.message || 'Could not create the microgrid. Please try again.');
      dialog.close();
      form.reset();
      picker.manualInput(); // Invalidates any unfinished address or device lookup.
      picker.addressLookupRevision = null;
      validateBattery();
      ApiClient.showToast('Microgrid registered successfully.', 'success');
      document.getElementById('search-input').value = '';
      document.getElementById('status-filter').value = '';
      await loadMicrogrids();
    } catch (err) {
      showError(err.message || 'Unable to create this microgrid. Check your connection and try again.');
    } finally {
      saving = false;
      body.inert = false;
      form.removeAttribute('aria-busy');
      dialog.querySelectorAll('button').forEach(button => { button.disabled = false; });
      submit.textContent = 'Create microgrid';
      if (!error.hidden && dialog.open) error.focus();
    }
  });

  // Preserve bookmarks to the former registration page without leaving this workspace.
  const url = new URL(window.location.href);
  if (url.searchParams.get('create') === '1' && permitted()) {
    open();
    url.searchParams.delete('create');
    history.replaceState(history.state, '', url);
  }
})();
