/* ==========================================================================
   Smart Microgrid Energy System - M4 System Configuration Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  // Toggles are applied through their own PATCH endpoints, so the last values
  // received from the server are tracked to detect what actually changed.
  const lastKnown = { maintenanceMode: false, allowRegistration: true };

  const form = document.getElementById('config-form');

  document.getElementById('reload-btn').addEventListener('click', loadConfiguration);
  form.addEventListener('submit', saveConfiguration);

  loadConfiguration();

  async function loadConfiguration() {
    try {
      const response = await AdminApi.getConfiguration();

      if (response.success && response.data) {
        populate(response.data);
      } else {
        ApiClient.showToast(response.message || 'Failed to load configuration.', 'error');
      }
    } catch (err) {
      ApiClient.showToast(err.message || 'Network error while loading configuration.', 'error');
    }
  }

  function populate(config) {
    lastKnown.maintenanceMode = !!config.maintenanceMode;
    lastKnown.allowRegistration = !!config.allowRegistration;

    document.getElementById('platform-name').value = config.platformName || '';
    document.getElementById('platform-description').value = config.platformDescription || '';
    document.getElementById('maintenance-mode').value = String(config.maintenanceMode);
    document.getElementById('maintenance-message').value = config.maintenanceMessage || '';
    document.getElementById('allow-registration').value = String(config.allowRegistration);
    document.getElementById('session-timeout').value = config.sessionTimeoutMinutes;
    document.getElementById('max-login-attempts').value = config.maxLoginAttempts;
    document.getElementById('default-page-size').value = config.defaultPageSize;

    document.getElementById('last-updated').textContent = AdminUi.formatDateTime(config.updatedAt);
    document.getElementById('last-updated-by').textContent = config.updatedBy
      ? ` by ${config.updatedBy}` : '';

    clearErrors();
  }

  function clearErrors() {
    ['err-name', 'err-description', 'err-timeout', 'err-attempts', 'err-page-size']
      .forEach(id => { document.getElementById(id).textContent = ''; });
  }

  function readForm() {
    return {
      platformName: document.getElementById('platform-name').value.trim(),
      platformDescription: document.getElementById('platform-description').value.trim(),
      maintenanceMessage: document.getElementById('maintenance-message').value.trim() || null,
      sessionTimeoutMinutes: Number(document.getElementById('session-timeout').value),
      maxLoginAttempts: Number(document.getElementById('max-login-attempts').value),
      defaultPageSize: Number(document.getElementById('default-page-size').value)
    };
  }

  function validate(config) {
    clearErrors();
    let valid = true;

    if (config.platformName.length < 3) {
      document.getElementById('err-name').textContent = 'Platform name must be at least 3 characters.';
      valid = false;
    }
    if (config.platformDescription.length > 500) {
      document.getElementById('err-description').textContent = 'Description cannot exceed 500 characters.';
      valid = false;
    }
    if (!Number.isFinite(config.sessionTimeoutMinutes) ||
        config.sessionTimeoutMinutes < 5 || config.sessionTimeoutMinutes > 1440) {
      document.getElementById('err-timeout').textContent = 'Session timeout must be between 5 and 1440 minutes.';
      valid = false;
    }
    if (!Number.isFinite(config.maxLoginAttempts) ||
        config.maxLoginAttempts < 1 || config.maxLoginAttempts > 20) {
      document.getElementById('err-attempts').textContent = 'Maximum login attempts must be between 1 and 20.';
      valid = false;
    }
    if (!Number.isFinite(config.defaultPageSize) ||
        config.defaultPageSize < 5 || config.defaultPageSize > 200) {
      document.getElementById('err-page-size').textContent = 'Default page size must be between 5 and 200.';
      valid = false;
    }

    return valid;
  }

  async function saveConfiguration(event) {
    event.preventDefault();

    const config = readForm();

    if (!validate(config)) {
      ApiClient.showToast('Please correct the highlighted fields.', 'error');
      return;
    }

    // The PUT body deliberately excludes the two toggles, so their response still
    // carries the pre-save values. Capture what the officer actually chose before
    // the round trip, otherwise populate() would silently discard the change.
    const desiredMaintenance = readToggle('maintenance-mode');
    const desiredRegistration = readToggle('allow-registration');
    const maintenanceMessage = document.getElementById('maintenance-message').value.trim();

    const button = document.getElementById('save-btn');
    button.disabled = true;
    button.textContent = 'Saving...';

    try {
      const response = await AdminApi.updateConfiguration(config);

      if (!response.success) {
        ApiClient.showToast(response.message || 'Failed to save configuration.', 'error');
        if (response.errors) ApiClient.showToast(response.errors.join(' '), 'error');
        return;
      }

      populate(response.data);
      setToggle('maintenance-mode', desiredMaintenance);
      setToggle('allow-registration', desiredRegistration);

      const togglesApplied = await syncToggles(maintenanceMessage);

      if (togglesApplied) {
        ApiClient.showToast('Configuration saved successfully.', 'success');
      } else {
        ApiClient.showToast(
          'Details saved, but one or more feature toggles failed to apply. Use Reload to see the current state.',
          'error'
        );
      }
    } catch (err) {
      ApiClient.showToast(err.message || 'Network error while saving configuration.', 'error');
    } finally {
      button.disabled = false;
      button.textContent = 'Save Configuration';
    }
  }

  function readToggle(id) {
    return document.getElementById(id).value === 'true';
  }

  function setToggle(id, value) {
    document.getElementById(id).value = String(value);
  }

  // The two toggles have dedicated PATCH endpoints, so they are applied only when
  // their value actually differs from what the server last reported. lastKnown is
  // advanced only on success, so a failed toggle is retried on the next save.
  async function syncToggles(maintenanceMessage) {
    const maintenance = readToggle('maintenance-mode');
    const registration = readToggle('allow-registration');
    let allApplied = true;

    if (maintenance !== lastKnown.maintenanceMode) {
      const result = await AdminApi.setMaintenanceMode(maintenance, maintenanceMessage);
      if (result && result.success) {
        lastKnown.maintenanceMode = maintenance;
      } else {
        ApiClient.showToast(
          (result && result.message) || 'Failed to update maintenance mode.',
          'error'
        );
        allApplied = false;
      }
    }

    if (registration !== lastKnown.allowRegistration) {
      const result = await AdminApi.setRegistrationMode(registration);
      if (result && result.success) {
        lastKnown.allowRegistration = registration;
      } else {
        ApiClient.showToast(
          (result && result.message) || 'Failed to update registration mode.',
          'error'
        );
        allApplied = false;
      }
    }

    return allApplied;
  }
});
