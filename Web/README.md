# Web Frontend - Smart Microgrid Web Application

## Overview
The `Web/` folder contains the frontend web interface for the **Smart Microgrid Energy Management & Trading System**. It is built using pure **HTML5**, **Vanilla CSS3**, and **JavaScript (ES6)** without external heavy frameworks (No React, Angular, or Vue).

---

## 1. Directory Structure

```text
Web/
└── SmartMicrogrid.Web/
    ├── index.html                     # Landing page with auto-redirect
    ├── login.html                     # User authentication sign-in
    ├── register.html                  # Public Prosumer registration
    ├── dashboard.html                 # Main dashboard overview with stat widgets
    │
    ├── pages/
    │   └── users/
    │       ├── users.html             # Admin user list table with search & filters
    │       ├── user-details.html      # Full user BSON record & timestamps
    │       ├── create-user.html       # Admin create user with role assignment
    │       └── edit-user.html         # Edit profile & password update
    │
    ├── css/
    │   ├── style.css                  # Core design system tokens, Light/Dark variables
    │   ├── sidebar.css                # Professional App layout & Sidebar menu
    │   ├── forms.css                  # Input controls, floating labels, alerts
    │   ├── tables.css                 # Data tables, status badges, action buttons
    │   └── dashboard.css              # Stat cards, top-accent lines, hero banner
    │
    ├── js/
    │   ├── config/
    │   │   └── api-config.js          # Centralized API Base URL configuration
    │   ├── common/
    │   │   ├── theme.js               # ThemeManager (Light Blue default / Dark switcher)
    │   │   ├── session.js             # SessionManager (JWT localStorage helper)
    │   │   ├── auth.js                # AuthGuard (Route guard & role verification)
    │   │   ├── sidebar.js             # Dynamic role-tailored sidebar component
    │   │   └── navbar.js              # Header navbar component
    │   ├── api/
    │   │   ├── api-client.js          # Central Fetch client with Bearer token injection
    │   │   ├── auth-api.js            # Auth endpoint functions
    │   │   └── user-api.js            # User management endpoint functions
    │   └── users/
    │       ├── users.js               # Users table renderer & filter logic
    │       ├── user-details.js        # User details population script
    │       ├── create-user.js         # User creation form handler
    │       └── edit-user.js           # User modification form handler
    └── assets/                        # Static images & icons
```

---

## OpenStreetMap maps and location picker

Both dashboards and the create/edit location picker use the locally bundled **Leaflet 1.9.4** library with OpenStreetMap tiles. No map key or billing configuration is required. Library files and their BSD license are in `SmartMicrogrid.Web/vendor/leaflet`; tiles still need an internet connection.

Select a dashboard microgrid to move the marker to its saved coordinates. In create/edit forms, click the map, drag the pin, choose **Use my current location**, or type the address and leave the field / press Enter / choose **Find typed address**. Address edits fill coordinates through `GET /api/geocoding?address=...`; map selection uses `GET /api/geocoding/reverse?latitude=...&longitude=...`. Both endpoints require an Admin or MicrogridOperator session. Address searches do not run on every keystroke.

Coordinates and addresses remain manually editable if the map or provider is unavailable. Old lookup responses cannot overwrite newer manual edits or selections. Failed reverse lookups retain the selected point with a coordinate-based address. Approximate address matches must be checked on the map. Current location requires browser permission and HTTPS or localhost.

`MAP_TILE_URL` in `js/config/api-config.js` controls the tile provider. See [backend geocoding configuration](../Backend/README.md#openstreetmap-address-geocoding) to change the Nominatim endpoint. Keep the visible OpenStreetMap attribution when changing map styling.

Public services are suitable for moderate interactive use, not unrestricted production traffic. [Nominatim policy](https://operations.osmfoundation.org/policies/nominatim/) requires an application-wide maximum of one request per second, identification, caching and no autocomplete. The backend serializes and caches forward/reverse requests in one process. Use a self-hosted/provider endpoint or a shared rate limiter before running multiple API instances. [Tile policy](https://operations.osmfoundation.org/policies/tiles/) requires attribution, normal HTTP caching, identifying requests and no bulk/offline tile downloads.

Run `node --test Web/SmartMicrogrid.Web/tests/*.test.cjs`. The tests use stubbed providers and do not contact public map services.

## 2. Key Architecture Concepts

### A. Centralized API Configuration (`js/config/api-config.js`)
Do not hardcode API URLs in individual JavaScript files. Change the API base URL in `api-config.js`:
```javascript
const API_CONFIG = {
  BASE_URL: 'http://localhost:5050/api', // Update to LAN IP for Wi-Fi deployment (e.g., http://192.168.1.50:5050/api)
  TOKEN_KEY: 'smart_microgrid_token',
  USER_KEY: 'smart_microgrid_user'
};
```

### B. Theme Management System (`js/common/theme.js`)
- **Default Theme**: Light Mode with Vibrant Royal Blue accents.
- **Dark Mode Switcher**: Toggled via button in sidebar/navbar (`ThemeManager.toggleTheme()`).
- Theme preference persists automatically in `localStorage`.

### C. Role-Based Sidebar Navigation (`js/common/sidebar.js`)
Displays custom navigation options based on the authenticated user's assigned role:
- **`Admin`**: Dashboard, User Management, Operational Reports.
- **`MicrogridOperator`**: Dashboard, Solar Nodes, Battery Storage.
- **`Prosumer`**: Dashboard, Browse Energy Slots, My Reservations.
- **`MicrogridOperator`**: Dashboard, reservation management, QR/pass verification, and transfer completion.

---

## 3. How Team Members Add New Web Pages

1. Create new page in `pages/<your_module>/<page-name>.html`.
2. Wrap page content inside `<div id="app-layout-wrapper">...</div>`.
3. Include core scripts in `<head>` or before `</body>`:
   ```html
   <script src="../../js/config/api-config.js"></script>
   <script src="../../js/common/theme.js"></script>
   <script src="../../js/common/session.js"></script>
   <script src="../../js/api/api-client.js"></script>
   <script src="../../js/common/auth.js"></script>
   <script src="../../js/common/sidebar.js"></script>
   <script>
     AuthGuard.requireAuth(); // Guard page
     renderAppLayout('your-active-page-id', 'Your Page Title');
   </script>
   ```
4. Use `ApiClient` (`js/api/api-client.js`) for all HTTP calls so JWT Bearer tokens are automatically attached to requests.
