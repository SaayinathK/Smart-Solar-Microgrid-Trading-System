const map = L.map('map').setView([6.9271, 79.8612], 11);
const tiles = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
  maxZoom: 19,
  attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
}).addTo(map);
const marker = L.marker([0, 0]);
let pickerEnabled = false;
let pickedLocation = null;
window.enableLocationPicker = function (enabled) { pickerEnabled = Boolean(enabled); };
window.getPickedLocation = function () { return pickedLocation; };
map.on('click', event => {
  if (!pickerEnabled) return;
  pickedLocation = { latitude: event.latlng.lat, longitude: event.latlng.lng };
  marker.setLatLng(event.latlng).unbindPopup().addTo(map);
});
tiles.on('tileerror', () => { document.getElementById('status').hidden = false; });
tiles.on('tileload', () => { document.getElementById('status').hidden = true; });
document.getElementById('retry').addEventListener('click', () => tiles.redraw());
window.showMicrogrid = function (node) {
  marker.remove();
  if (!node || !Number.isFinite(node.latitude) || !Number.isFinite(node.longitude)) return;
  pickedLocation = { latitude: node.latitude, longitude: node.longitude };
  const label = document.createElement('span');
  label.textContent = node.name;
  marker.setLatLng([node.latitude, node.longitude]).bindPopup(label).addTo(map);
  map.invalidateSize();
  map.setView([node.latitude, node.longitude], 15);
};
