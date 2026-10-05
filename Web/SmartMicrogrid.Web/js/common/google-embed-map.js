/* Google Maps Embed API is used only by explicitly selected dashboard pages. */
class GoogleEmbedMapView {
  constructor(element) {
    this.key = API_CONFIG.GOOGLE_MAPS_EMBED_KEY?.trim();
    if (!this.key) throw new Error('Google Maps Embed API key is missing.');
    this.element = element;
    this.frame = document.createElement('iframe');
    this.frame.className = 'grid-map-google-frame';
    this.frame.title = 'Selected microgrid location on Google Maps';
    this.frame.loading = 'lazy';
    this.frame.referrerPolicy = 'strict-origin-when-cross-origin';
    this.frame.allowFullscreen = true;
    element.replaceChildren(this.frame);
  }

  setLocation(latitude, longitude, label) {
    const url = new URL('https://www.google.com/maps/embed/v1/place');
    url.search = new URLSearchParams({
      key: this.key,
      q: `${latitude},${longitude}`,
      center: `${latitude},${longitude}`,
      zoom: '16',
      maptype: 'roadmap'
    }).toString();
    this.frame.title = `${label || 'Selected microgrid'} location on Google Maps`;
    // Refreshing dashboard data should not reload an unchanged map.
    if (this.frame.src !== url.href) this.frame.src = url.href;
  }

  clear() { this.frame.removeAttribute('src'); }
  destroy() { this.element.replaceChildren(); }
}
