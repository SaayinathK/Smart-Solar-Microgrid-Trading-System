const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function workspace(role, authenticated = true) {
  const location = { href: 'https://example.test/microgrid/pages/M4/system.html' };
  const session = { isAuthenticated: () => authenticated, getUserRole: () => role };
  const context = vm.createContext({
    URL, window: { location, addEventListener() {} },
    document: {
      baseURI: location.href,
      scripts: ['sidebar', 'auth'].map(name => ({ src: `https://example.test/microgrid/js/common/${name}.js` })),
      addEventListener() {}
    },
    SessionManager: session, ApiClient: { showToast() {} }, setTimeout() {}
  });
  for (const file of ['sidebar', 'auth']) {
    vm.runInContext(fs.readFileSync(path.join(__dirname, `../js/common/${file}.js`), 'utf8'), context);
  }
  vm.runInContext('globalThis.guard = AuthGuard', context);
  return { context, location };
}

test('Admin navigation has seven unique destinations and one dashboard', () => {
  const { context } = workspace('Admin');
  const markup = context.getMenuItemsForRole('Admin', 'm4-dashboard');
  const links = [...markup.matchAll(/href="([^"]+)"/g)].map(match => match[1]);
  assert.equal(links.length, 7);
  assert.equal(new Set(links).size, 7);
  assert.equal(links.filter(link => link.endsWith('/dashboard.html')).length, 1);
  assert.ok(links.every(link => link.startsWith('https://example.test/microgrid/')));
  assert.match(markup, /class="sidebar-link active"[^]*?<span>Dashboard<\/span>/);
});

test('Workspace search includes grouped tools and preserves role boundaries', () => {
  const { context } = workspace('Admin');
  const primary = [{title: 'Microgrids', group: 'Workspace', href: 'https://example.test/microgrid/pages/M1/microgrids.html', keywords: 'battery storage'}];
  const admin = context.getWorkspaceSearchPages('Admin', primary);
  assert.ok(admin.some(page => page.title === 'Energy slots'));
  assert.ok(admin.some(page => page.title === 'Configuration'));
  assert.equal(new Set(admin.map(page => page.href)).size, admin.length);
  assert.ok(admin.every(page => page.href.startsWith('https://example.test/microgrid/')));
  const operator = context.getWorkspaceSearchPages('MicrogridOperator', primary);
  assert.ok(operator.some(page => page.title === 'History'));
  assert.ok(operator.some(page => page.title === 'Battery storage'));
  assert.ok(operator.every(page => !/\/pages\/(M4|users)\//.test(page.href)));
  const prosumer = context.getWorkspaceSearchPages('Prosumer', primary);
  assert.equal(prosumer.length, 1);
});

test('Workspace search ranks page names first, matches multiple words, and handles no results', () => {
  const { context } = workspace('Admin');
  const pages = context.getWorkspaceSearchPages('Admin', [{title: 'Microgrids', group: 'Workspace', href: 'https://example.test/microgrid/pages/M1/microgrids.html', keywords: 'battery storage energy'}]);
  assert.equal(context.matchWorkspacePages(pages, '  BATTERY  ')[0].title, 'Battery storage');
  assert.equal(context.matchWorkspacePages(pages, 'energy capacity')[0].title, 'Energy capacity');
  assert.equal(context.matchWorkspacePages(pages, 'available energy')[0].title, 'Availability');
  assert.equal(context.matchWorkspacePages(pages, 'unmatched phrase').length, 0);
  assert.equal(context.matchWorkspacePages(pages, '').length, 6);
});

test('Grouped pages select the correct primary destination', () => {
  const { context } = workspace('Admin');
  for (const [page, label] of [
    ['capacity', 'Microgrids'], ['battery', 'Microgrids'], ['slots', 'Microgrids'],
    ['availability', 'Microgrids'], ['m4-roles', 'Users & access'],
    ['m4-activity', 'System'], ['m4-configuration', 'System'], ['history', 'Transactions']
  ]) {
    const markup = context.getMenuItemsForRole('Admin', page);
    const selected = [...markup.matchAll(/class="sidebar-link active">\s*<svg[^]*?<\/svg><span>([^<]+)<\/span>/g)];
    assert.equal(selected.length, 1, page);
    assert.equal(selected[0][1], label, page);
  }
});

test('Operator navigation keeps operational tools and excludes administrative pages', () => {
  const { context } = workspace('MicrogridOperator');
  const markup = context.getMenuItemsForRole('MicrogridOperator', 'dashboard');
  const links = [...markup.matchAll(/href="([^"]+)"/g)].map(match => match[1]);
  assert.equal(links.length, 4);
  assert.equal(new Set(links).size, 4);
  assert.ok(links.every(link => link.startsWith('https://example.test/microgrid/')));
  assert.ok(links.some(link => link.endsWith('/transactions.html?view=attention')));
  assert.doesNotMatch(markup, /pages\/M4\//);
  assert.doesNotMatch(markup, /pages\/users\//);
});

test('Operator energy and transaction pages retain one selected workspace', () => {
  const { context } = workspace('MicrogridOperator');
  for (const page of ['microgrids', 'capacity', 'battery', 'slots', 'availability', 'transactions', 'history', 'm3-dashboard']) {
    const markup = context.getMenuItemsForRole('MicrogridOperator', page);
    const selected = [...markup.matchAll(/class="sidebar-link active">\s*<svg[^]*?<\/svg><span>([^<]+)<\/span>/g)];
    assert.equal(selected.length, 1, page);
    assert.equal(selected[0][1], ['transactions', 'history', 'm3-dashboard'].includes(page) ? 'Transactions' : 'Microgrids');
  }
  const energy = context.getOperatorWorkspace('battery');
  assert.equal(energy.pages.length, 5);
  const transactions = context.getOperatorWorkspace('transactions');
  assert.deepEqual(Array.from(transactions.pages, ([key]) => key), ['attention', 'all', 'history']);
  assert.equal(context.getOperatorWorkspace('m4-roles'), null);
});

test('Home routing follows role and works under an IIS application path', () => {
  for (const [role, route] of [['Admin', 'pages/M4/dashboard.html'], ['MicrogridOperator', 'dashboard.html']]) {
    const { context, location } = workspace(role);
    assert.equal(context.guard.getHomeUrl(), `https://example.test/microgrid/${route}`);
    context.guard.redirectIfAuthenticated();
    assert.equal(location.href, context.guard.getHomeUrl());
  }
});

test('Clean page URLs retain transaction IDs and views without a lossy HTML redirect', () => {
  const { context, location } = workspace('MicrogridOperator');
  location.href = 'https://example.test/microgrid/pages/M3/transactions?view=all';
  const detail = new URL(context.getWebPageUrl('transaction-details.html?id=tx%2F1'));
  assert.equal(detail.pathname, '/microgrid/pages/M3/transaction-details');
  assert.equal(detail.searchParams.get('id'), 'tx/1');
  const history = new URL(context.getWebAppUrl('pages/M3/transactions.html?view=history'));
  assert.equal(history.pathname, '/microgrid/pages/M3/transactions');
  assert.equal(history.searchParams.get('view'), 'history');
  for (const page of ['transaction-verify', 'transaction-complete']) {
    assert.equal(context.getWebPageUrl(`${page}.html?id=tx-1`), `https://example.test/microgrid/pages/M3/${page}?id=tx-1`);
  }
  assert.equal(context.getWebPageUrl('https://docs.example.test/help.html?topic=transactions'), 'https://docs.example.test/help.html?topic=transactions');
});

test('HTML and file-based hosting retain HTML links and transaction query parameters', () => {
  const { context, location } = workspace('MicrogridOperator');
  for (const base of ['https://example.test/microgrid/pages/M3/transactions.html', 'file:///C:/microgrid/pages/M3/transactions.html']) {
    location.href = base;
    const target = new URL(context.getWebPageUrl('transaction-details.html?id=tx-1'));
    assert.ok(target.pathname.endsWith('/transaction-details.html'));
    assert.equal(target.searchParams.get('id'), 'tx-1');
  }
});

test('Admin authentication checks still reject guests and other roles', () => {
  const guest = workspace(null, false);
  assert.equal(guest.context.guard.requireAuth('Admin'), false);
  assert.equal(guest.location.href, 'https://example.test/microgrid/login.html');
  assert.equal(workspace('MicrogridOperator').context.guard.requireAuth('Admin'), false);
  assert.equal(workspace('Admin').context.guard.requireAuth('Admin'), true);
});
