/* ==========================================================================
   Smart Microgrid Energy System - Create User Controller
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  AuthGuard.requireAuth('Admin');

  const form = document.getElementById('create-user-form');
  const alertBox = document.getElementById('alert-box');
  const btnSubmit = document.getElementById('btn-submit');

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    alertBox.classList.remove('active');

    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();
    const email = document.getElementById('email').value.trim();
    const phoneNumber = document.getElementById('phoneNumber').value.trim();
    const nic = document.getElementById('nic').value.trim().toUpperCase();
    const role = document.getElementById('role').value;
    const isActive = document.getElementById('isActive').value === 'true';
    const password = document.getElementById('password').value;

    btnSubmit.disabled = true;
    btnSubmit.textContent = 'Creating User...';

    try {
      const response = await UserApi.createUser({
        firstName,
        lastName,
        email,
        phoneNumber,
        nic,
        role,
        isActive,
        password
      });

      if (response.success) {
        ApiClient.showToast('User created successfully!', 'success');
        setTimeout(() => {
          window.location.href = 'users.html';
        }, 1200);
      } else {
        showError(response.message || 'Failed to create user.');
      }
    } catch (err) {
      showError(err.message || 'Server error occurred while creating user.');
    } finally {
      btnSubmit.disabled = false;
      btnSubmit.textContent = 'Create User';
    }
  });

  function showError(msg) {
    alertBox.textContent = msg;
    alertBox.classList.add('active');
  }
});
