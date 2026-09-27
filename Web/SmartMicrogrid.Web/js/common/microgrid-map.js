/* Shared map and node selector for the overview and infrastructure dashboards. */
class MicrogridMap {
  constructor(container) {
    this.container = container;
    this.nodes = [];
    this.selectedId = null;
    container.innerHTML = `
      <div class="grid-map-heading"><div><h2>Microgrid locations</h2><p data-map-summary role="status">Loading active microgrids...</p></div></div>
      <div class="grid-map-layout">
        <div class="grid-map-canvas">
          <div data-map-frame aria-label="Selected microgrid location on OpenStreetMap" hidden></div>
          <p data-map-message class="grid-map-message" role="status">Loading locations...</p>
        </div>
        <aside class="grid-map-panel" aria-label="Select microgrid">
          <div class="grid-map-panel-heading"><strong>Select microgrid</strong><span data-map-count>0 nodes</span></div>
          <div data-map-list class="grid-map-list"></div>
        </aside>
      </div>
      <p data-map-selection class="grid-map-selection" aria-live="polite">Select a microgrid to view its location.</p>`;
    this.frame = container.querySelector('[data-map-frame]');
    this.message = container.querySelector('[data-map-message]');
    this.list = container.querySelector('[data-map-list]');
  }

  static hasLocation(node) {
    return typeof node.latitude === 'number' && Number.isFinite(node.latitude)
      && typeof node.longitude === 'number' && Number.isFinite(node.longitude)
      && Math.abs(node.latitude) <= 90 && Math.abs(node.longitude) <= 180
      // Older records default missing coordinates to 0,0.
      && !(node.latitude === 0 && node.longitude === 0);
  }

  async load() {
    try {
      const response = await MicrogridApi.getMicrogrids({ status: 'Active', isActive: true });
      if (response.success === false || !Array.isArray(response.data)) throw new Error('Unable to load microgrids');
      this.nodes = response.data;
      this.container.querySelector('[data-map-summary]').textContent = `${this.nodes.length} active microgrids across the network`;
      this.container.querySelector('[data-map-count]').textContent = `${this.nodes.length} nodes`;
      this.list.replaceChildren();
      this.nodes.forEach(node => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'grid-map-node';
        button.dataset.nodeId = node.id;
        button.setAttribute('aria-pressed', 'false');
        const icon = document.createElement('span');
        icon.className = 'grid-map-pin';
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = '⌖';
        const details = document.createElement('span');
        const name = document.createElement('strong');
        name.textContent = node.name;
        const location = document.createElement('small');
        location.textContent = node.location || 'Address unavailable';
        details.append(name, location);
        if (!MicrogridMap.hasLocation(node)) {
          const missing = document.createElement('small');
          missing.textContent = 'Location unavailable';
          details.append(missing);
        }
        button.append(icon, details);
        button.addEventListener('click', () => this.select(node));
        this.list.append(button);
      });
      const selected = this.nodes.find(node => node.id === this.selectedId)
        || this.nodes.find(MicrogridMap.hasLocation) || this.nodes[0];
      if (selected) this.select(selected);
      else {
        this.selectedId = null;
        this.showMessage('No active microgrids to display.');
        this.container.querySelector('[data-map-selection]').textContent = 'Microgrid locations will appear here when available.';
      }
    } catch (error) {
      this.nodes = [];
      this.list.replaceChildren();
      this.container.querySelector('[data-map-count]').textContent = '0 nodes';
      this.container.querySelector('[data-map-summary]').textContent = 'Unable to load microgrid locations.';
      this.container.querySelector('[data-map-selection]').textContent = '';
      this.showMessage('Unable to load microgrids.');
      const retry = document.createElement('button');
      retry.type = 'button';
      retry.className = 'btn btn-secondary btn-sm';
      retry.textContent = 'Retry';
      retry.addEventListener('click', () => this.load());
      this.list.append(retry);
    }
  }

  showMessage(message) {
    this.frame.hidden = true;
    this.mapView?.clear();
    this.message.hidden = false;
    this.message.textContent = message;
  }

  select(node) {
    this.selectedId = node.id;
    Array.from(this.list.children).forEach(button => {
      button.setAttribute('aria-pressed', String(button.dataset.nodeId === node.id));
    });
    this.container.querySelector('[data-map-selection]').textContent = `${node.name} · ${node.location || 'Address unavailable'}`;
    if (!MicrogridMap.hasLocation(node)) {
      this.showMessage('This microgrid does not have a saved map location yet.');
      return;
    }
    this.frame.hidden = false;
    this.message.hidden = true;
    try {
      if (!this.mapView) this.mapView = new OpenStreetMapView(this.frame, {
        onTileError: () => {
          this.container.querySelector('[data-map-selection]').textContent =
            'Map tiles could not load. Check your connection and select a microgrid to retry.';
        }
      });
      this.mapView.setLocation(node.latitude, node.longitude, node.name);
    } catch (error) {
      this.showMessage('The map could not load. Refresh the page to try again.');
    }
  }
}

document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('[data-microgrid-map]').forEach(container => {
    const dashboardMap = new MicrogridMap(container);
    dashboardMap.load();
    document.getElementById('refresh-dashboard')?.addEventListener('click', () => dashboardMap.load());
  });
});
