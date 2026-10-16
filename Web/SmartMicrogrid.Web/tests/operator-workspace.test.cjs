const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function element(tag) {
  return {
    tag, children: [],
    appendChild(child) { this.children.push(child); },
    replaceChildren(...children) { this.children = children; }
  };
}

function transactionContext(role, currentUrl = 'https://example.test/microgrid/pages/M3/transactions.html') {
  const context = vm.createContext({
    URL, window: { location: { href: currentUrl }, addEventListener() {} },
    document: { addEventListener() {}, createElement: element },
    SessionManager: { getUserRole: () => role }
  });
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/sidebar.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/record-references.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/transactions/transactions.js'), 'utf8'), context);
  return context;
}

test('Action queue includes every unfinished handoff and excludes closed transactions', () => {
  const context = transactionContext('MicrogridOperator');
  for (const status of ['Pending', 'QRGenerated', 'VerificationPending', 'Verified', 'EnergyTransferInProgress']) {
    assert.equal(context.matchesStatusFilter({ status }, 'attention'), true, status);
    assert.equal(context.matchesStatusFilter({ status }, 'history'), false, status);
  }
  for (const status of ['Completed', 'Rejected', 'Cancelled']) {
    assert.equal(context.matchesStatusFilter({ status }, 'attention'), false, status);
    assert.equal(context.matchesStatusFilter({ status }, 'history'), true, status);
  }
});

function transactionPage(view, records) {
  const ids = Object.fromEntries([
    'transaction-filter', 'transaction-search', 'transactions-list-title', 'transactions-list-description',
    'transactions-page-description', 'refresh-transactions', 'transactions-table-body', 'transactions-state',
    'total-transactions', 'pending-transactions', 'verified-transactions', 'completed-transactions'
  ].map(id => [id, element('div')]));
  ids['transaction-search'].value = '';
  ids['transaction-filter'].options = ['all', 'attention', 'history', 'Completed'].map(value => ({ value }));
  const tabs = ['attention', 'all', 'history'].map(view => ({
    href: `https://example.test/microgrid/pages/M3/transactions.html?view=${view}`,
    current: false,
    setAttribute() { this.current = true; },
    removeAttribute() { this.current = false; }
  }));
  const table = { attention: false, classList: { toggle(name, value) { table.attention = value; } } };
  const location = {
    href: `https://example.test/microgrid/pages/M3/transactions.html?view=${view}`,
    search: `?view=${view}`
  };
  const context = vm.createContext({
    URL, URLSearchParams,
    window: {
      location,
      addEventListener() {},
      history: { pushState(state, title, url) { location.href = url.href; location.search = url.search; } }
    },
    document: {
      addEventListener() {}, createElement: element,
      getElementById: id => ids[id], querySelector: () => table,
      querySelectorAll: () => tabs
    },
    SessionManager: { getUserRole: () => 'MicrogridOperator' },
    TransactionApi: { getTransactions: async () => ({ success: true, data: records }) }
  });
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/sidebar.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/record-references.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/transactions/transactions.js'), 'utf8'), context);
  return { context, ids, tabs, table };
}

test('All transactions deep link loads every status, dates, and the correct selected tab', async () => {
  const records = ['Pending', 'Verified', 'Completed', 'Rejected', 'Cancelled'].map((status, i) => ({ id: `tx-${i}`, status }));
  const { context, ids, tabs, table } = transactionPage('all', records);
  context.applyTransactionView();
  await context.loadTransactions();
  assert.equal(ids['transaction-filter'].value, 'all');
  assert.equal(ids['transactions-list-title'].textContent, 'Transaction records');
  assert.equal(ids['transactions-table-body'].children.length, 5);
  assert.equal(tabs.find(tab => tab.current).href.endsWith('?view=all'), true);
  assert.equal(table.attention, false);
  assert.equal(ids['transactions-state'].hidden, true);
  assert.equal(ids['refresh-transactions'].disabled, false);
});

test('History deep link and refresh retain only closed records with read-only row actions', async () => {
  const records = ['Pending', 'Verified', 'Completed', 'Rejected', 'Cancelled'].map((status, i) => ({ id: `tx-${i}`, status }));
  const { context, ids, tabs, table } = transactionPage('history', records);
  context.applyTransactionView();
  await context.loadTransactions();
  await context.loadTransactions();
  assert.equal(ids['transaction-filter'].value, 'history');
  assert.equal(ids['transactions-list-title'].textContent, 'Transaction history');
  assert.equal(tabs.find(tab => tab.current).href.endsWith('?view=history'), true);
  assert.equal(table.attention, false);
  const rows = ids['transactions-table-body'].children;
  assert.deepEqual(Array.from(rows, row => row.children[0].textContent), ['tx-2', 'tx-3', 'tx-4']);
  assert.ok(rows.every(row => row.children.find(cell => cell.className === 'transaction-row-actions').children.length === 1));
});

test('History with no closed records shows an empty state instead of active transfers', async () => {
  const { context, ids } = transactionPage('history', [{ id: 'active', status: 'Verified' }]);
  context.applyTransactionView();
  await context.loadTransactions();
  assert.equal(ids['transactions-table-body'].children.length, 0);
  assert.equal(ids['transactions-state'].hidden, false);
  assert.match(ids['transactions-state'].textContent, /No transactions match/);
});

test('Tab clicks switch loaded records in place, clear stale searches, and update the URL', async () => {
  const records = ['Pending', 'Verified', 'Completed', 'Rejected', 'Cancelled'].map((status, i) => ({ id: `tx-${i}`, status }));
  const { context, ids, tabs } = transactionPage('attention', records);
  context.applyTransactionView();
  await context.loadTransactions();
  for (const [view, count] of [['all', 5], ['history', 3], ['attention', 2]]) {
    let prevented = false;
    ids['transaction-search'].value = 'stale-search';
    context.navigateTransactionView({
      currentTarget: tabs.find(tab => new URL(tab.href).searchParams.get('view') === view),
      button: 0, preventDefault() { prevented = true; }
    });
    assert.equal(prevented, true);
    assert.equal(ids['transaction-search'].value, '');
    assert.equal(ids['transactions-table-body'].children.length, count);
    assert.equal(context.window.location.search, `?view=${view}`);
    assert.equal(tabs.filter(tab => tab.current).length, 1);
    assert.ok(tabs.find(tab => tab.current).href.endsWith(`?view=${view}`));
  }
});

test('Tab clicks still switch records when an embedded preview denies history updates', async () => {
  const { context, ids, tabs } = transactionPage('attention', [{ id: 'closed', status: 'Completed' }]);
  context.applyTransactionView();
  await context.loadTransactions();
  context.window.history.pushState = () => { const error = new Error('Preview denied URL change'); error.name = 'SecurityError'; throw error; };
  context.navigateTransactionView({ currentTarget: tabs[2], button: 0, preventDefault() {} });
  assert.equal(ids['transaction-filter'].value, 'history');
  assert.equal(ids['transactions-table-body'].children.length, 1);
  assert.equal(tabs[2].current, true);
});

test('Modified tab clicks retain the native open-in-new-tab behavior', () => {
  const { context, ids, tabs } = transactionPage('attention', []);
  context.applyTransactionView();
  for (const modifiers of [{ ctrlKey: true }, { metaKey: true }, { shiftKey: true }, { button: 1 }]) {
    context.navigateTransactionView({ currentTarget: tabs[2], button: 0, ...modifiers, preventDefault() { assert.fail('Modified click was intercepted'); } });
    assert.equal(ids['transaction-filter'].value, 'attention');
  }
});

test('Switching tabs preserves loading and API error states', async () => {
  const { context, ids, tabs } = transactionPage('attention', []);
  let rejectRequest;
  context.TransactionApi.getTransactions = () => new Promise((resolve, reject) => { rejectRequest = reject; });
  context.applyTransactionView();
  const request = context.loadTransactions();
  context.navigateTransactionView({ currentTarget: tabs[1], button: 0, preventDefault() {} });
  assert.equal(ids['transactions-state'].textContent, 'Loading transactions...');
  rejectRequest(new Error('Connection failed'));
  await request;
  context.navigateTransactionView({ currentTarget: tabs[2], button: 0, preventDefault() {} });
  assert.match(ids['transactions-state'].textContent, /Unable to load transactions/);
  assert.equal(ids['transactions-state'].hidden, false);
  assert.equal(ids['transactions-table-body'].children.length, 0);
});

test('Merged records retain operator actions while Admin remains read-only', () => {
  for (const [status, action] of [['Pending', 'Generate QR'], ['QRGenerated', 'Verify'], ['VerificationPending', 'Verify'], ['Verified', 'Complete'], ['EnergyTransferInProgress', 'Complete']]) {
    for (const role of ['Admin', 'MicrogridOperator']) {
      const row = transactionContext(role).createTransactionRow({ id: 'tx/1', status });
      const links = row.children.find(cell => cell.className === 'transaction-row-actions').children;
      assert.equal(links[0].textContent, 'View Details');
      assert.equal(links.length, role === 'MicrogridOperator' ? 2 : 1, `${role}: ${status}`);
      if (role === 'MicrogridOperator') {
        assert.equal(links[1].textContent, action);
        assert.match(links[1].href, /id=tx%2F1$/);
      }
    }
  }
});

test('Record and workflow links bypass clean-URL redirects while retaining the selected ID', () => {
  const context = transactionContext('MicrogridOperator', 'https://example.test/microgrid/pages/M3/transactions?view=all');
  for (const status of ['Pending', 'QRGenerated', 'Verified', 'Completed']) {
    const row = context.createTransactionRow({ id: 'selected-transaction', status });
    const links = row.children.find(cell => cell.className === 'transaction-row-actions').children;
    for (const link of links) {
      const url = new URL(link.href);
      assert.ok(url.pathname.startsWith('/microgrid/pages/M3/transaction-'));
      assert.equal(url.pathname.endsWith('.html'), false);
      assert.equal(url.searchParams.get('id'), 'selected-transaction');
    }
  }
});

test('Legacy operations route redirects only after passing the operator guard', () => {
  for (const allowed of [true, false]) {
    let redirected;
    let requiredRole;
    const context = vm.createContext({
      URL,
      document: { addEventListener: (name, callback) => callback() },
      AuthGuard: { requireRole: role => { requiredRole = role; return allowed; } },
      window: { location: { href: 'https://example.test/microgrid/pages/M3/dashboard.html', replace: url => { redirected = url; } } }
    });
    vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/transactions/dashboard.js'), 'utf8'), context);
    assert.equal(requiredRole, 'MicrogridOperator');
    assert.equal(redirected, allowed ? 'https://example.test/microgrid/pages/M3/transactions.html?view=attention' : undefined);
  }
});
