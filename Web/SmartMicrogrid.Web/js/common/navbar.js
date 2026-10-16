/* ==========================================================================
   Smart Microgrid Energy System - Dynamic Navbar Component
   ========================================================================== */

function renderNavbar(activePage = '') {
  const container = document.getElementById('navbar-container');
  if (!container) return;

  const user = SessionManager.getUser();
  const isAuthenticated = SessionManager.isAuthenticated();
  const isAdmin = SessionManager.isAdmin();

  let roleClass = 'role-prosumer';
  if (user?.role === 'Admin') roleClass = 'role-admin';
  else if (user?.role === 'MicrogridOperator') roleClass = 'role-operator';

  const currentTheme = ThemeManager.getTheme();
  const navbarScript = Array.from(document.scripts).find(script =>
    new URL(script.src, document.baseURI).pathname.endsWith('/js/common/navbar.js')
  );
  const logoUrl = new URL('../../assets/images/smart_micro.png',
    navbarScript?.src || new URL('js/common/navbar.js', document.baseURI)).href;

  container.innerHTML = `
    <nav class="navbar" aria-label="Main navigation">
      <div class="container navbar-container">
        <a href="/dashboard.html" class="navbar-brand">
          <img class="brand-logo" src="${logoUrl}" alt="" aria-hidden="true" width="40" height="40">
          <span>SmartMicrogrid</span>
        </a>

        ${isAuthenticated ? `
          <ul class="navbar-nav">
            <li><a href="/dashboard.html" class="nav-link ${activePage === 'dashboard' ? 'active' : ''}">Overview</a></li>
            ${isAdmin ? `<li><a href="/pages/users/users.html" class="nav-link ${activePage === 'users' ? 'active' : ''}">User Management</a></li>` : ''}
          </ul>

          <div class="user-menu">
            <button id="theme-toggle-btn" onclick="ThemeManager.toggleTheme()" class="theme-toggle-btn" title="Toggle Light / Dark Theme">
              ${currentTheme === 'dark' ? '☀️ Light Mode' : '🌙 Dark Mode'}
            </button>
            <div class="user-badge">
              <span class="user-name">${user ? `${user.firstName} ${user.lastName}` : 'User'}</span>
              <span class="role-pill ${roleClass}">${user?.role || 'Guest'}</span>
            </div>
            <button onclick="AuthGuard.logout()" class="btn btn-secondary btn-sm">Logout</button>
          </div>
        ` : `
          <div class="user-menu">
            <button id="theme-toggle-btn" onclick="ThemeManager.toggleTheme()" class="theme-toggle-btn" title="Toggle Light / Dark Theme">
              ${currentTheme === 'dark' ? '☀️ Light Mode' : '🌙 Dark Mode'}
            </button>
            <a href="/login.html" class="btn btn-secondary btn-sm">Sign In</a>
            <a href="/register.html" class="btn btn-primary btn-sm">Register</a>
          </div>
        `}
      </div>
    </nav>
  `;

  ThemeManager.updateToggleIcon(currentTheme);
}
