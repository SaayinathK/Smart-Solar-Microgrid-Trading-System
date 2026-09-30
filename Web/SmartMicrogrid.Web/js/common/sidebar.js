/* ==========================================================================
   Smart Microgrid Energy System - Dynamic Sidebar Component
   ========================================================================== */

function renderAppLayout(activePage = 'dashboard', pageTitle = 'Dashboard') {
  const layoutContainer = document.getElementById('app-layout-wrapper');
  if (!layoutContainer) return;

  const user = SessionManager.getUser();
  const role = user ? user.role : 'Guest';
  const currentTheme = ThemeManager.getTheme();

  let roleClass = 'role-prosumer';
  if (role === 'Admin') roleClass = 'role-admin';
  else if (role === 'MicrogridOperator') roleClass = 'role-operator';

  const userInitial = user && user.firstName ? user.firstName.charAt(0).toUpperCase() : 'U';

  // Role-Specific Navigation Menu Configs
  const menuItems = getMenuItemsForRole(role, activePage);

  const innerContentHTML = layoutContainer.innerHTML;

  layoutContainer.innerHTML = `
    <div class="app-layout">
      
      <!-- Sidebar Navigation -->
      <aside id="sidebar" class="sidebar">
        <div class="sidebar-header">
          <svg class="sidebar-logo" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
          </svg>
          <span class="sidebar-title">SmartMicrogrid</span>
        </div>

        <div class="sidebar-user-card">
          <div class="sidebar-user-avatar-wrapper">
            <div class="sidebar-user-avatar">${userInitial}</div>
            <span class="status-dot"></span>
          </div>
          <div class="sidebar-user-info">
            <div class="sidebar-user-name">${user ? `${user.firstName} ${user.lastName}` : 'User'}</div>
            <span class="role-pill ${roleClass}">${role}</span>
          </div>
        </div>

        <nav class="sidebar-nav">
          ${menuItems}
        </nav>

        <div class="sidebar-footer">
          <button id="theme-toggle-btn" onclick="ThemeManager.toggleTheme()" class="theme-toggle-btn">
            ${currentTheme === 'dark' ? '☀️ Light Mode' : '🌙 Dark Mode'}
          </button>
          <button onclick="AuthGuard.logout()" class="btn btn-secondary btn-sm" style="width: 100%;">
            Logout
          </button>
        </div>
      </aside>

      <!-- Main Application Area -->
      <div class="app-main">
        <header class="app-header">
          <div class="header-left">
            <button class="mobile-toggle-btn" onclick="toggleSidebar()">
              <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
              </svg>
            </button>
            <div class="header-title">${pageTitle}</div>
          </div>

          <div class="header-right">
            <span class="role-pill ${roleClass}">${role} Portal</span>
          </div>
        </header>

        <main class="main-content">
          <div class="container">
            ${innerContentHTML}
          </div>
        </main>

        <footer class="footer">
          <div class="container">
            &copy; 2026 Smart Microgrid Energy System — Component 1 Infrastructure.
          </div>
        </footer>
      </div>

    </div>
  `;

  ThemeManager.updateToggleIcon(currentTheme);
}

function getMenuItemsForRole(role, activePage) {
  const icons = {
    dashboard: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"></path></svg>`,
    users: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"></path></svg>`,
    grid: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path></svg>`,
    battery: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"></path></svg>`,
    trading: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>`,
    reports: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"></path></svg>`
  };

  let sections = '';

  // Core Overview Navigation
  sections += `
    <div>
      <div class="sidebar-section-title">Navigation</div>
      <ul class="sidebar-menu">
        <li>
          <a href="/dashboard.html" class="sidebar-link ${activePage === 'dashboard' ? 'active' : ''}">
            ${icons.dashboard} <span>Dashboard Overview</span>
          </a>
        </li>
      </ul>
    </div>
  `;

  // M1 Microgrid Management Menu Section
  sections += `
    <div>
      <div class="sidebar-section-title">Microgrid Resources</div>
      <ul class="sidebar-menu">
        <li>
          <a href="/pages/M1/microgrids.html" class="sidebar-link ${activePage === 'microgrids' ? 'active' : ''}">
            ${icons.grid} <span>Solar Microgrids</span>
          </a>
        </li>
        <li>
          <a href="/pages/M1/energy-capacity.html" class="sidebar-link ${activePage === 'capacity' ? 'active' : ''}">
            ${icons.reports} <span>Energy Capacity</span>
          </a>
        </li>
        <li>
          <a href="/pages/M1/battery-storage.html" class="sidebar-link ${activePage === 'battery' ? 'active' : ''}">
            ${icons.battery} <span>Battery Storage</span>
          </a>
        </li>
        <li>
          <a href="/pages/M1/energy-slots.html" class="sidebar-link ${activePage === 'slots' ? 'active' : ''}">
            ${icons.trading} <span>Energy Slots</span>
          </a>
        </li>
        <li>
          <a href="/pages/M1/energy-availability.html" class="sidebar-link ${activePage === 'availability' ? 'active' : ''}">
            ${icons.trading} <span>Energy Availability</span>
          </a>
        </li>
        <li>
          <a href="/pages/M1/dashboard.html" class="sidebar-link ${activePage === 'm1-dashboard' ? 'active' : ''}">
            ${icons.dashboard} <span>Infrastructure Dashboard</span>
          </a>
        </li>
      </ul>
    </div>
  `;

  // M2 Energy Search & Reservations Section (All Roles)
  const reservationLabel = role === 'Admin' ? 'All Reservations Monitor' :
                           role === 'MicrogridOperator' ? 'Microgrid Reservations' :
                           role === 'MicrogridOperator' ? 'Reservations & Verification' :
                           'My Reservations & Claims';

  sections += `
    <div>
      <div class="sidebar-section-title">Energy Trading & Reservations</div>
      <ul class="sidebar-menu">
        <li>
          <a href="/pages/reservations/reservations.html" class="sidebar-link ${activePage === 'reservations' ? 'active' : ''}">
            ${icons.trading} <span>${reservationLabel}</span>
          </a>
        </li>
        ${role === 'Prosumer' ? `
        <li>
          <a href="/pages/M1/energy-availability.html" class="sidebar-link ${activePage === 'availability' ? 'active' : ''}">
            ${icons.grid} <span>Browse Available Energy</span>
          </a>
        </li>` : ''}
      </ul>
    </div>
  `;

  // Admin section
  if (role === 'Admin') {
    sections += `
      <div>
        <div class="sidebar-section-title">Administration</div>
        <ul class="sidebar-menu">
          <li>
            <a href="/pages/users/users.html" class="sidebar-link ${activePage === 'users' ? 'active' : ''}">
              ${icons.users} <span>User Management</span>
            </a>
          </li>
        </ul>
      </div>
    `;
  }

  return sections;
}

function toggleSidebar() {
  const sidebar = document.getElementById('sidebar');
  if (sidebar) {
    sidebar.classList.toggle('open');
  }
}

// Optional: Close sidebar on mobile when a link is clicked
document.addEventListener('click', function(event) {
  const sidebar = document.getElementById('sidebar');
  const toggleBtn = document.querySelector('.mobile-toggle-btn');
  
  if (window.innerWidth <= 992 && sidebar && sidebar.classList.contains('open')) {
    if (!sidebar.contains(event.target) && (!toggleBtn || !toggleBtn.contains(event.target))) {
      sidebar.classList.remove('open');
    }
  }
});