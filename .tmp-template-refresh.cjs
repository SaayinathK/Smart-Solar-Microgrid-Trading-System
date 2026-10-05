const fs = require('fs');
const path = require('path');
const web = 'Web/SmartMicrogrid.Web';
function walk(dir) { return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]); }
function edit(file, fn) { const s=fs.readFileSync(file,'utf8'); fs.writeFileSync(file,fn(s)); }
for (const file of walk(web).filter(f=>f.endsWith('.html')&&!f.includes('vendor'))) {
  edit(file,s=>{
    if (!s.includes('app-layout-wrapper')) return s;
    // These banners repeat the preceding page title and description.
    if (/(users[\\/]users|M3[\\/]transactions|M4[\\/]dashboard)\.html$/.test(file)) {
      s=s.replace(/\s*<div class="hero-image-banner mb-4">[\s\S]*?<\/div>\s*<\/div>/,'');
    }
    s=s.replace(/<(div|section) class="(page-header[^\"]*|dashboard-welcome|hero-image-banner[^\"]*|reservation-hero|availability-hero)"/, '<$1 data-page-intro class="$2"');
    if (!s.includes('data-page-intro')) {
      if (/create-microgrid|edit-microgrid/.test(file)) s=s.replace('<div style="margin-bottom: 1.5rem;">','<div data-page-intro style="margin-bottom: 1.5rem;">');
      else if (/microgrid-details/.test(file)) s=s.replace('<div style="margin-bottom: 1.5rem; display:', '<div data-page-intro style="margin-bottom: 1.5rem; display:');
      else s=s.replace('<div class="card" style="margin-bottom: 1.5rem;">','<div data-page-intro class="card" style="margin-bottom: 1.5rem;">');
    }
    return s;
  });
}
edit(web+'/js/common/sidebar.js',s=>s.replace('function renderAppLayout(activePage', 'function renderAppLayout(activePage')
  .replace("pageTitle = 'Dashboard'", "pageTitle = 'Overview'")
  .replace('  if (!layoutContainer) return;', '  if (!layoutContainer || layoutContainer.dataset.layoutReady) return;\n  layoutContainer.dataset.layoutReady = \'true\';')
  .replace('<div class="header-title">${pageTitle}</div>', '<div class="workspace-brand">SmartMicrogrid</div>')
  .replace('&copy; 2026 Smart Solar Microgrid Energy Management & Trading System.', '<span class="footer-brand">SmartMicrogrid</span><span>Connected energy communities</span><span>&copy; ${new Date().getFullYear()}</span>')
  .replace('  ThemeManager.updateToggleIcon(currentTheme);', '  renderPageIntro(document.querySelector(\'.main-content > .container\'), activePage, pageTitle);\n  ThemeManager.updateToggleIcon(currentTheme);'));
edit(web+'/js/common/navbar.js',s=>s.replace('<nav class="navbar">','<nav class="navbar" aria-label="Main navigation">'));
for (const file of ['index.html','login.html','register.html','mobile-redirect.html']) {
  edit(web+'/'+file,s=>{
    const footer='<footer class="footer"><div class="container"><span class="footer-brand">SmartMicrogrid</span><span>Connected energy communities</span><span>&copy; 2026</span></div></footer>';
    return s.includes('<footer') ? s.replace(/<footer[\s\S]*?<\/footer>/,footer) : s.replace('  <script src="js/config/api-config.js">',footer+'\n  <script src="js/config/api-config.js">');
  });
}
edit(web+'/pages/M3/transactions.html',s=>s.replace('id="transactions-list-title">Energy Transactions','id="transactions-list-title">Transaction records'));
edit(web+'/js/transactions/transactions.js',s=>s.replace("'transactions-list-title').textContent = 'Transaction History'", "'transactions-list-title').textContent = 'Past transfers'"));
edit(web+'/pages/M4/configuration.html',s=>s.replace('Platform-wide settings stored in the <code>systemConfiguration</code> collection','Manage platform identity, trading rules, and operating preferences.'));
edit(web+'/pages/users/user-details.html',s=>s.replace('MongoDB Document ID & System Information','Review contact details, account access, and user status.'));
edit(web+'/pages/M1/microgrids.html',s=>s.replace(/<div style="text-align: right; display: flex; flex-direction: column; gap: 6px;">[\s\S]*?<\/div>/,'').replace('Solar Microgrid Directory & Filters','Find a microgrid'));
edit(web+'/pages/M1/energy-availability.html',s=>s.replaceAll('$LKR','LKR').replace('class="energy-offer-card"','class="energy-offer-card data-surface"'));
edit(web+'/pages/M1/battery-storage.html',s=>s.replace('class="card" style="display: flex; flex-direction: column; justify-content: space-between;"','class="card battery-data-card data-surface" style="display: flex; flex-direction: column; justify-content: space-between;"').replace('1fr solid','1px solid'));
edit(web+'/pages/M1/energy-capacity.html',s=>s.replace('class="card" style="margin-bottom: 1.5rem;">\n              <div', 'class="card capacity-data-card data-surface">\n              <div').replace('class="card" style="margin-bottom: 1.5rem;">\r\n              <div', 'class="card capacity-data-card data-surface">\r\n              <div'));
