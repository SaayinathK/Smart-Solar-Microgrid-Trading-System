/* ==========================================================================
   Smart Microgrid Energy System - M4 Role Management Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  document.getElementById('refresh-btn').addEventListener('click', loadRoles);
  loadRoles();

  async function loadRoles() {
    const body = document.getElementById('roles-body');
    body.innerHTML = AdminUi.loadingRow(5);

    try {
      const response = await AdminApi.getRoleReport();

      if (response.success && Array.isArray(response.data)) {
        render(response.data);
      } else {
        body.innerHTML = AdminUi.emptyRow(5, response.message || 'Failed to load roles.');
      }
    } catch (err) {
      body.innerHTML = AdminUi.emptyRow(5, err.message || 'Network error while loading roles.');
    }
  }

  function render(roles) {
    const body = document.getElementById('roles-body');

    if (!roles.length) {
      body.innerHTML = AdminUi.emptyRow(5, 'No roles are defined on the platform.');
      return;
    }

    body.innerHTML = roles.map(role => `
      <tr>
        <td><strong>${escapeHtml(role.role)}</strong></td>
        <td style="max-width: 460px;">${escapeHtml(role.description)}</td>
        <td>${AdminUi.formatNumber(role.userCount)}</td>
        <td>${AdminUi.formatNumber(role.activeUserCount)}</td>
        <td>
          <span class="status-badge status-approved">Active</span>
        </td>
      </tr>
    `).join('');
  }
});
