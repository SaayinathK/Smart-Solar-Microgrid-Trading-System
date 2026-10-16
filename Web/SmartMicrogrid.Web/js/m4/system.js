/* ==========================================================================
   Smart Microgrid Energy System - M4 System Health Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  document.getElementById('refresh-btn').addEventListener('click', loadHealth);
  loadHealth();

  async function loadHealth() {
    const button = document.getElementById('refresh-btn');
    button.disabled = true;
    button.textContent = 'Checking…';
    document.querySelector('.health-services').setAttribute('aria-busy', 'true');
    document.getElementById('health-error').hidden = true;

    try {
      const response = await AdminApi.getSystemHealth();

      if (response.success && response.data) {
        render(response.data);
      } else {
        showHealthError(response.message || 'Failed to load system health.');
      }
    } catch (err) {
      showHealthError(err.message || 'Network error while loading system health.');
    } finally {
      button.disabled = false;
      button.textContent = 'Refresh status';
      document.querySelector('.health-services').setAttribute('aria-busy', 'false');
    }
  }

  function showHealthError(message) {
    document.getElementById('health-error').textContent = `${message} Use Refresh status to try again.`;
    document.getElementById('health-error').hidden = false;
    document.getElementById('health-last-checked').textContent = 'Update unavailable';
    document.getElementById('health-details').innerHTML = '<div><dt>Runtime snapshot</dt><dd>Unavailable</dd></div>';
    for (const id of ['health-latency', 'health-memory', 'health-uptime']) document.getElementById(id).textContent = '—';
    for (const id of ['health-api', 'health-db', 'health-auth', 'health-server']) {
      document.getElementById(id).textContent = 'Unavailable';
      document.getElementById(id).closest('.alert-item').classList.remove('healthy', 'warning', 'critical');
    }
  }

  function render(health) {
    const apiIndicator = document.getElementById('health-api');
    const authIndicator = document.getElementById('health-auth');
    const serverIndicator = document.getElementById('health-server');
    apiIndicator.innerHTML = AdminUi.healthPill(health.api);
    authIndicator.innerHTML = AdminUi.healthPill(health.authentication);
    serverIndicator.innerHTML = AdminUi.healthPill(health.server);
    AdminUi.setHealthState(apiIndicator, health.api);
    AdminUi.setHealthState(authIndicator, health.authentication);
    AdminUi.setHealthState(serverIndicator, health.server);

    const databaseIndicator = document.getElementById('health-db');
    databaseIndicator.innerHTML =
      AdminUi.healthPill(health.database) +
      (health.databaseDetail ? `<br><small class="text-muted">${escapeHtml(health.databaseDetail)}</small>` : '');
    AdminUi.setHealthState(databaseIndicator, health.database);

    const rows = [
      ['Environment', escapeHtml(health.environment || '-')],
      ['Application Version', escapeHtml(health.version || '-')],
      ['Server Time', AdminUi.formatDateTime(health.serverTime)],
      ['JWT Issuer', escapeHtml(health.jwtIssuer || '-')]
    ];

    document.getElementById('health-details').innerHTML = rows
      .map(([label, value]) => `<div><dt>${label}</dt><dd>${value}</dd></div>`)
      .join('');
    document.getElementById('health-latency').textContent = health.databaseLatencyMs == null ? '—' : `${AdminUi.formatNumber(health.databaseLatencyMs, 1)} ms`;
    document.getElementById('health-memory').textContent = health.processMemoryBytes == null ? '—' : `${AdminUi.formatNumber(health.processMemoryBytes / 1048576, 1)} MB`;
    document.getElementById('health-uptime').textContent = formatUptime(health.uptime);
    document.getElementById('health-last-checked').textContent = `Checked ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
  }

  function formatUptime(uptime) {
    if (!uptime) return '-';
    // ASP.NET serialises TimeSpan as a string such as "00:12:03.123".
    if (typeof uptime === 'object' && uptime !== null) {
      const seconds = Math.floor((uptime.seconds || 0) + (uptime.days || 0) * 86400);
      return seconds > 0 ? `${AdminUi.formatNumber(seconds)} s` : '-';
    }
    const parts = String(uptime).match(/^(?:(\d+)\.)?(\d+):(\d+):(\d+)(?:\.\d+)?$/);
    if (!parts) return String(uptime);
    const [, days, hours, minutes, seconds] = parts;
    return [Number(days) ? `${Number(days)}d` : '', Number(hours) ? `${Number(hours)}h` : '', Number(minutes) ? `${Number(minutes)}m` : '', Number(seconds) ? `${Number(seconds)}s` : ''].filter(Boolean).join(' ') || 'Under 1 second';
  }
});
