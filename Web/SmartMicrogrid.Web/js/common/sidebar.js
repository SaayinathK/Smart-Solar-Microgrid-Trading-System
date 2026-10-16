/* ==========================================================================
   Smart Microgrid Energy System - Dynamic Sidebar Component
   ========================================================================== */

// Match the current host's page style. Some static hosts redirect *.html to
// extensionless paths without retaining query parameters (transaction IDs/views).
function getWebPageUrl(path, baseUrl = window.location.href) {
  const url = new URL(path, baseUrl);
  const current = new URL(window.location.href);
  const pageName = current.pathname.split('/').pop();
  if (pageName && !pageName.includes('.') && url.origin === current.origin) {
    url.pathname = url.pathname.replace(/\.html$/, '');
  }
  return url.href;
}

function getWebAppUrl(path) {
  const sidebarScript = Array.from(document.scripts).find(script =>
    new URL(script.src, document.baseURI).pathname.endsWith('/js/common/sidebar.js')
  );
  const appBase = sidebarScript
    ? new URL('../../', sidebarScript.src)
    : new URL('.', window.location.href);

  return getWebPageUrl(path, appBase);
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
  if (role === 'Admin') {
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
            <img class="sidebar-logo" src="${getWebAppUrl('assets/images/smart_micro.png')}" alt="" aria-hidden="true" width="42" height="42">
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

        <nav class="sidebar-nav" aria-label="Main navigation">
          ${menuItems}
        </nav>

        <div class="sidebar-footer">
          <button type="button" onclick="AuthGuard.logout()" class="btn btn-secondary btn-sm sidebar-logout-btn">
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
              <div class="workspace-brand"><img class="workspace-brand-logo" src="${getWebAppUrl('assets/images/smart_micro.png')}" alt="" aria-hidden="true" width="24" height="24"><span>SmartMicrogrid</span></div>
            </div>
          </div>

          <div class="workspace-search">
            <form id="workspace-search-form" class="workspace-search-field" role="search" aria-label="Workspace page search">
              <svg aria-hidden="true" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.8" viewBox="0 0 24 24"><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 4 4"/></svg>
              <input id="navigation-search" type="search" placeholder="Search workspace pages..." aria-label="Search workspace pages" aria-controls="workspace-search-results" autocomplete="off" spellcheck="false">
              <button id="workspace-search-clear" type="button" aria-label="Clear page search" hidden>&times;</button>
              <kbd id="workspace-search-shortcut" aria-hidden="true">Ctrl K</kbd>
            </form>
            <section id="workspace-search-results" class="workspace-search-results" aria-label="Matching workspace pages" hidden>
              <div class="workspace-search-caption"><span id="workspace-search-status" role="status" aria-live="polite">Quick navigation</span><span aria-hidden="true">Esc to close</span></div>
              <ul id="workspace-search-list"></ul>
              <p id="workspace-search-empty" hidden>No matching pages. Try energy, reservations, or transactions.</p>
            </section>
          </div>

          <div class="header-right">
            <span class="header-date">${new Intl.DateTimeFormat(undefined, {month: 'short', day: 'numeric', year: 'numeric'}).format(new Date())}</span>
            <button type="button" id="theme-toggle-btn" onclick="ThemeManager.toggleTheme()" class="theme-toggle-btn header-theme-toggle">
              ${currentTheme === 'dark' ? 'Light Mode' : 'Dark Mode'}
            </button>
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
            <span class="footer-brand"><img src="${getWebAppUrl('assets/images/smart_micro.png')}" alt="" aria-hidden="true" width="24" height="24">SmartMicrogrid</span><span>Connected energy communities</span><span>&copy; ${new Date().getFullYear()}</span>
          </div>
        </footer>
      </div>

    </div>
  `;

  renderPageIntro(document.querySelector('.main-content > .container'), activePage, pageTitle);
  if (role === 'Admin') renderAdminWorkspaceNavigation(activePage);
  if (role === 'MicrogridOperator') renderOperatorWorkspaceNavigation(activePage);
  ThemeManager.updateToggleIcon(currentTheme);
  document.querySelectorAll('.sidebar-link').forEach(link => {
    if (link.classList.contains('active')) link.setAttribute('aria-current', 'page');
    if (link.getAttribute('href').startsWith('/')) link.href = getWebAppUrl(link.getAttribute('href').slice(1));
  });
  document.querySelectorAll('.app-layout a[href]').forEach(link => {
    if (!link.getAttribute('href').startsWith('#')) link.href = getWebPageUrl(link.href);
  });
  initializeWorkspaceSearch(role);
  syncSidebarAccessibility();
  document.querySelector('.sidebar-link[aria-current="page"]')?.scrollIntoView({ block: 'nearest' });
}

// Search only destinations exposed in this role's navigation and workspaces.
function getWorkspaceSearchPages(role, primaryPages) {
  const pages = [...primaryPages];
  const aliases = {
    availability: 'energy available supply solar offers marketplace',
    battery: 'energy charge storage reserves',
    capacity: 'generation energy network allocation',
    slots: 'publish energy offers delivery windows',
    'm4-activity': 'audit events history',
    'm4-configuration': 'settings platform maintenance'
  };
  const workspaces = role === 'Admin' ? Object.values(ADMIN_WORKSPACES)
    : role === 'MicrogridOperator' ? [ADMIN_WORKSPACES.microgrids] : [];
  if (['Admin', 'MicrogridOperator'].includes(role)) workspaces.push(getOperatorWorkspace('transactions'));
  workspaces.forEach(workspace => workspace.pages.forEach(([key, title, path]) => {
    pages.push({ title, group: workspace.label, href: getWebAppUrl(path), keywords: aliases[key] || '' });
  }));
  const seen = new Set();
  return pages.filter(page => {
    if (seen.has(page.href)) return false;
    seen.add(page.href);
    return true;
  });
}

function matchWorkspacePages(pages, query) {
  const normalized = query.trim().toLowerCase();
  if (!normalized) return pages.slice(0, 6);
  const tokens = normalized.split(/\s+/);
  return pages.filter(page => tokens.every(token => `${page.title} ${page.group} ${page.keywords}`.toLowerCase().includes(token)))
    .sort((a, b) => {
      const score = page => page.title.toLowerCase() === normalized ? 0 : page.title.toLowerCase().startsWith(normalized) ? 1 : page.title.toLowerCase().includes(normalized) ? 2 : 3;
      return score(a) - score(b) || a.title.localeCompare(b.title);
    }).slice(0, 8);
}

function initializeWorkspaceSearch(role) {
  const wrapper = document.querySelector('.workspace-search');
  const input = document.getElementById('navigation-search');
  const panel = document.getElementById('workspace-search-results');
  const list = document.getElementById('workspace-search-list');
  const clear = document.getElementById('workspace-search-clear');
  const pages = getWorkspaceSearchPages(role, Array.from(document.querySelectorAll('.sidebar-link')).map(link => ({
    title: link.querySelector('span')?.textContent.trim() || link.textContent.trim(),
    href: link.href,
    group: link.closest('[role="group"]')?.getAttribute('aria-label') || 'Workspace',
    keywords: link.closest('li')?.dataset.search || ''
  })));
  const isMac = /Mac|iPhone|iPad/.test(navigator.platform);
  document.getElementById('workspace-search-shortcut').textContent = isMac ? '⌘ K' : 'Ctrl K';
  input.setAttribute('aria-keyshortcuts', isMac ? 'Meta+K' : 'Control+K');
  function close() { panel.hidden = true; }
  function render() {
    const matches = matchWorkspacePages(pages, input.value);
    clear.hidden = !input.value;
    list.replaceChildren(...matches.map(page => {
      const item = document.createElement('li');
      const link = document.createElement('a');
      link.href = page.href;
      const title = document.createElement('strong'); title.textContent = page.title;
      const group = document.createElement('span'); group.textContent = page.group;
      const arrow = document.createElement('span'); arrow.className = 'workspace-search-arrow'; arrow.textContent = '↗'; arrow.setAttribute('aria-hidden', 'true');
      const label = document.createElement('div'); label.append(title, group);
      link.append(label, arrow); item.append(link); return item;
    }));
    document.getElementById('workspace-search-empty').hidden = matches.length > 0;
    document.getElementById('workspace-search-status').textContent = input.value.trim() ? `${matches.length} matching page${matches.length === 1 ? '' : 's'}` : 'Quick navigation';
    panel.hidden = false;
  }
  input.addEventListener('input', render);
  input.addEventListener('focus', render);
  clear.addEventListener('click', () => { input.value = ''; input.focus(); render(); });
  document.getElementById('workspace-search-form').addEventListener('submit', event => {
    event.preventDefault();
    if (panel.hidden) render();
    list.querySelector('a')?.click();
  });
  wrapper.addEventListener('keydown', event => {
    if (event.key === 'Escape') { event.preventDefault(); input.focus(); close(); return; }
    if (!['ArrowDown', 'ArrowUp'].includes(event.key)) return;
    event.preventDefault();
    if (panel.hidden) render();
    const links = Array.from(list.querySelectorAll('a'));
    if (!links.length) return;
    const index = links.indexOf(document.activeElement);
    const next = event.key === 'ArrowDown' ? (index + 1) % links.length : index < 0 ? links.length - 1 : (index - 1 + links.length) % links.length;
    links[next].focus();
  });
  wrapper.addEventListener('focusout', event => { if (!wrapper.contains(event.relatedTarget)) close(); });
  document.addEventListener('click', event => { if (!wrapper.contains(event.target)) close(); });
  document.addEventListener('keydown', event => {
    if (!(event.ctrlKey || event.metaKey) || event.altKey || event.key.toLowerCase() !== 'k') return;
    if (document.querySelector('dialog[open]') || document.querySelector('.app-main')?.inert) return;
    event.preventDefault(); input.focus(); input.select(); render();
  });
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

  if (role === 'Admin') return getAdminMenuItems(icons, activePage);
  if (role === 'MicrogridOperator') return getOperatorMenuItems(icons, activePage);

  let sections = '';

  // Core Overview Navigation
  sections += `
    <div role="group" aria-label="Navigation">
      <ul class="sidebar-menu">
        <li>
          <a href="${getWebAppUrl('dashboard.html')}" class="sidebar-link ${activePage === 'dashboard' ? 'active' : ''}">
            ${icons.dashboard} <span>Overview</span>
          </a>
        </li>
      </ul>
    </div>
  `;

  // M1 Microgrid Management Menu Section
  sections += `
    <div role="group" aria-label="Microgrid resources">
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
  const reservationLabel = 'My Reservations & Claims';

  sections += `
    <div role="group" aria-label="Energy marketplace">
      <ul class="sidebar-menu">
        <li>
          <a href="/pages/reservations/reservations.html" class="sidebar-link ${activePage === 'reservations' ? 'active' : ''}">
            ${icons.trading} <span>${reservationLabel}</span>
          </a>
        </li>
        ${role === 'Prosumer' ? `
        <li>
          <a href="/pages/M3/transactions.html?view=all" class="sidebar-link ${activePage === 'transactions' ? 'active' : ''}">
            ${icons.reports} <span>My transactions & QR</span>
          </a>
        </li>
        <li>
          <a href="/pages/M1/energy-availability.html" class="sidebar-link ${activePage === 'availability' ? 'active' : ''}">
            ${icons.grid} <span>Browse Available Energy</span>
          </a>
        </li>` : ''}
      </ul>
    </div>
  `;

  return sections;
}

// Seven primary destinations; related tools live in contextual workspace navigation.
const ADMIN_WORKSPACES = {
  microgrids: {
    label: 'Microgrid tools',
    pages: [
      ['microgrids', 'Microgrids', 'pages/M1/microgrids.html'],
      ['capacity', 'Energy capacity', 'pages/M1/energy-capacity.html'],
      ['battery', 'Battery storage', 'pages/M1/battery-storage.html'],
      ['slots', 'Energy slots', 'pages/M1/energy-slots.html'],
      ['availability', 'Availability', 'pages/M1/energy-availability.html']
    ]
  },
  users: {
    label: 'Users and access',
    pages: [
      ['users', 'User accounts', 'pages/users/users.html'],
      ['m4-roles', 'Roles & permissions', 'pages/M4/roles.html']
    ]
  },
  system: {
    label: 'System management',
    pages: [
      ['m4-system', 'System health', 'pages/M4/system.html'],
      ['m4-activity', 'Activity log', 'pages/M4/activity.html'],
      ['m4-configuration', 'Configuration', 'pages/M4/configuration.html']
    ]
  }
};

function getAdminWorkspace(activePage) {
  return Object.entries(ADMIN_WORKSPACES).find(([, workspace]) =>
    workspace.pages.some(([key]) => key === activePage)
  );
}

function getAdminMenuItems(icons, activePage) {
  const selected = getAdminWorkspace(activePage)?.[0] ||
    (['dashboard', 'm4-dashboard'].includes(activePage) ? 'dashboard' :
      ['transactions', 'history', 'm3-dashboard'].includes(activePage) ? 'transactions' : activePage);
  const groups = [
    ['Workspace', [
      ['dashboard', 'Dashboard', 'pages/M4/dashboard.html', 'dashboard', 'overview administration'],
      ['microgrids', 'Microgrids', 'pages/M1/microgrids.html', 'grid', 'solar energy capacity battery storage slots availability'],
      ['reservations', 'Reservations', 'pages/reservations/reservations.html', 'trading', 'bookings approvals'],
      ['transactions', 'Transactions', 'pages/M3/transactions.html', 'reports', 'transfers ledger history']
    ]],
    ['Administration', [
      ['users', 'Users & access', 'pages/users/users.html', 'users', 'accounts roles permissions pending activations'],
      ['system', 'System', 'pages/M4/system.html', 'settings', 'health activity audit log configuration maintenance'],
      ['m4-reports', 'Reports', 'pages/M4/reports.html', 'reports', 'analytics exports']
    ]]
  ];
  return groups.map(([label, items]) => `
    <div role="group" aria-label="${label}">
      <ul class="sidebar-menu">${items.map(([key, title, path, icon, keywords]) => `
        <li data-search="${keywords}"><a href="${getWebAppUrl(path)}" class="sidebar-link ${selected === key ? 'active' : ''}">
          ${icons[icon]}<span>${title}</span>
        </a></li>`).join('')}
      </ul>
    </div>`).join('');
}

function renderAdminWorkspaceNavigation(activePage) {
  const workspace = getAdminWorkspace(activePage)?.[1];
  if (!workspace) return;
  renderWorkspaceNavigation(workspace, activePage);
}

function getOperatorWorkspace(activePage) {
  if (ADMIN_WORKSPACES.microgrids.pages.some(([key]) => key === activePage)) {
    return { key: 'microgrids', ...ADMIN_WORKSPACES.microgrids };
  }
  if (['transactions', 'history', 'm3-dashboard'].includes(activePage)) {
    return {
      key: 'transactions', label: 'Transaction views',
      pages: [
        ['attention', 'Needs attention', 'pages/M3/transactions.html?view=attention'],
        ['all', 'All transactions', 'pages/M3/transactions.html?view=all'],
        ['history', 'History', 'pages/M3/transactions.html?view=history']
      ]
    };
  }
  return null;
}

function getOperatorMenuItems(icons, activePage) {
  const selected = getOperatorWorkspace(activePage)?.key || activePage;
  const items = [
    ['dashboard', 'Dashboard', 'dashboard.html', 'dashboard', 'overview network alerts'],
    ['microgrids', 'Microgrids', 'pages/M1/microgrids.html', 'grid', 'solar energy capacity battery storage slots availability'],
    ['reservations', 'Reservations', 'pages/reservations/reservations.html', 'trading', 'bookings approvals claims'],
    ['transactions', 'Transactions', 'pages/M3/transactions.html?view=attention', 'reports', 'operations verification QR transfers ledger history']
  ];
  return `<div role="group" aria-label="Operator workspace">
    <ul class="sidebar-menu">${items.map(([key, label, path, icon, keywords]) => `
      <li data-search="${keywords}"><a href="${getWebAppUrl(path)}" class="sidebar-link ${selected === key ? 'active' : ''}">
        ${icons[icon]}<span>${label}</span>
      </a></li>`).join('')}
    </ul></div>`;
}

function renderOperatorWorkspaceNavigation(activePage) {
  const workspace = getOperatorWorkspace(activePage);
  if (!workspace) return;
  const params = new URL(window.location.href).searchParams;
  const view = params.get('view');
  const onList = /\/transactions(?:\.html)?$/.test(window.location.pathname);
  const selected = workspace.key === 'transactions'
    ? (onList ? (view === 'history' ? 'history' : !view || view === 'attention' ? 'attention' : 'all') : null)
    : activePage;
  renderWorkspaceNavigation(workspace, selected);
}

function renderWorkspaceNavigation(workspace, activePage) {
  const navigation = document.createElement('nav');
  navigation.className = 'workspace-navigation';
  if (workspace.key) navigation.dataset.workspace = workspace.key;
  navigation.setAttribute('aria-label', workspace.label);
  for (const [key, label, path] of workspace.pages) {
    const link = document.createElement('a');
    link.href = getWebAppUrl(path);
    link.textContent = label;
    if (key === activePage) link.setAttribute('aria-current', 'page');
    navigation.append(link);
  }
  document.querySelector('.page-intro')?.after(navigation);
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
