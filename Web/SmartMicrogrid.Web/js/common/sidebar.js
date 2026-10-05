/* ==========================================================================
   Smart Microgrid Energy System - Dynamic Sidebar Component
   ========================================================================== */

function getWebAppUrl(path) {
  const sidebarScript = Array.from(document.scripts).find(script =>
    new URL(script.src, document.baseURI).pathname.endsWith('/js/common/sidebar.js')
  );
  const appBase = sidebarScript
    ? new URL('../../', sidebarScript.src)
    : new URL('.', window.location.href);

  return new URL(path, appBase).href;
}

function renderAppLayout(activePage = 'dashboard', pageTitle = 'Overview') {
  const layoutContainer = document.getElementById('app-layout-wrapper');
  if (!layoutContainer || layoutContainer.dataset.layoutReady) return;
  layoutContainer.dataset.layoutReady = 'true';

  const rawUser = SessionManager.getUser();
  const escape = value => String(value ?? '').replace(/[&<>"']/g, char => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
  const user = rawUser ? { ...rawUser, firstName: escape(rawUser.firstName), lastName: escape(rawUser.lastName), nic: escape(rawUser.nic) } : null;
  const roleLabel = rawUser?.role === 'MicrogridOperator' ? 'Microgrid operator' : rawUser?.role === 'Admin' ? 'Administrator' : 'Prosumer';
  const role = user ? user.role : 'Guest';
  if (role === 'Admin' && (activePage.startsWith('m4-') || activePage === 'users')) {
    document.body.classList.add('admin-workspace');
  }
  const currentTheme = ThemeManager.getTheme();

  let roleClass = 'role-prosumer';
  if (role === 'Admin') roleClass = 'role-admin';
  else if (role === 'MicrogridOperator') roleClass = 'role-operator';

  const userInitial = rawUser?.firstName ? escape(rawUser.firstName.charAt(0).toUpperCase()) : 'U';

  // Role-Specific Navigation Menu Configs
  const menuItems = getMenuItemsForRole(role, activePage);

  const innerContentHTML = layoutContainer.innerHTML;

  layoutContainer.innerHTML = `
    <a class="skip-link" href="#main-content">Skip to content</a>
    <div class="app-layout">
      <button class="sidebar-backdrop" aria-label="Close navigation" tabindex="-1" onclick="setSidebarOpen(false)"></button>

      <!-- Professional Sidebar Navigation -->
      <aside id="sidebar" class="sidebar" aria-label="Workspace navigation">
        <div class="sidebar-header">
          <div class="sidebar-brand-badge">
            <svg class="sidebar-logo" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.3" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
            </svg>
          </div>
          <div class="sidebar-brand-text">
            <div class="sidebar-title">SmartMicrogrid</div>
            <div class="sidebar-subtitle">ENERGY WORKSPACE</div>
          </div>
        </div>

        <button class="sidebar-close" aria-label="Close navigation" onclick="setSidebarOpen(false)">&times;</button>
        <div class="sidebar-user-card">
          <div class="sidebar-user-avatar-wrapper">
            <div class="sidebar-user-avatar">${userInitial}</div>
            <span class="status-dot"></span>
          </div>
          <div class="sidebar-user-info">
            <div class="sidebar-user-name" title="${user ? `${user.firstName} ${user.lastName}` : 'User'}">${user ? `${user.firstName} ${user.lastName}` : 'User'}</div>
            <div class="sidebar-user-role-row">
              <span class="sidebar-role-label">${roleLabel}</span>

            </div>
          </div>
        </div>

        <div class="sidebar-search"><svg aria-hidden="true" width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24"><circle cx="10.5" cy="10.5" r="6.5" stroke-width="1.8"/><path d="m16 16 4 4" stroke-width="1.8"/></svg><input id="navigation-search" type="search" placeholder="Find a page…" aria-label="Find a navigation page" autocomplete="off"></div>
        <nav class="sidebar-nav" aria-label="Main navigation">
          ${menuItems}
        </nav>
        <p class="sidebar-no-results" role="status" hidden>No matching pages.</p>

        <div class="sidebar-footer">
          <div class="sidebar-workspace-caption">Smarter energy. Connected communities.</div>
          <button id="theme-toggle-btn" onclick="ThemeManager.toggleTheme()" class="theme-toggle-btn">
            ${currentTheme === 'dark' ? '☀️ Light Mode' : '🌙 Dark Mode'}
          </button>
          <button onclick="AuthGuard.logout()" class="btn btn-secondary btn-sm sidebar-logout-btn" style="width: 100%;">
            <svg width="15" height="15" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path></svg>
            <span>Sign Out</span>
          </button>
        </div>
      </aside>

      <!-- Main Application Area -->
      <div class="app-main">
        <header class="app-header">
          <div class="header-left">
            <button class="mobile-toggle-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-expanded="false" aria-controls="sidebar">
              <svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
              </svg>
            </button>
            <div class="header-title-group">
              <div class="header-breadcrumb">Workspace <span aria-hidden="true">/</span> ${roleLabel}</div>
              <div class="workspace-brand">SmartMicrogrid</div>
            </div>
          </div>

          <div class="header-right">
            <span class="header-date">${new Intl.DateTimeFormat(undefined, {month: 'short', day: 'numeric', year: 'numeric'}).format(new Date())}</span>
            <span class="header-avatar" aria-hidden="true">${userInitial}</span>
          </div>
        </header>

        <main id="main-content" class="main-content" tabindex="-1">
          <div class="container animate-fade-in">
            ${innerContentHTML}
          </div>
        </main>

        <footer class="footer">
          <div class="container">
            <span class="footer-brand">SmartMicrogrid</span><span>Connected energy communities</span><span>&copy; ${new Date().getFullYear()}</span>
          </div>
        </footer>
      </div>

    </div>
  `;

  renderPageIntro(document.querySelector('.main-content > .container'), activePage, pageTitle);
  ThemeManager.updateToggleIcon(currentTheme);
  document.querySelectorAll('.sidebar-link').forEach(link => {
    if (link.classList.contains('active')) link.setAttribute('aria-current', 'page');
    if (link.getAttribute('href').startsWith('/')) link.href = getWebAppUrl(link.getAttribute('href').slice(1));
  });
  document.getElementById('navigation-search').addEventListener('input', event => {
    const query = event.target.value.trim().toLowerCase();
    let count = 0;
    document.querySelectorAll('.sidebar-nav > div').forEach(section => {
      let matches = 0;
      section.querySelectorAll('li').forEach(item => {
        item.hidden = !item.textContent.toLowerCase().includes(query);
        if (!item.hidden) matches++;
      });
      section.hidden = matches === 0;
      count += matches;
    });
    document.querySelector('.sidebar-no-results').hidden = count > 0;
  });
  syncSidebarAccessibility();
  document.querySelector('.sidebar-link[aria-current="page"]')?.scrollIntoView({ block: 'nearest' });
}

function getMenuItemsForRole(role, activePage) {
  const icons = {
    dashboard: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"></path></svg>`,
    users: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"></path></svg>`,
    grid: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path></svg>`,
    battery: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"></path></svg>`,
    trading: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>`,
    reports: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"></path></svg>`,
    activity: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`,
    system: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path></svg>`,
    settings: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path></svg>`,
    roles: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path></svg>`,
    adminDashboard: `<svg fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path></svg>`
  };

  let sections = '';
  const isM3User = ['Admin', 'MicrogridOperator'].includes(role);

  // Core Overview Navigation
  sections += `
    <div>
      <div class="sidebar-section-title">Navigation</div>
      <ul class="sidebar-menu">
        <li>
          <a href="${getWebAppUrl('dashboard.html')}" class="sidebar-link ${activePage === 'dashboard' ? 'active' : ''}">
            ${icons.dashboard} <span>Overview</span>
          </a>
        </li>
      </ul>
    </div>
  `;

  if (isM3User) {
    sections += `
      <div>
        <div class="sidebar-section-title">Transactions</div>
        <ul class="sidebar-menu">
          ${role === 'MicrogridOperator' ? `
          <li><a href="${getWebAppUrl('pages/M3/dashboard.html')}" class="sidebar-link ${activePage === 'm3-dashboard' ? 'active' : ''}">${icons.dashboard} <span>Transaction Operations</span></a></li>` : ''}

          <li>
            <a href="${getWebAppUrl('pages/M3/transactions.html')}" class="sidebar-link ${activePage === 'transactions' ? 'active' : ''}">
              ${icons.trading} <span>${role === 'Admin' ? 'All Transactions' : 'Transactions'}</span>
            </a>
          </li>
        </ul>
      </div>
    `;
  }

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
      </ul>
    </div>
  `;

  // M2 Energy Search & Reservations Section (All Roles)
  const reservationLabel = role === 'Admin' ? 'All Reservations' :
                           role === 'MicrogridOperator' ? 'Microgrid Reservations' :
                           role === 'MicrogridOperator' ? 'Reservations & Verification' :
                           'My Reservations & Claims';

  sections += `
    <div>
      <div class="sidebar-section-title">Energy marketplace</div>
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
            <a href="/pages/M4/dashboard.html" class="sidebar-link ${activePage === 'm4-dashboard' ? 'active' : ''}">
              ${icons.adminDashboard} <span>Admin Dashboard</span>
            </a>
          </li>
          <li>
            <a href="/pages/users/users.html" class="sidebar-link ${activePage === 'users' ? 'active' : ''}">
              ${icons.users} <span>User Management</span>
            </a>
          </li>
          <li>
            <a href="/pages/M4/roles.html" class="sidebar-link ${activePage === 'm4-roles' ? 'active' : ''}">
              ${icons.roles} <span>Role Management</span>
            </a>
          </li>
          <li>
            <a href="/pages/M4/activity.html" class="sidebar-link ${activePage === 'm4-activity' ? 'active' : ''}">
              ${icons.activity} <span>System Activity</span>
            </a>
          </li>
          <li>
            <a href="/pages/M4/system.html" class="sidebar-link ${activePage === 'm4-system' ? 'active' : ''}">
              ${icons.system} <span>System Health</span>
            </a>
          </li>
          <li>
            <a href="/pages/M4/configuration.html" class="sidebar-link ${activePage === 'm4-configuration' ? 'active' : ''}">
              ${icons.settings} <span>Configuration</span>
            </a>
          </li>
          <li>
            <a href="/pages/M4/reports.html" class="sidebar-link ${activePage === 'm4-reports' ? 'active' : ''}">
              ${icons.reports} <span>Reports</span>
            </a>
          </li>
        </ul>
      </div>
    `;
  }

  return sections;
}

function setSidebarOpen(open) {
  const sidebar = document.getElementById('sidebar');
  if (!sidebar) return;
  sidebar.classList.toggle('open', open);
  document.body.classList.toggle('navigation-open', open);
  const toggle = document.querySelector('.mobile-toggle-btn');
  toggle?.setAttribute('aria-expanded', String(open));
  syncSidebarAccessibility();
  if (open) sidebar.querySelector('.sidebar-close')?.focus();
  else toggle?.focus();
}

function toggleSidebar() { setSidebarOpen(!document.getElementById('sidebar')?.classList.contains('open')); }

function syncSidebarAccessibility() {
  const sidebar = document.getElementById('sidebar');
  if (!sidebar) return;
  const mobile = window.matchMedia('(max-width: 992px)').matches;
  const open = sidebar.classList.contains('open');
  sidebar.inert = mobile && !open;
  const main = document.querySelector('.app-main');
  if (main) main.inert = mobile && open;
}

document.addEventListener('keydown', event => {
  const sidebar = document.getElementById('sidebar');
  if (!sidebar?.classList.contains('open')) return;
  if (event.key === 'Escape') { setSidebarOpen(false); return; }
  if (event.key !== 'Tab') return;
  const controls = Array.from(sidebar.querySelectorAll('button, a[href], input')).filter(el => el.getClientRects().length && !el.disabled);
  const first = controls[0], last = controls[controls.length - 1];
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
});
window.addEventListener('resize', () => {
  if (window.innerWidth > 992) {
    document.getElementById('sidebar')?.classList.remove('open');
    document.body.classList.remove('navigation-open');
    document.querySelector('.mobile-toggle-btn')?.setAttribute('aria-expanded', 'false');
  }
  syncSidebarAccessibility();
});
