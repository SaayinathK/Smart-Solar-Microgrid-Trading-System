/* Leaflet 1.9.4 + OpenStreetMap. Library files are bundled locally. */
class OpenStreetMapView {
  constructor(element, { selectable = false, onSelect, onTileError } = {}) {
    this.map = L.map(element, { scrollWheelZoom: false }).setView([6.9271, 79.8612], 11);
    this.tiles = L.tileLayer(API_CONFIG.MAP_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors'
    }).addTo(this.map);
    this.tiles.on('tileerror', () => onTileError?.());
    this.marker = L.marker([0, 0], { draggable: selectable, title: 'Microgrid location', autoPan: true });
    if (selectable) {
      this.map.on('click', event => onSelect(event.latlng.lat, event.latlng.lng));
      this.marker.on('dragend', () => {
        const position = this.marker.getLatLng();
        onSelect(position.lat, position.lng);
      });
    }
  }

  setLocation(latitude, longitude, label) {
    this.map.invalidateSize();
    this.marker.setLatLng([latitude, longitude]).addTo(this.map);
    if (label) {
      const text = document.createElement('span');
      text.textContent = label;
      this.marker.bindPopup(text);
    }
    this.map.setView([latitude, longitude], 16);
  }

  clear() { this.marker.remove(); }
}
