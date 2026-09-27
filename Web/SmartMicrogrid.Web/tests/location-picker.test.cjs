const test = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');

function setup({ configured = true } = {}) {
  const elements = new Map();
  const element = key => {
    if (!elements.has(key)) elements.set(key, {
      value: '', textContent: '', disabled: false, listeners: {},
      addEventListener(event, callback) { this.listeners[event] = callback; },
      setAttribute() {}, append() {}, scrollIntoView() {},
      querySelector: selector => element(selector)
    });
    return elements.get(key);
  };
  const requests = [];
  class MapView {
    constructor(container, options) {
      if (!configured) throw new Error('Map unavailable');
      this.options = options;
      this.marker = { position: null };
    }
    setLocation(lat, lng) { this.marker.position = { lat, lng }; this.marker.map = this; }
    clear() { this.marker.map = null; }
  }
  const context = vm.createContext({
    OpenStreetMapView: MapView, window: {}, API_CONFIG: {},
    document: { getElementById: element }, navigator: {}, setTimeout, clearTimeout,
    MicrogridApi: {
      geocodeAddress: address => new Promise((resolve, reject) => requests.push({ query: { address }, resolve, reject })),
      reverseGeocode: (latitude, longitude) => new Promise((resolve, reject) => requests.push({ query: { latitude, longitude }, resolve, reject }))
    }
  });
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../js/common/location-picker.js'), 'utf8') + '\nglobalThis.Picker = LocationPicker;', context);
  const picker = new context.Picker(element('container'));
  return { picker, context, element, requests };
}

test('new form has no default location and cannot save before selecting a point', async () => {
  const { picker, element } = setup();
  await picker.ready;
  assert.equal(picker.getSelection(), null);
  assert.equal(element('latitude').value, '');
  assert.equal(picker.mapView.marker.map, undefined);
});

test('map selection saves exact coordinates and the fetched address', async () => {
  const { picker, requests, element } = setup();
  await picker.ready;
  const pending = picker.chooseCoordinates(6.927123456, 79.861234567);
  assert.equal(picker.getSelection(), null);
  assert.equal(element('location').value, '');
  requests[0].resolve({ success: true, data: { formattedAddress: 'Colombo, Sri Lanka' } });
  await pending;
  assert.equal(picker.getSelection().latitude, 6.927123456);
  assert.equal(picker.getSelection().longitude, 79.861234567);
  assert.equal(element('location').value, 'Colombo, Sri Lanka');
});

test('older reverse-geocoding replies cannot overwrite a newer pin', async () => {
  const { picker, requests, element } = setup();
  await picker.ready;
  const old = picker.chooseCoordinates(6.9, 79.8);
  const current = picker.chooseCoordinates(7.2, 80.5);
  requests[1].resolve({ success: true, data: { formattedAddress: 'New point' } });
  await current;
  requests[0].resolve({ success: true, data: { formattedAddress: 'Old point' } });
  await old;
  assert.equal(element('location').value, 'New point');
  assert.equal(picker.getSelection().latitude, 7.2);
});

test('dragging the marker updates selection and uses coordinate fallback if no address exists', async () => {
  const { picker, requests, element } = setup();
  await picker.ready;
  picker.mapView.options.onSelect(7.1, 80.2);
  requests[0].reject(new Error('ZERO_RESULTS'));
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(picker.getSelection().longitude, 80.2);
  assert.equal(element('location').value, 'Map location (7.100000, 80.200000)');
});

test('saved location restores before map initialization and retains precision', async () => {
  const { picker, element, requests } = setup();
  picker.setSavedLocation({ latitude: 7.123456789, longitude: 80.987654321, location: 'Existing site' });
  await picker.ready;
  assert.equal(picker.mapView.marker.position.lat, 7.123456789);
  assert.equal(element('location').value, 'Existing site');
  assert.equal(requests.length, 0);
});


test('late device location does not replace a point selected while it was pending', async () => {
  const { picker, context } = setup();
  await picker.ready;
  let locateSuccess;
  context.navigator.geolocation = { getCurrentPosition(success) { locateSuccess = success; } };
  picker.locate();
  await picker.chooseCoordinates(7.4, 80.6, 'User selected site');
  locateSuccess({ coords: { latitude: 6.9, longitude: 79.8 } });
  assert.equal(picker.getSelection().location, 'User selected site');
});

test('denied device location leaves the saved location intact', async () => {
  const { picker, context } = setup();
  await picker.ready;
  picker.setSavedLocation({ latitude: 6.9, longitude: 79.8, location: 'Saved site' });
  context.navigator.geolocation = { getCurrentPosition(success, failure) { failure({ code: 1 }); } };
  picker.locate();
  assert.match(picker.message.textContent, /permission was denied/);
  assert.equal(picker.getSelection().location, 'Saved site');
});

test('missing map library shows an error and cannot create an unlocated node', async () => {
  const { picker } = setup({ configured: false });
  await picker.ready;
  assert.match(picker.message.textContent, /map preview is unavailable/);
  assert.equal(picker.locateButton.disabled, false);
  assert.equal(picker.getSelection(), null);
});

function typeLocation(element, values) {
  for (const [id, value] of Object.entries(values)) {
    element(id).value = String(value);
    element(id).listeners.input();
  }
}

test('manual location can be saved with an unavailable map', async () => {
  const { picker, element } = setup({ configured: false });
  await picker.ready;
  typeLocation(element, { location: '  Dehiwala  ', latitude: 6.851234567, longitude: 79.861234567 });
  assert.equal(picker.getSelection().location, 'Dehiwala');
  assert.equal(picker.getSelection().latitude, 6.851234567);
  assert.equal(element('[data-picker-map]').hidden, true);
});

test('manual corrections supersede a pending reverse-geocoding response', async () => {
  const { picker, element, requests } = setup();
  await picker.ready;
  const lookup = picker.chooseCoordinates(6.9, 79.8);
  typeLocation(element, { location: 'Manually corrected address', latitude: 7.123456789, longitude: 80.5 });
  requests[0].resolve({ success: true, data: { formattedAddress: 'Old map address' } });
  await lookup;
  assert.equal(picker.getSelection().location, 'Manually corrected address');
  assert.equal(picker.getSelection().latitude, 7.123456789);
});


test('map failure keeps lookup independent and allows manual save', async () => {
  const { picker, context, requests, element } = setup();
  await picker.ready;
  const lookup = picker.chooseCoordinates(6.9, 79.8);
  picker.mapUnavailable();
  assert.equal(picker.pending, true);
  typeLocation(element, { location: 'Offline entry', latitude: 7.1, longitude: 80.1 });
  requests[0].resolve({ success: true, data: { formattedAddress: 'Late map result' } });
  await lookup;
  assert.equal(picker.getSelection().location, 'Offline entry');
  assert.equal(element('[data-picker-map]').hidden, true);
});

test('save reads current fields even when a lookup is stuck or input events were not fired', async () => {
  const { picker, element } = setup();
  await picker.ready;
  picker.pending = true;
  element('location').value = 'Autofilled site';
  element('latitude').value = '6.99';
  element('longitude').value = '79.99';
  assert.equal(picker.getSelection().location, 'Autofilled site');
  assert.equal(picker.pending, false);
});

test('blank, nonnumeric and out-of-range manual coordinates cannot save an old selection', async () => {
  const { picker, element } = setup();
  await picker.ready;
  for (const values of [
    { latitude: '', longitude: '80' },
    { latitude: '7', longitude: '' },
    { latitude: '91', longitude: '80' },
    { latitude: '7', longitude: '-181' },
    { latitude: 'NaN', longitude: '80' },
    { latitude: '0', longitude: '0' }
  ]) {
    picker.setSavedLocation({ location: 'Existing location', latitude: 7, longitude: 80 });
    typeLocation(element, values);
    assert.equal(picker.getSelection(), null);
  }
  typeLocation(element, { location: '   ', latitude: 7, longitude: 80 });
  assert.equal(picker.getSelection(), null);
});

test('users can return to map selection after typing, and manual coordinate changes move the pin', async () => {
  const { picker, element } = setup();
  await picker.ready;
  typeLocation(element, { location: 'Typed site', latitude: 7.5, longitude: 80.5 });
  element('longitude').listeners.change();
  assert.equal(picker.mapView.marker.position.lat, 7.5);
  await picker.chooseCoordinates(6.9, 79.8, 'New map site');
  assert.equal(picker.getSelection().location, 'New map site');
  assert.equal(element('latitude').value, 6.9);
});

const addressResult = (latitude = 6.851234567, longitude = 79.861234567) => ({
  success: true, data: { lat: latitude, lng: longitude, formattedAddress: 'Provider formatted address', isApproximate: false }
});

test('typing does not send autocomplete requests; completing the address fills coordinates', async () => {
  const { picker, element, requests } = setup();
  await picker.ready;
  typeLocation(element, { location: 'Dehiwala' });
  typeLocation(element, { location: '25/3, Srimabodhi road, Dehiwala, Sri Lanka' });
  assert.equal(requests.length, 0);
  await new Promise(resolve => setTimeout(resolve, 850));
  assert.equal(requests.length, 0);
  element('location').listeners.change();
  assert.equal(requests.length, 1);
  assert.equal(requests[0].query.address, '25/3, Srimabodhi road, Dehiwala, Sri Lanka');
  requests[0].resolve(addressResult());
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(picker.getSelection().latitude, 6.851234567);
  assert.equal(picker.getSelection().longitude, 79.861234567);
  assert.equal(picker.getSelection().location, '25/3, Srimabodhi road, Dehiwala, Sri Lanka');
  assert.equal(picker.mapView.marker.position.lat, 6.851234567);
});

test('leaving the address field fetches immediately and does not duplicate the request', async () => {
  const { picker, element, requests } = setup();
  await picker.ready;
  typeLocation(element, { location: 'Colombo, Sri Lanka' });
  const lookup = element('location').listeners.change();
  await element('location').listeners.change();
  assert.equal(requests.length, 1);
  requests[0].resolve(addressResult());
  await lookup;
  assert.equal(picker.getSelection().latitude, 6.851234567);
});

test('changing an address clears old coordinates and ignores stale address responses', async () => {
  const { picker, element, requests } = setup();
  await picker.ready;
  picker.setSavedLocation({ location: 'Original site', latitude: 7.2, longitude: 80.5 });
  typeLocation(element, { location: 'First address' });
  assert.equal(element('latitude').value, '');
  assert.equal(element('longitude').value, '');
  assert.equal(picker.getSelection(), null);
  const old = picker.lookupAddress();
  typeLocation(element, { location: 'Second address' });
  const current = picker.lookupAddress();
  requests[1].resolve(addressResult(7.1, 80.1));
  await current;
  requests[0].resolve(addressResult(6.9, 79.9));
  await old;
  assert.equal(picker.getSelection().location, 'Second address');
  assert.equal(picker.getSelection().latitude, 7.1);
});

test('manual coordinates override a pending forward address lookup', async () => {
  const { picker, element, requests } = setup();
  await picker.ready;
  typeLocation(element, { location: 'New site' });
  const lookup = picker.lookupAddress();
  typeLocation(element, { latitude: 7.5, longitude: 80.5 });
  requests[0].resolve(addressResult());
  await lookup;
  assert.equal(picker.getSelection().latitude, 7.5);
  assert.equal(picker.getSelection().longitude, 80.5);
});

test('address lookup failure leaves coordinates blank and permits manual fallback', async () => {
  const { picker, element, requests } = setup();
  await picker.ready;
  typeLocation(element, { location: 'Unknown address' });
  const lookup = picker.lookupAddress();
  requests[0].reject(new Error('Address lookup unavailable. You can enter latitude and longitude manually.'));
  await lookup;
  assert.equal(picker.pending, false);
  assert.match(element('location-status').textContent, /enter latitude and longitude manually/);
  assert.equal(element('latitude').value, '');
  typeLocation(element, { latitude: 6.9, longitude: 79.9 });
  assert.equal(picker.getSelection().latitude, 6.9);
});

test('unknown and approximate address matches are clearly reported', async () => {
  const { picker, element, requests } = setup();
  await picker.ready;
  typeLocation(element, { location: 'Unknown address' });
  const missing = picker.lookupAddress();
  requests[0].reject({ status: 404, message: 'Address not found. Include the city and country.' });
  await missing;
  assert.match(element('location-status').textContent, /Address not found/);
  typeLocation(element, { location: 'Colombo' });
  const approximate = picker.lookupAddress();
  const result = addressResult();
  result.data.isApproximate = true;
  requests[1].resolve(result);
  await approximate;
  assert.match(element('location-status').textContent, /approximate match/);
});

test('address lookup uses the backend even when the browser map cannot load', async () => {
  const { picker, element, requests } = setup({ configured: false });
  await picker.ready;
  typeLocation(element, { location: 'Dehiwala, Sri Lanka' });
  const lookup = picker.lookupAddress();
  requests[0].resolve(addressResult());
  await lookup;
  assert.equal(picker.getSelection().latitude, 6.851234567);
  assert.equal(element('[data-picker-map]').hidden, true);
});

test('a late map error cannot cancel a backend address lookup', async () => {
  const { picker, context, element, requests } = setup();
  await picker.ready;
  typeLocation(element, { location: 'Dehiwala, Sri Lanka' });
  const lookup = picker.lookupAddress();
  picker.mapUnavailable();
  requests[0].resolve(addressResult());
  await lookup;
  assert.equal(picker.getSelection().longitude, 79.861234567);
});

test('backend configuration errors are shown rather than a generic lookup failure', async () => {
  const { picker, element, requests } = setup({ configured: false });
  await picker.ready;
  typeLocation(element, { location: 'Dehiwala' });
  const lookup = picker.lookupAddress();
  requests[0].reject({ status: 503, message: 'Address lookup is not configured on the server.' });
  await lookup;
  assert.equal(element('location-status').textContent, 'Address lookup is not configured on the server.');
});

