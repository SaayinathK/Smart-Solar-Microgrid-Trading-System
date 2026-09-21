/* ==========================================================================
   Smart Microgrid Energy System - Edit User Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', async () => {
  AuthGuard.requireAuth();
  renderNavbar('users');

  const urlParams = new URLSearchParams(window.location.search);
  let userId = urlParams.get('id');

  const currentUser = SessionManager.getUser();
  const isAdmin = SessionManager.isAdmin();

  if (!userId && currentUser) {
    userId = currentUser.id;
  }

  const alertBox = document.getElementById('alert-box');
  const pwdAlertBox = document.getElementById('pwd-alert-box');
  const editForm = document.getElementById('edit-user-form');
  const pwdForm = document.getElementById('change-pwd-form');
  const adminFields = document.getElementById('admin-fields-container');

  if (isAdmin) {
    adminFields.style.display = 'grid';
  }

  // Fetch Existing User
  try {
    const response = await UserApi.getUserById(userId);
    if (response.success && response.data) {
      populateForm(response.data);
    } else {
      showError(response.message || 'User account not found.');
    }
  } catch (err) {
    showError(err.message || 'Error fetching user data.');
  }

  function populateForm(user) {
    document.getElementById('firstName').value = user.firstName;
    document.getElementById('lastName').value = user.lastName;
    document.getElementById('email').value = user.email;
    document.getElementById('phoneNumber').value = user.phoneNumber || '';

    if (isAdmin) {
      document.getElementById('role').value = user.role;
      document.getElementById('isActive').value = user.isActive ? 'true' : 'false';
    }
  }

  // Submit Profile Changes
  editForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    alertBox.classList.remove('active');

    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();
    const phoneNumber = document.getElementById('phoneNumber').value.trim();

    const payload = { firstName, lastName, phoneNumber };

    if (isAdmin) {
      payload.role = document.getElementById('role').value;
      payload.isActive = document.getElementById('isActive').value === 'true';
    }

    try {
      let response;
      if (isAdmin && userId !== currentUser?.id) {
        response = await UserApi.updateUser(userId, payload);
      } else {
        response = await UserApi.updateProfile(payload);
        if (response.success) {
          SessionManager.updateUser(response.data);
        }
      }

      if (response.success) {
        ApiClient.showToast('User profile updated successfully!', 'success');
        setTimeout(() => {
          if (isAdmin) {
            window.location.href = 'users.html';
          } else {
            window.location.href = '../../dashboard.html';
          }
        }, 1200);
      } else {
        showError(response.message || 'Failed to update user.');
      }
    } catch (err) {
      showError(err.message || 'Error saving user modifications.');
    }
  });

  // Submit Password Change
  pwdForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    pwdAlertBox.classList.remove('active');

    const currentPassword = document.getElementById('currentPassword').value;
    const newPassword = document.getElementById('newPassword').value;
    const confirmNewPassword = document.getElementById('confirmNewPassword').value;

    if (newPassword !== confirmNewPassword) {
      showPwdError('New password and confirmation do not match.');
      return;
    }

    try {
      const response = await AuthApi.changePassword({
        currentPassword,
        newPassword,
        confirmNewPassword
      });

      if (response.success) {
        ApiClient.showToast('Password updated successfully!', 'success');
        pwdForm.reset();
      } else {
        showPwdError(response.message || 'Failed to update password.');
      }
    } catch (err) {
      showPwdError(err.message || 'Error changing password.');
    }
  });

  function showError(msg) {
    alertBox.textContent = msg;
    alertBox.classList.add('active');
  }

  function showPwdError(msg) {
    pwdAlertBox.textContent = msg;
    pwdAlertBox.classList.add('active');
  }
});
