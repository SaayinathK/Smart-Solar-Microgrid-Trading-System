const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
function browser(role = null, page = 'pages/reservations/reservations.html') {
  const storage = new Map(role ? [['token', 'existing-token'], ['user', JSON.stringify({ role })]] : []);
  const redirects = [];
  const elements = new Map();
  function element(id) {
    if (!elements.has(id)) elements.set(id, {
      value: id === 'email' ? 'demo@example.com' : 'password',
      classList: { remove() {}, add() {} },
      addEventListener(event, handler) { this[event] = handler; }
    });
    return elements.get(id);
  }
  const location = {
    href: `https://example.com/microgrid/${page}`,
    replace(url) { this.href = url; redirects.push(url); }
  };
  const context = vm.createContext({
    URL, API_CONFIG: { TOKEN_KEY: 'token', USER_KEY: 'user' },
    localStorage: {
      getItem: key => storage.get(key) || null,
      setItem: (key, value) => storage.set(key, value),
      removeItem: key => storage.delete(key)
    },
    window: { location },
    document: {
      baseURI: location.href,
      scripts: [{ src: 'https://example.com/microgrid/js/common/auth.js' }],
      getElementById: element
    },
    ApiClient: { showToast() {} }, renderNavbar() {}, setTimeout: callback => callback()
  });
  for (const file of ['js/common/session.js', 'js/common/auth.js']) {
    vm.runInContext(fs.readFileSync(path.join(root, file), 'utf8'), context);
  }
  return { context, storage, redirects, location, element, run: code => vm.runInContext(code, context) };
}

test('legacy prosumer sessions cannot enter protected pages', () => {
  for (const guard of ["AuthGuard.requireAuth()", "AuthGuard.requireRole(['Prosumer', 'Admin'])"]) {
    const b = browser('Prosumer');
    assert.equal(b.run(guard), false);
    assert.equal(b.storage.size, 0);
    assert.equal(b.location.href, 'https://example.com/microgrid/mobile-redirect.html');
  }
});

test('prosumer sessions on login/register go to mobile guidance and are cleared', () => {
  const b = browser('Prosumer', 'login.html');
  b.run('AuthGuard.redirectIfAuthenticated()');
  assert.equal(b.storage.size, 0);
  assert.match(b.location.href, /mobile-redirect.html$/);
});

test('operator and administrator sessions retain web access and their destinations', () => {
  for (const [role, destination] of [['MicrogridOperator', 'dashboard.html'], ['Admin', 'pages/M4/dashboard.html']]) {
    const b = browser(role);
    assert.equal(b.run(`AuthGuard.requireAuth('${role}')`), true);
    assert.equal(b.run(`AuthGuard.requireRole('${role}')`), true);
    assert.equal(b.run('AuthGuard.getHomeUrl()'), `https://example.com/microgrid/${destination}`);
    b.run('AuthGuard.redirectIfAuthenticated()');
    assert.equal(b.storage.get('token'), 'existing-token');
  }
});

test('unauthenticated direct links return to the app-relative login page', () => {
  const b = browser();
  assert.equal(b.run('AuthGuard.requireAuth()'), false);
  assert.equal(b.location.href, 'https://example.com/microgrid/login.html');
});

test('login never persists a prosumer token; staff login still persists its session', async () => {
  const html = fs.readFileSync(path.join(root, 'login.html'), 'utf8');
  const script = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)][0][1];
  for (const role of ['Prosumer', 'MicrogridOperator', 'Admin']) {
    const b = browser(null, 'login.html');
    b.context.AuthApi = { login: async () => ({ success: true, data: { token: 'new-token', user: { role } } }) };
    b.run(script);
    await b.element('login-form').submit({ preventDefault() {} });
    assert.equal(b.storage.get('token'), role === 'Prosumer' ? undefined : 'new-token');
    assert.match(b.location.href, role === 'Prosumer' ? /mobile-redirect.html$/ : role === 'Admin' ? /pages\/M4\/dashboard.html$/ : /\/dashboard.html$/);
  }
});

test('visiting mobile guidance does not log staff out', () => {
  const html = fs.readFileSync(path.join(root, 'mobile-redirect.html'), 'utf8');
  const script = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)][0][1];
  for (const role of ['Prosumer', 'MicrogridOperator', 'Admin']) {
    const b = browser(role, 'mobile-redirect.html');
    b.run(script);
    assert.equal(b.storage.has('token'), role !== 'Prosumer');
  }
});
