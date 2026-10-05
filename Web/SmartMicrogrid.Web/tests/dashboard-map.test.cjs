const test = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');

function setup({ provider = 'google', key = 'test-browser-key', nodes = [] } = {}) {
  const elements = new Map();
  function element() {
    return {
      children: [], dataset: {}, attributes: {}, listeners: {}, hidden: false,
      setAttribute(name, value) { this.attributes[name] = value; },
      removeAttribute(name) { delete this[name]; },
      addEventListener(name, handler) { this.listeners[name] = handler; },
      append(...children) { this.children.push(...children); },
      replaceChildren(...children) { this.children = children; },
      querySelector(selector) {
        if (selector === '[data-switch-map]' && provider !== 'google') return null;
        if (!elements.has(selector)) elements.set(selector, element());
        return elements.get(selector);
      }
    };
  }
  const osmViews = [];
  class OpenStreetMapView {
    constructor() { osmViews.push(this); }
    setLocation(...location) { this.location = location; }
    clear() { this.location = null; }
    destroy() { this.destroyed = true; }
  }
  const context = vm.createContext({
    API_CONFIG: { GOOGLE_MAPS_EMBED_KEY: key }, URL, URLSearchParams, OpenStreetMapView,
    document: { createElement: element, addEventListener() {} },
    MicrogridApi: { getMicrogrids: async () => ({ success: true, data: nodes }) }
  });
  for (const file of ['google-embed-map.js', 'microgrid-map.js']) {
    vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common', file), 'utf8'), context);
  }
  vm.runInContext('globalThis.DashboardMap = MicrogridMap;', context);
  const container = element();
  container.dataset.mapProvider = provider;
  const map = new context.DashboardMap(container);
  return { map, context, osmViews };
}

const colombo = { id: '1', name: 'Colombo solar', location: 'Colombo', latitude: 6.927123456, longitude: 79.861234567 };
const kandy = { id: '2', name: 'Kandy solar', location: 'Kandy', latitude: 7.29, longitude: 80.63 };

test('Google dashboard displays exact saved coordinates and moves to the selected node', async () => {
  const { map, osmViews } = setup({ nodes: [colombo, kandy] });
  await map.load();
  const frame = map.mapView.frame;
  let url = new URL(frame.src);
  assert.equal(url.origin + url.pathname, 'https://www.google.com/maps/embed/v1/place');
  assert.equal(url.searchParams.get('q'), '6.927123456,79.861234567');
  assert.equal(url.searchParams.get('key'), 'test-browser-key');
  assert.equal(frame.referrerPolicy, 'strict-origin-when-cross-origin');
  assert.equal(frame.title, 'Colombo solar location on Google Maps');
  assert.equal(osmViews.length, 0);
  map.list.children[1].listeners.click();
  url = new URL(frame.src);
  assert.equal(url.searchParams.get('q'), '7.29,80.63');
  assert.equal(map.selectedId, '2');
  assert.equal(map.list.children[1].attributes['aria-pressed'], 'true');
});

test('refreshing unchanged node data does not reload the Google iframe', async () => {
  const { map } = setup({ nodes: [colombo] });
  await map.load();
  const frame = map.mapView.frame;
  let src = frame.src;
  let writes = 0;
  Object.defineProperty(frame, 'src', { get: () => src, set: value => { writes++; src = value; } });
  await map.load();
  assert.equal(writes, 0);
});

test('fallback and return to Google preserve the selected node and dispose of old maps', async () => {
  const { map, osmViews } = setup({ nodes: [colombo, kandy] });
  await map.load();
  map.select(kandy);
  map.providerButton.listeners.click();
  assert.equal(map.provider, 'openstreetmap');
  assert.deepEqual(osmViews[0].location, [7.29, 80.63, 'Kandy solar']);
  assert.equal(map.frame.children.length, 0);
  map.providerButton.listeners.click();
  assert.equal(osmViews[0].destroyed, true);
  assert.equal(map.selectedId, '2');
  assert.equal(new URL(map.mapView.frame.src).searchParams.get('q'), '7.29,80.63');
});

test('other dashboards use only OpenStreetMap even when a Google key is configured', async () => {
  const other = setup({ provider: '', nodes: [colombo] });
  vm.runInContext('GoogleEmbedMapView = class { constructor() { throw new Error("Must not load Google"); } };', other.context);
  await other.map.load();
  assert.equal(other.osmViews.length, 1);
  assert.equal(other.map.providerButton, null);
  assert.equal(other.map.frame.hidden, false);
});

test('empty lists and missing coordinates never create an embedded map', async () => {
  for (const nodes of [[], [{ ...colombo, latitude: 0, longitude: 0 }], [{ ...colombo, latitude: null }]]) {
    const { map } = setup({ nodes });
    await map.load();
    assert.equal(map.mapView, undefined);
    assert.equal(map.frame.hidden, true);
  }
});

test('selecting an unlocated node clears the previous Google location', async () => {
  const missing = { ...kandy, latitude: null };
  const { map } = setup({ nodes: [colombo, missing] });
  await map.load();
  map.select(missing);
  assert.equal(map.mapView.frame.src, undefined);
  assert.equal(map.frame.hidden, true);
  map.select(colombo);
  assert.equal(map.frame.hidden, false);
  assert.equal(new URL(map.mapView.frame.src).searchParams.get('q'), '6.927123456,79.861234567');
});

test('missing Google configuration still permits explicit OpenStreetMap fallback', async () => {
  const { map, osmViews } = setup({ key: '', nodes: [colombo] });
  await map.load();
  assert.match(map.message.textContent, /Use OpenStreetMap/);
  map.providerButton.listeners.click();
  assert.equal(osmViews.length, 1);
  assert.equal(map.frame.hidden, false);
});

test('only the main web dashboard opts into Google Maps', () => {
  const read = file => fs.readFileSync(path.join(__dirname, '..', file), 'utf8');
  assert.match(read('dashboard.html'), /data-map-provider="google"/);
  for (const page of ['dashboard', 'create-microgrid', 'edit-microgrid']) {
    const html = read(`pages/M1/${page}.html`);
    assert.doesNotMatch(html, /data-map-provider="google"|google-embed-map\.js|maps\.googleapis\.com/);
    assert.match(html, /openstreet-map\.js/);
  }
});
