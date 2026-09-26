/* OpenStreetMap location selection with independent server address lookup. */
class LocationPicker {
  constructor(container) {
    this.container = container;
    this.selection = null;
    this.pending = false;
    this.revision = 0;
    container.innerHTML = `
      <div class="location-picker-heading"><strong>Find the microgrid location on the map</strong><p>Search, click the map, or drag the pin to fill the fields below. You can also type the address and coordinates directly.</p></div>
      <div class="location-picker-tools"><button type="button" class="btn btn-secondary btn-sm" data-search>Find typed address</button><button type="button" class="btn btn-secondary btn-sm" data-locate>Use my current location</button></div>
      <div data-picker-map class="location-picker-map" aria-label="Choose microgrid location on OpenStreetMap"></div>
      <p data-picker-message class="location-picker-message" role="status" aria-live="polite">Loading OpenStreetMap...</p><p class="location-map-attribution">Address data &copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap contributors</a></p>`;
    this.message = container.querySelector('[data-picker-message]');
    this.locateButton = container.querySelector('[data-locate]');
    this.fields = Object.fromEntries(['location', 'latitude', 'longitude'].map(id => [id, document.getElementById(id)]));
    this.addressStatus = document.getElementById('location-status');
    this.fields.location.addEventListener('input', () => this.addressInput());
    this.fields.location.addEventListener('change', () => this.lookupAddress());
    this.fields.location.addEventListener('keydown', event => {
      if (event.key === 'Enter') { event.preventDefault(); this.lookupAddress(); }
    });
    [this.fields.latitude, this.fields.longitude].forEach(field => {
      field.addEventListener('input', () => this.manualInput());
      field.addEventListener('change', () => { this.manualInput(); this.movePin(); });
    });
    this.locateButton.addEventListener('click', () => this.locate());
    container.querySelector('[data-search]').addEventListener('click', () => this.lookupAddress());
    this.ready = this.initialize();
  }

  setMessage(message) { this.message.textContent = message; }

  static validCoordinates(latitude, longitude) {
    return Number.isFinite(latitude) && Number.isFinite(longitude)
      && Math.abs(latitude) <= 90 && Math.abs(longitude) <= 180
      && !(latitude === 0 && longitude === 0);
  }

  async initialize() {
    try {
      this.mapView = new OpenStreetMapView(this.container.querySelector('[data-picker-map]'), {
        selectable: true,
        onSelect: (latitude, longitude) => this.chooseCoordinates(latitude, longitude),
        onTileError: () => this.setMessage('Map tiles could not load. You can still look up an address or enter coordinates manually.')
      });
      if (this.selection) this.movePin();
      this.setMessage('Click the map, drag the pin, or enter an address below.');
    } catch (error) { this.mapUnavailable(); }
  }

  mapUnavailable() {
    this.mapFailed = true;
    this.container.querySelector('[data-picker-map]').hidden = true;
    this.setMessage('The map preview is unavailable. Type an address below to fetch coordinates, or enter them manually.');
  }

  async chooseCoordinates(latitude, longitude, address) {

    this.addressStatus.textContent = 'Location selected from the map. You can adjust the address or coordinates.';
    const revision = ++this.revision;
    if (!LocationPicker.validCoordinates(latitude, longitude)) {
      this.pending = false;
      this.setMessage('Please choose a valid microgrid location.');
      return;
    }
    this.selection = { latitude, longitude, location: address || '' };
    this.writeFields();
    this.movePin();
    if (address) {
      this.pending = false;
      this.setMessage('Location selected. Drag the pin to fine-tune its position.');
      return;
    }
    this.pending = true;
    this.setMessage('Fetching the address for the selected location...');
    try {
      const response = await MicrogridApi.reverseGeocode(latitude, longitude);
      if (revision !== this.revision) return;
      if (!response.success || !response.data?.formattedAddress) throw new Error('No address');
      this.selection.location = response.data.formattedAddress;
      this.setMessage('Location selected. Drag the pin to fine-tune its position.');
    } catch (error) {
      if (revision !== this.revision) return;
      this.selection.location = `Map location (${latitude.toFixed(6)}, ${longitude.toFixed(6)})`;
      this.setMessage('Coordinates selected. No address was found; this map location will be saved.');
    } finally {
      if (revision === this.revision) { this.pending = false; this.writeFields(); }
    }
  }

  locate() {

    if (!navigator.geolocation) { this.setMessage('Current location is unavailable in this browser. Search or click the map instead.'); return; }
    const revision = ++this.revision;
    this.pending = true;
    this.locateButton.disabled = true;
    this.setMessage('Finding your current location...');
    navigator.geolocation.getCurrentPosition(position => {
      this.locateButton.disabled = false;
      if (revision !== this.revision) return;
      this.chooseCoordinates(position.coords.latitude, position.coords.longitude);
    }, error => {
      this.locateButton.disabled = false;
      if (revision !== this.revision) return;
      this.pending = false;
      this.setMessage(error.code === 1 ? 'Location permission was denied. Search or click the map instead.' : 'Could not find your current location. Search or click the map instead.');
    }, { enableHighAccuracy: true, timeout: 12000, maximumAge: 0 });
  }

  setSavedLocation(node) {

    ++this.revision;
    this.pending = false;
    this.fields.location.value = node.location || '';
    if (!LocationPicker.validCoordinates(node.latitude, node.longitude)) return;
    this.selection = { latitude: node.latitude, longitude: node.longitude, location: node.location || `Map location (${node.latitude}, ${node.longitude})` };
    this.writeFields();
    this.movePin();
  }

  writeFields() {
    this.fields.latitude.value = this.selection.latitude;
    this.fields.longitude.value = this.selection.longitude;
    this.fields.location.value = this.selection.location;
  }

  movePin() {
    if (this.mapFailed || !this.mapView || !this.selection) return;
    this.mapView.setLocation(this.selection.latitude, this.selection.longitude);
  }

  readFields() {
    const location = this.fields.location.value.trim();
    const latitudeText = String(this.fields.latitude.value).trim();
    const longitudeText = String(this.fields.longitude.value).trim();
    const latitude = Number(latitudeText);
    const longitude = Number(longitudeText);
    if (!location || !latitudeText || !longitudeText || !LocationPicker.validCoordinates(latitude, longitude)) return null;
    return { location, latitude, longitude };
  }

  manualInput() {
    // A typed correction supersedes any in-flight place, address, or device lookup.

    ++this.revision;
    this.pending = false;
    this.selection = this.readFields();
    this.addressStatus.textContent = 'Coordinates can also be entered manually.';
    if (!this.selection) this.mapView?.clear();
    this.setMessage(this.selection
      ? 'Entered location ready to save. You can edit it or select another point on the map.'
      : 'Enter an address, latitude (-90 to 90), and longitude (-180 to 180), or choose a point on the map.');
  }

  addressInput() {
    this.manualInput();
    // Coordinates for the previous address must not be saved with a new address.
    this.fields.latitude.value = '';
    this.fields.longitude.value = '';
    this.selection = null;
    this.mapView?.clear();
    if (!this.fields.location.value.trim()) {
      this.addressStatus.textContent = 'Type an address to automatically find its latitude and longitude.';
      return;
    }
    this.addressStatus.textContent = 'Finish editing the address, press Enter, or choose Find typed address to fill coordinates.';
  }

  async lookupAddress() {

    const address = this.fields.location.value.trim();
    const revision = this.revision;
    // Change/Enter/Search can refer to the same in-flight request.
    if (!address || this.addressLookupRevision === revision) return;
    if (address.length < 3 || address.length > 500) {
      this.addressStatus.textContent = 'Enter an address between 3 and 500 characters.';
      return;
    }
    this.addressLookupRevision = revision;
    this.pending = true;
    this.addressStatus.textContent = 'Finding coordinates for this address...';
    let timeout;
    try {
      const response = await Promise.race([
        MicrogridApi.geocodeAddress(address),
        new Promise((resolve, reject) => { timeout = setTimeout(() => reject(new Error('Lookup timed out')), 10000); })
      ]);
      if (revision !== this.revision) return;
      if (!response.success) throw new Error(response.message || 'Address lookup failed.');
      const match = response.data;
      if (!match || !LocationPicker.validCoordinates(match.lat, match.lng))
        throw new Error('Address lookup returned invalid coordinates. Try another address or enter coordinates manually.');
      // Keep the user's address wording while using the returned coordinates.
      this.selection = { location: address, latitude: match.lat, longitude: match.lng };
      this.writeFields();
      this.movePin();
      this.addressStatus.textContent = match.isApproximate
        ? 'Coordinates filled from an approximate match. Check the pin and adjust it to the exact site.'
        : 'Latitude and longitude filled automatically. Check the pin before saving.';
      this.setMessage('Address located. You can adjust the map pin or edit the coordinates.');
    } catch (error) {
      if (revision !== this.revision) return;
      this.addressStatus.textContent = error.message || 'Could not fetch coordinates. Try again or enter latitude and longitude manually.';
      // Allow an explicit change/Enter retry after a failed lookup.
      this.addressLookupRevision = null;
    } finally {
      clearTimeout(timeout);
      if (revision === this.revision) this.pending = false;
    }
  }

  getSelection() {
    const selection = this.readFields();
    if (!selection) {
      this.setMessage('Enter an address and valid latitude/longitude, or choose a location on the map before saving.');
      this.container.scrollIntoView({ block: 'center', behavior: 'smooth' });
      return null;
    }
    // Valid fields can always be saved, even if the map is unavailable or a lookup is stuck.

    ++this.revision;
    this.pending = false;
    this.selection = selection;
    return { ...selection };
  }
}
