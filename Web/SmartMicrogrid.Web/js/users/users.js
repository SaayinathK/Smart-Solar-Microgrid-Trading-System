/* ==========================================================================
   Smart Microgrid Energy System - Users List Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  AuthGuard.requireAuth('Admin');

  const tableBody = document.getElementById('users-table-body');
  const searchInput = document.getElementById('search-input');
  const roleFilter = document.getElementById('role-filter');
  const statusFilter = document.getElementById('status-filter');

  let debounceTimer;

  // Initial Load
  loadUsers();

  // Event Listeners
  searchInput.addEventListener('input', () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(loadUsers, 300);
  });

  roleFilter.addEventListener('change', loadUsers);
  statusFilter.addEventListener('change', loadUsers);

  async function loadUsers() {
    tableBody.innerHTML = `
      <tr>
        <td colspan="8" style="text-align: center; color: var(--text-muted); padding: 2rem;">
          Loading system users...
        </td>
      </tr>
    `;

    const search = searchInput.value.trim();
    const role = roleFilter.value;
    const status = statusFilter.value;

    const params = {};
    if (search) params.search = search;
    if (role) params.role = role;
    if (status) params.accountStatus = status;

    try {
      const response = await UserApi.getAllUsers(params);
      if (response.success && response.data) {
        renderUserTable(response.data);
      } else {
        tableBody.innerHTML = `
          <tr>
            <td colspan="8" style="text-align: center; color: var(--accent-rose); padding: 2rem;">
              ${response.message || 'Failed to load users.'}
            </td>
          </tr>
        `;
      }
    } catch (err) {
      tableBody.innerHTML = `
        <tr>
          <td colspan="8" style="text-align: center; color: var(--accent-rose); padding: 2rem;">
            ${err.message || 'Error connecting to API.'}
          </td>
        </tr>
      `;
    }
  }

  function renderUserTable(users) {
    if (!users || users.length === 0) {
      tableBody.innerHTML = `
        <tr>
          <td colspan="8" style="text-align: center; color: var(--text-muted); padding: 2rem;">
            No user accounts found matching criteria.
          </td>
        </tr>
      `;
      return;
    }

    tableBody.innerHTML = users.map(user => {
      let roleClass = 'role-prosumer';
      if (user.role === 'Admin') roleClass = 'role-admin';
      else if (user.role === 'MicrogridOperator') roleClass = 'role-operator';

      const accountStatus = user.accountStatus
        || (user.isActive ? 'Active' : 'Inactive');

      const statusBadge = accountStatus === 'Active'
        ? '<span class="status-badge status-active"><span class="status-dot"></span> Active</span>'
        : `<span class="status-badge status-${accountStatus.toLowerCase()}">${accountStatus}</span>`;

      const createdDate = new Date(user.createdAt).toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });

      const primaryAction = accountStatus === 'Active'
        ? `<button onclick="changeUserStatus('${user.id}', 'Inactive')" class="btn btn-danger btn-sm">Deactivate</button>`
        : `<button onclick="changeUserStatus('${user.id}', 'Active')" class="btn btn-primary btn-sm">Activate</button>`;

      return `
        <tr>
          <td>
            <strong>${user.firstName} ${user.lastName}</strong>
          </td>
          <td><code>${user.nic || '-'}</code></td>
          <td>${user.email}</td>
          <td>${user.phoneNumber || '-'}</td>
          <td><span class="role-pill ${roleClass}">${user.role}</span></td>
          <td>${statusBadge}</td>
          <td>${createdDate}</td>
          <td>
            <div class="table-actions">
              <a href="user-details.html?id=${user.id}" class="btn btn-secondary btn-sm">View</a>
              <a href="edit-user.html?id=${user.id}" class="btn btn-secondary btn-sm">Edit</a>
              ${primaryAction}
              <select class="form-select btn-sm" style="width: auto; min-width: 118px;"
                      onchange="changeUserStatus('${user.id}', this.value); this.value = '';">
                <option value="">More...</option>
                <option value="Suspended">Suspend</option>
                <option value="Pending">Set Pending</option>
                ${accountStatus === 'Active' ? '<option value="Inactive">Deactivate</option>' : ''}
                ${accountStatus !== 'Active' ? '<option value="Active">Activate</option>' : ''}
              </select>
            </div>
          </td>
        </tr>
      `;
    }).join('');
  }

  window.changeUserStatus = async (userId, newStatus) => {
    if (!newStatus) return;

    const actionText = {
      Active: 'activate',
      Inactive: 'deactivate',
      Suspended: 'suspend',
      Pending: 'move to pending review'
    }[newStatus] || `set to ${newStatus}`;

    if (!confirm(`Are you sure you want to ${actionText} this user account?`)) return;

    try {
      const response = await UserApi.updateAccountStatus(userId, newStatus);
      if (response.success) {
        ApiClient.showToast(response.message || `User status updated!`, 'success');
        loadUsers();
      } else {
        ApiClient.showToast(response.message || `Failed to update status.`, 'error');
      }
    } catch (err) {
      ApiClient.showToast(err.message || 'Server error occurred.', 'error');
    }
  };
});
