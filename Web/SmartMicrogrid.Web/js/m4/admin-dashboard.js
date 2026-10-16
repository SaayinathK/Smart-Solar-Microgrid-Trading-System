/* ==========================================================================
   Smart Microgrid Energy System - M4 Administration Dashboard Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  const user = SessionManager.getUser();
  document.getElementById('admin-display-name').textContent =
    [user?.firstName, user?.lastName].filter(Boolean).join(' ') || 'Administrator';

  document.getElementById('refresh-btn').addEventListener('click', () => {
    loadDashboard();
    document.dispatchEvent(new Event('dashboard:refresh'));
  });
  loadDashboard();
});

async function loadDashboard() {
  const button = document.getElementById('refresh-btn');
  button.disabled = true;
  button.textContent = 'Refreshing...';

  await Promise.all([loadAdminSummary(), InfrastructureOverview.refresh()]);
  button.disabled = false;
  button.textContent = 'Refresh data';
}

async function loadAdminSummary() {
  const error = document.getElementById('admin-dashboard-error');
  error.hidden = true;

  try {
    const response = await AdminApi.getDashboard();

    if (response.success && response.data) {
      render(response.data);
    } else {
      throw new Error(response.message || 'Failed to load dashboard.');
    }
  } catch (err) {
    error.textContent = `${err.message || 'Unable to load the platform summary.'} Displayed values may be out of date. Refresh to try again.`;
    error.hidden = false;
    document.getElementById('platform-subtitle').textContent = 'Platform summary unavailable. Network data is loaded separately.';
    document.getElementById('recent-activity-body').innerHTML = AdminUi.emptyRow(6, 'Activity could not be refreshed. Open the activity log or try again.');
  }
}

function render(data) {
  document.getElementById('platform-subtitle').textContent =
    `${data.platformName} — generated ${AdminUi.formatDateTime(data.generatedAt)}`;

  renderMaintenance(data.maintenanceMode);
  renderUsers(data.users || {});
  renderRoleDistribution(data.roleDistribution || []);
  renderPlatform(data.platform || {});
  renderHealth(data.health || {});
  renderActivity(data.activity || {});
}

function renderMaintenance(enabled) {
  const banner = document.getElementById('maintenance-banner');
  banner.hidden = !enabled;
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
  setComponentCard('res-card', 'res-count', 'res-note', platform.reservations, platform.reservationCount);
  setComponentCard('txn-card', 'txn-count', 'txn-note', platform.transactions, platform.transactionCount);
  if (platform.reservations?.status === 'Live') {
    document.getElementById('res-note').textContent = `${AdminUi.formatNumber(platform.pendingReservationCount)} pending approval`;
  }
  if (platform.transactions?.status === 'Live') {
    document.getElementById('txn-note').textContent = `${AdminUi.formatNumber(platform.completedTransactionCount)} completed transfers`;
  }
}

function setComponentCard(cardId, valueId, noteId, stat, value) {
  const card = document.getElementById(cardId);
  const isLive = stat && stat.status === 'Live';

  card.classList.toggle('healthy', !!isLive);
  card.classList.toggle('warning', !isLive);

  document.getElementById(valueId).textContent = isLive ? AdminUi.formatNumber(value) : 'Unavailable';
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
