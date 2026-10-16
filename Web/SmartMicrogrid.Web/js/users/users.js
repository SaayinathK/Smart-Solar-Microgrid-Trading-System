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
  const resultsCount = document.getElementById('user-results-count');
  const refreshButton = document.getElementById('refresh-users');
  const directory = document.querySelector('.user-directory');

  let debounceTimer;
  let loadRequestId = 0;

  // Dashboard approval shortcuts open the existing filtered account queue.
  const initialStatus = new URLSearchParams(window.location.search).get('status');
  if (['Active', 'Pending', 'Inactive', 'Suspended'].includes(initialStatus)) {
    statusFilter.value = initialStatus;
    updateQuickTabs(initialStatus);
  }

  // Initial Load
  loadUsers();

  // Event Listeners
  searchInput.addEventListener('input', () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(loadUsers, 300);
  });

  roleFilter.addEventListener('change', loadUsers);
  refreshButton.addEventListener('click', loadUsers);
  document.getElementById('clear-user-filters').addEventListener('click', () => {
    clearTimeout(debounceTimer);
    searchInput.value = '';
    roleFilter.value = '';
    statusFilter.value = '';
    updateQuickTabs('');
    loadUsers();
  });
  tableBody.addEventListener('click', event => {
    const button = event.target.closest('[data-user-status]');
    if (button) window.changeUserStatus(button.dataset.userId, button.dataset.userStatus);
  });
  tableBody.addEventListener('change', event => {
    const select = event.target.closest('[data-user-actions]');
    if (!select) return;
    window.handleUserAction(select.dataset.userId, select.value);
    select.value = '';
  });
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
        el.setAttribute('aria-pressed', String(val === (status || '')));
        if (val === (status || '')) {
          el.classList.add('active');
        } else {
          el.classList.remove('active');
        }
      }
    });

    const banner = document.getElementById('pending-queue-banner');
    if (banner) {
      banner.hidden = status !== 'Pending';
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
    const requestId = ++loadRequestId;
    refreshButton.disabled = true;
    directory.setAttribute('aria-busy', 'true');
    resultsCount.textContent = 'Loading accounts…';
    tableBody.innerHTML = `
      <tr>
        <td colspan="6" class="user-table-message">
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
      if (requestId !== loadRequestId) return;
      if (response.success && Array.isArray(response.data)) {
        renderUserTable(response.data);
        const count = response.data.length;
        resultsCount.textContent = `${count} ${count === 1 ? 'account' : 'accounts'}${Object.keys(params).length ? ' matching your filters' : ''}`;
      } else {
        resultsCount.textContent = 'Accounts unavailable';
        tableBody.innerHTML = `
          <tr>
            <td colspan="6" class="user-table-message">
            ${escapeUserHtml(response.message || 'Failed to load users.')}
            </td>
          </tr>
        `;
      }
    } catch (err) {
      if (requestId !== loadRequestId) return;
      resultsCount.textContent = 'Accounts unavailable';
      tableBody.innerHTML = `
        <tr>
          <td colspan="6" class="user-table-message">
            ${escapeUserHtml(err.message || 'Error connecting to API.')}
          </td>
        </tr>
      `;
    } finally {
      if (requestId === loadRequestId) {
        directory.setAttribute('aria-busy', 'false');
        refreshButton.disabled = false;
        refreshPendingCount();
      }
    }
  }

  function renderUserTable(users) {
    if (!users || users.length === 0) {
      tableBody.innerHTML = `
        <tr>
          <td colspan="6" class="user-table-message">
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

      const statusBadge = `<span class="status-badge status-${safeStatus.toLowerCase()}">${safeStatus === 'Pending' ? 'Pending activation' : escapeUserHtml(safeStatus)}</span>`;

      const date = user.createdAt ? new Date(user.createdAt) : null;
      const createdDate = date && !Number.isNaN(date.getTime()) ? date.toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      }) : 'Not available';
      const userId = encodeURIComponent(String(user.id || ''));
      const safeUserId = escapeUserHtml(user.id || '');
      const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'Unnamed user';
      const initials = [user.firstName, user.lastName].filter(Boolean).map(part => String(part).charAt(0)).join('').toUpperCase() || 'U';
      const roleLabel = { Admin: 'Administrator', MicrogridOperator: 'Microgrid operator', Prosumer: 'Prosumer' }[user.role] || 'Unknown';

      let primaryAction = '';
      if (accountStatus !== 'Active') {
        primaryAction = `<button type="button" data-user-id="${safeUserId}" data-user-status="Active" class="btn btn-primary btn-sm" aria-label="Activate ${escapeUserHtml(fullName)}">Activate</button>`;
      }

      return `
        <tr>
          <td data-label="Account">
            <div class="user-account-identity"><span class="user-account-avatar" aria-hidden="true">${escapeUserHtml(initials)}</span><div><strong class="user-account-name">${escapeUserHtml(fullName)}</strong><span class="user-account-nic">NIC ${escapeUserHtml(user.nic || 'not provided')}</span></div></div>
          </td>
          <td data-label="Contact"><span class="user-account-email">${escapeUserHtml(user.email || 'Not provided')}</span><span class="user-account-phone">${escapeUserHtml(user.phoneNumber || 'No phone number')}</span></td>
          <td data-label="Role"><span class="role-pill ${roleClass}">${roleLabel}</span></td>
          <td data-label="Status">${statusBadge}</td>
          <td data-label="Joined">${createdDate}</td>
          <td data-label="Actions">
            <div class="user-row-actions">
              <a href="${getWebPageUrl(`user-details.html?id=${userId}`)}" class="btn btn-secondary btn-sm" aria-label="View ${escapeUserHtml(fullName)}">View</a>
              ${primaryAction}
              <select class="form-select" data-user-actions data-user-id="${safeUserId}" aria-label="More actions for ${escapeUserHtml(fullName)}">
                <option value="">More</option>
                <option value="__edit">Edit account</option>
                ${accountStatus !== 'Suspended' ? '<option value="Suspended">Suspend</option>' : ''}
                ${accountStatus !== 'Pending' ? '<option value="Pending">Set pending</option>' : ''}
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
    if (action === '__edit') {
      window.location.href = getWebPageUrl(`edit-user.html?id=${encodeURIComponent(userId)}`);
      return;
    }
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
