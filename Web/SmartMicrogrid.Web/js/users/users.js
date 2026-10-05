/* ==========================================================================
   Smart Microgrid Energy System - Users List Controller
   Module: M4 – Platform Administration & Prosumer Activation
   Author: K. Saayinath (IT23304338)
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  if (!AuthGuard.requireAuth('Admin')) return;

  const tableBody = document.getElementById('users-table-body');
  const searchInput = document.getElementById('search-input');
  const roleFilter = document.getElementById('role-filter');
  const statusFilter = document.getElementById('status-filter');

  let debounceTimer;

  // Initial Load
  loadUsers();
  refreshPendingCount();

  // Event Listeners
  searchInput.addEventListener('input', () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(loadUsers, 300);
  });

  roleFilter.addEventListener('change', loadUsers);
  statusFilter.addEventListener('change', () => {
    updateQuickTabs(statusFilter.value);
    loadUsers();
  });

  window.setQuickStatus = (status) => {
    statusFilter.value = status;
    updateQuickTabs(status);
    loadUsers();
  };

  function updateQuickTabs(status) {
    const tabs = {
      '': 'tab-all-users',
      'Pending': 'tab-pending-users',
      'Active': 'tab-active-users',
      'Inactive': 'tab-inactive-users',
      'Suspended': 'tab-suspended-users'
    };

    Object.entries(tabs).forEach(([val, tabId]) => {
      const el = document.getElementById(tabId);
      if (el) {
        if (val === (status || '')) {
          el.classList.add('active');
        } else {
          el.classList.remove('active');
        }
      }
    });

    const banner = document.getElementById('pending-queue-banner');
    if (banner) {
      if (status === 'Pending') {
        banner.classList.remove('d-none');
      } else {
        banner.classList.add('d-none');
      }
    }
  }

  async function refreshPendingCount() {
    try {
      const res = await UserApi.getAllUsers({ accountStatus: 'Pending' });
      if (res.success && Array.isArray(res.data)) {
        const count = res.data.length;
        const badge = document.getElementById('pending-count-badge');
        if (badge) {
          badge.textContent = count;
          if (count > 0) {
            badge.classList.remove('d-none');
          } else {
            badge.classList.add('d-none');
          }
        }
      }
    } catch (_) {}
  }

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
            ${escapeUserHtml(response.message || 'Failed to load users.')}
            </td>
          </tr>
        `;
      }
    } catch (err) {
      tableBody.innerHTML = `
        <tr>
          <td colspan="8" style="text-align: center; color: var(--accent-rose); padding: 2rem;">
            ${escapeUserHtml(err.message || 'Error connecting to API.')}
          </td>
        </tr>
      `;
    }

    refreshPendingCount();
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
      const safeStatus = ['Active', 'Pending', 'Inactive', 'Suspended'].includes(accountStatus) ? accountStatus : 'Unknown';

      let statusBadge = `<span class="status-badge">${escapeUserHtml(safeStatus)}</span>`;
      if (accountStatus === 'Active') {
        statusBadge = '<span class="status-badge status-active"><span class="status-dot"></span> Active</span>';
      } else if (accountStatus === 'Pending') {
        statusBadge = '<span class="badge bg-warning text-dark px-2 py-1">⏳ Pending Activation</span>';
      } else if (accountStatus === 'Inactive') {
        statusBadge = '<span class="badge bg-secondary px-2 py-1">Inactive / Deactivated</span>';
      } else if (accountStatus === 'Suspended') {
        statusBadge = '<span class="badge bg-danger px-2 py-1">Suspended</span>';
      }

      const createdDate = new Date(user.createdAt).toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });
      const userId = encodeURIComponent(String(user.id || ''));

      let primaryAction = '';
      if (accountStatus === 'Active') {
        primaryAction = `<button onclick="changeUserStatus('${userId}', 'Inactive')" class="btn btn-outline-danger btn-sm">Deactivate</button>`;
      } else if (accountStatus === 'Pending') {
        primaryAction = `<button onclick="changeUserStatus('${user.id}', 'Active')" class="btn btn-success btn-sm font-weight-bold">✓ Activate</button>`;
      } else {
        primaryAction = `<button onclick="changeUserStatus('${userId}', 'Active')" class="btn btn-primary btn-sm">Activate</button>`;
      }
      if (accountStatus === 'Pending') {
        statusBadge = '<span class="status-badge status-pending">Pending activation</span>';
        primaryAction = `<button onclick="changeUserStatus('${userId}', 'Active')" class="btn btn-primary btn-sm">Activate</button>`;
      }

      return `
        <tr>
          <td>
            <strong>${escapeUserHtml(`${user.firstName || ''} ${user.lastName || ''}`.trim() || 'Unnamed user')}</strong>
          </td>
          <td><code>${escapeUserHtml(user.nic || '-')}</code></td>
          <td>${escapeUserHtml(user.email || '-')}</td>
          <td>${escapeUserHtml(user.phoneNumber || '-')}</td>
          <td><span class="role-pill ${roleClass}">${escapeUserHtml(user.role || 'Unknown')}</span></td>
          <td>${statusBadge}</td>
          <td>${createdDate}</td>
          <td>
            <div class="table-actions">
              <a href="user-details.html?id=${userId}" class="btn btn-secondary btn-sm">View</a>
              <a href="edit-user.html?id=${userId}" class="btn btn-secondary btn-sm">Edit</a>
              ${primaryAction}
              <select class="form-select btn-sm" style="width: auto; min-width: 118px;"
                      onchange="handleUserAction('${userId}', this.value); this.value = '';">
                <option value="">More...</option>
                <option value="Suspended">Suspend</option>
                <option value="Pending">Set Pending</option>
                ${accountStatus === 'Active' ? '<option value="Inactive">Deactivate</option>' : ''}
                ${accountStatus !== 'Active' ? '<option value="Active">Activate</option>' : ''}
                <option value="__delete">Delete user...</option>
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
      Active: 'activate and approve',
      Inactive: 'deactivate',
      Suspended: 'suspend',
      Pending: 'mark as pending review'
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

  window.handleUserAction = (userId, action) => {
    if (action === '__delete') return window.deleteUserAccount(userId);
    return window.changeUserStatus(userId, action);
  };

  window.deleteUserAccount = async (userId) => {
    if (!confirm('Permanently delete this user account? This action cannot be undone.')) return;

    try {
      const response = await UserApi.deleteUser(userId);
      if (response.success) {
        ApiClient.showToast(response.message || 'User account deleted.', 'success');
        await loadUsers();
      } else {
        ApiClient.showToast(response.message || 'User account could not be deleted.', 'error');
      }
    } catch (err) {
      ApiClient.showToast(err.message || 'Server error while deleting the user account.', 'error');
    }
  };
});

function escapeUserHtml(value) {
  return String(value ?? '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]));
}
