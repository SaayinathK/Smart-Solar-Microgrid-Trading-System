/* ==========================================================================
   Smart Microgrid Energy System - M4 Administration Dashboard Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  document.getElementById('refresh-btn').addEventListener('click', loadDashboard);
  loadDashboard();
});

async function loadDashboard() {
  const button = document.getElementById('refresh-btn');
  button.disabled = true;
  button.textContent = 'Refreshing...';

  try {
    const response = await AdminApi.getDashboard();

    if (response.success && response.data) {
      render(response.data);
    } else {
      ApiClient.showToast(response.message || 'Failed to load dashboard.', 'error');
    }
  } catch (err) {
    ApiClient.showToast(err.message || 'Network error while loading the dashboard.', 'error');
  } finally {
    button.disabled = false;
    button.textContent = 'Refresh';
  }
}

function render(data) {
  document.getElementById('platform-subtitle').textContent =
    `${data.platformName} — generated ${AdminUi.formatDateTime(data.generatedAt)}`;

  renderMaintenance(data.maintenanceMode);
  renderUsers(data.users);
  renderRoleDistribution(data.roleDistribution || []);
  renderPlatform(data.platform || {});
  renderHealth(data.health || {});
  renderActivity(data.activity || {});
}

function renderMaintenance(enabled) {
  const banner = document.getElementById('maintenance-banner');
  banner.style.display = enabled ? 'flex' : 'none';
}

function renderUsers(users) {
  document.getElementById('stat-total-users').textContent = AdminUi.formatNumber(users.totalUsers);
  document.getElementById('stat-active-users').textContent = AdminUi.formatNumber(users.activeUsers);
  document.getElementById('stat-inactive-users').textContent = AdminUi.formatNumber(users.inactiveUsers);
  document.getElementById('stat-suspended-users').textContent = AdminUi.formatNumber(users.suspendedUsers);
  document.getElementById('stat-pending-users').textContent = AdminUi.formatNumber(users.pendingUsers);
}

function renderRoleDistribution(rows) {
  const container = document.getElementById('role-distribution');

  if (!rows.length) {
    container.innerHTML = '<p class="text-muted">No roles defined.</p>';
    return;
  }

  const max = Math.max(...rows.map(r => r.userCount), 1);

  container.innerHTML = rows.map(row => `
    <div style="margin-bottom:1.1rem;">
      <div style="display:flex; justify-content:space-between; gap:1rem; margin-bottom:.4rem;">
        <strong style="font-size:.9rem;">${escapeHtml(row.role)}</strong>
        <span class="text-muted" style="font-size:.82rem;">
          ${AdminUi.formatNumber(row.userCount)} user(s) &middot; ${AdminUi.formatNumber(row.activeUserCount)} active
        </span>
      </div>
      <div style="height:9px; background:var(--bg-tertiary); border-radius:99px; overflow:hidden;">
        <div style="height:100%; width:${(row.userCount / max) * 100}%; background:var(--accent-primary); border-radius:99px;"></div>
      </div>
      <p class="text-muted" style="font-size:.76rem; margin-top:.35rem;">${escapeHtml(row.description)}</p>
    </div>
  `).join('');
}

function renderPlatform(platform) {
  setComponentCard('mg-card', 'mg-count', 'mg-note', platform.microgrids, platform.microgridCount);
  setComponentCard('cap-card', 'cap-value', 'cap-note', platform.microgrids, `${AdminUi.formatNumber(platform.totalAvailableCapacity, 1)} kWh`);
  setComponentCard('res-card', 'res-count', 'res-note', platform.reservations, platform.reservationCount);
  setComponentCard('txn-card', 'txn-count', 'txn-note', platform.transactions, platform.transactionCount);
}

function setComponentCard(cardId, valueId, noteId, stat, value) {
  const card = document.getElementById(cardId);
  const isLive = stat && stat.status === 'Live';

  card.classList.toggle('healthy', !!isLive);
  card.classList.toggle('warning', !isLive);

  document.getElementById(valueId).textContent = value;
  document.getElementById(noteId).textContent = isLive
    ? (stat.note || '')
    : (stat ? stat.note : 'Component data is not reachable.');
}

function renderHealth(health) {
  const apiIndicator = document.getElementById('health-api');
  const databaseIndicator = document.getElementById('health-db');
  const serverIndicator = document.getElementById('health-server');
  apiIndicator.innerHTML = AdminUi.healthPill(health.api);
  databaseIndicator.innerHTML =
    AdminUi.healthPill(health.database) +
    (health.databaseLatencyMs ? ` <span class="text-muted">(${AdminUi.formatNumber(health.databaseLatencyMs)} ms)</span>` : '');
  serverIndicator.innerHTML =
    AdminUi.healthPill(health.server) + ` <span class="text-muted">${escapeHtml(health.environment || '')}</span>`;
  AdminUi.setHealthState(apiIndicator, health.api);
  AdminUi.setHealthState(databaseIndicator, health.database);
  AdminUi.setHealthState(serverIndicator, health.server);
}

function renderActivity(activity) {
  const body = document.getElementById('recent-activity-body');
  const items = activity.recentActivity || [];

  document.getElementById('activity-subtitle').textContent =
    `Latest ${items.length} of ${AdminUi.formatNumber(activity.totalEvents)} audit record(s)`;

  if (!items.length) {
    body.innerHTML = AdminUi.emptyRow(6, 'No audit records yet. Administrative actions will appear here.');
    return;
  }

  body.innerHTML = items.map(item => `
    <tr>
      <td>${AdminUi.formatDateTime(item.timestamp)}</td>
      <td>${escapeHtml(item.userName || '-')}</td>
      <td>${escapeHtml(item.role || '-')}</td>
      <td>${escapeHtml(item.action)}</td>
      <td>${escapeHtml(item.module)}</td>
      <td>${AdminUi.statusBadge(item.status)}</td>
    </tr>
  `).join('');
}
