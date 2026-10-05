/* ==========================================================================
   Smart Microgrid Energy System - Central API Configuration
   ========================================================================== */

const API_CONFIG = {
  MAP_TILE_URL: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  // Dynamically uses the current host's IP/hostname (e.g. 192.168.1.6 or localhost) on port 5050
  BASE_URL: (typeof window !== 'undefined' && window.location.hostname)
    ? `${window.location.protocol}//${window.location.hostname}:5050/api`
    : 'http://localhost:5050/api',
  TOKEN_KEY: 'smart_microgrid_token',
  USER_KEY: 'smart_microgrid_user',
  TIMEOUT_MS: 15050
};
