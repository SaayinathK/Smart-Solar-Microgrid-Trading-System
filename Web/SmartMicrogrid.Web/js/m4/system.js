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

    try {
      const response = await AdminApi.getSystemHealth();

      if (response.success && response.data) {
        render(response.data);
      } else {
        ApiClient.showToast(response.message || 'Failed to load system health.', 'error');
      }
    } catch (err) {
      ApiClient.showToast(err.message || 'Network error while loading system health.', 'error');
    } finally {
      button.disabled = false;
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
      ['Database Latency', `${AdminUi.formatNumber(health.databaseLatencyMs, 1)} ms`],
      ['Process Memory', `${AdminUi.formatNumber((health.processMemoryBytes || 0) / 1048576, 1)} MB`],
      ['Uptime', formatUptime(health.uptime)],
      ['JWT Issuer', escapeHtml(health.jwtIssuer || '-')]
    ];

    document.getElementById('health-details').innerHTML = rows
      .map(([label, value]) => `<tr><td><strong>${label}</strong></td><td>${value}</td></tr>`)
      .join('');
  }

  function formatUptime(uptime) {
    if (!uptime) return '-';
    // ASP.NET serialises TimeSpan as a string such as "00:12:03.123".
    if (typeof uptime === 'object' && uptime !== null) {
      const seconds = Math.floor((uptime.seconds || 0) + (uptime.days || 0) * 86400);
      return seconds > 0 ? `${AdminUi.formatNumber(seconds)} s` : '-';
    }
    return escapeHtml(String(uptime));
  }
});
