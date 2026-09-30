/* ==========================================================================
   Smart Microgrid Energy System - User Details Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', async () => {
  AuthGuard.requireAuth();

  const urlParams = new URLSearchParams(window.location.search);
  let userId = urlParams.get('id');

  // If no ID provided, default to currently logged in user
  const currentUser = SessionManager.getUser();
  if (!userId && currentUser) {
    userId = currentUser.id;
  }

  const cardContainer = document.getElementById('details-card');
  const editBtn = document.getElementById('edit-user-btn');

  if (!userId) {
    cardContainer.innerHTML = `<div class="alert alert-danger active">No User ID provided.</div>`;
    return;
  }

  editBtn.href = `edit-user.html?id=${userId}`;

  try {
    const response = await UserApi.getUserById(userId);
    if (response.success && response.data) {
      renderUserDetails(response.data);
    } else {
      cardContainer.innerHTML = `<div class="alert alert-danger active">${response.message || 'User not found.'}</div>`;
    }
  } catch (err) {
    cardContainer.innerHTML = `<div class="alert alert-danger active">${err.message || 'Error fetching user details.'}</div>`;
  }

  function renderUserDetails(user) {
    let roleClass = 'role-prosumer';
    if (user.role === 'Admin') roleClass = 'role-admin';
    else if (user.role === 'MicrogridOperator') roleClass = 'role-operator';

    const statusBadge = user.isActive
      ? '<span class="status-badge status-active">● Active Account</span>'
      : '<span class="status-badge status-inactive">○ Deactivated</span>';

    cardContainer.innerHTML = `
      <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 1.5rem;">
        <div>
          <h2 style="font-size: 1.75rem;">${user.firstName} ${user.lastName}</h2>
          <p style="color: var(--text-secondary); margin-top: 0.2rem;">${user.email}</p>
        </div>
        <div>
          <span class="role-pill ${roleClass}" style="font-size: 0.85rem; padding: 0.35rem 0.85rem;">${user.role}</span>
        </div>
      </div>

      <div class="details-grid">
        <div class="detail-item">
          <div class="detail-label">MongoDB Document Id</div>
          <div class="detail-value" style="font-family: monospace; font-size: 0.95rem; color: var(--accent-cyan);">${user.id}</div>
        </div>

        <div class="detail-item">
          <div class="detail-label">Account Status</div>
          <div class="detail-value" style="margin-top: 0.4rem;">${statusBadge}</div>
        </div>

        <div class="detail-item">
          <div class="detail-label">Phone Number</div>
          <div class="detail-value">${user.phoneNumber || 'Not Provided'}</div>
        </div>

        <div class="detail-item">
          <div class="detail-label">Assigned System Role</div>
          <div class="detail-value">${user.role}</div>
        </div>

        <div class="detail-item">
          <div class="detail-label">Created Timestamp</div>
          <div class="detail-value" style="font-size: 0.95rem;">${new Date(user.createdAt).toLocaleString()}</div>
        </div>

        <div class="detail-item">
          <div class="detail-label">Last Updated Timestamp</div>
          <div class="detail-value" style="font-size: 0.95rem;">${new Date(user.updatedAt).toLocaleString()}</div>
        </div>
      </div>
    `;
  }
});
