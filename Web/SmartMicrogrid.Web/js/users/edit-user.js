/* ==========================================================================
   Smart Microgrid Energy System - Edit User Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', async () => {
  if (!AuthGuard.requireAuth()) return;

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
  let loadedRole = null;
  let loadedAccountStatus = null;

  if (!userId) {
    showError('Select a user account before opening this page.');
    return;
  }

  if (isAdmin) {
    adminFields.style.display = 'grid';
  }
  pwdForm.closest('.glass-card').hidden = isAdmin && userId !== currentUser?.id;

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
    document.getElementById('nic').value = user.nic || '';

    if (isAdmin) {
      loadedRole = user.role;
      document.getElementById('role').value = user.role;
      loadedAccountStatus = user.accountStatus || (user.isActive ? 'Active' : 'Inactive');
      document.getElementById('isActive').value = loadedAccountStatus;
    }
  }

  // Submit Profile Changes
  editForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    alertBox.classList.remove('active');

    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();
    const phoneNumber = document.getElementById('phoneNumber').value.trim();
    const nic = document.getElementById('nic').value.trim().toUpperCase();

    const payload = { firstName, lastName, phoneNumber, nic };

    try {
      let response;
      if (isAdmin) {
        response = await UserApi.updateUser(userId, payload);
        const desiredRole = document.getElementById('role').value;
        const desiredStatus = document.getElementById('isActive').value;

        if (response.success && desiredRole !== loadedRole) {
          const roleResponse = await UserApi.updateRole(userId, desiredRole);
          if (!roleResponse.success) {
            showError(`Profile changes were saved, but the role was not updated: ${roleResponse.message || 'Please retry the role change.'}`);
            return;
          }
          response = roleResponse;
          loadedRole = desiredRole;
        }

        if (response.success && desiredStatus !== loadedAccountStatus) {
          const statusResponse = await UserApi.updateAccountStatus(userId, desiredStatus);
          if (!statusResponse.success) {
            showError(`Profile changes were saved, but the account status was not updated: ${statusResponse.message || 'Please retry the status change.'}`);
            return;
          }
          response = statusResponse;
          loadedAccountStatus = desiredStatus;
        }
      } else {
        response = await UserApi.updateProfile(payload);
      }

      if (response.success && currentUser?.id === userId) {
        SessionManager.updateUser(response.data);
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
