/* ==========================================================================
   Smart Microgrid Energy System - Central API Configuration
   ========================================================================== */

const API_CONFIG = {
  // Browser key for the main dashboard only. Restrict to Maps Embed API and allowed website origins.
  GOOGLE_MAPS_EMBED_KEY: 'AIzaSyDmVj6iVmhXxLmJdk9mE8aou5F8NA9Iumg',
  MAP_TILE_URL: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  // Configurable REST API Base URL (Change to IP address for LAN deployment e.g. http://192.168.1.50:5050/api)
  BASE_URL: 'http://localhost:5050/api',
  TOKEN_KEY: 'smart_microgrid_token',
  USER_KEY: 'smart_microgrid_user',
  TIMEOUT_MS: 15050
};
