# Google Maps API Integration & Dual-Provider Architecture

**Project:** Smart Solar Microgrid Energy Management & Trading System  
**Module:** Microgrid Infrastructure & Map Visualization (SE4040 EAD)  
**Rubric Mapping:**  
- **Google Maps API Integration:** 3 Marks  
- **Interactive Map Feature:** 5 Marks  

---

## 1. Overview & Strategy

The assignment brief specifies **Google Maps API integration**, while real-world deployments frequently face Google Cloud Platform (GCP) billing / credit-card payment blocks (`BillingNotEnabledMapError`).

To guarantee full marks for **Google Maps API Integration** without breaking when billing or quotas are constrained, the system adopts a hybrid architecture:

1. **Dashboard Overview (Web) & Home Screen (Android)**:
   - **Google Maps** is active as the primary, default map provider.
   - Built using Google Maps API & Interactive Viewport integration, supporting full gesture navigation, zoom, pan, marker pinning, and deep-linking via the Google Maps Universal URLs / Intent API.
   - Provides a live toggle button: `[ 📍 Google Maps | 🍃 Leaflet OSM ]` allowing examiners to inspect Google Maps by default and test Leaflet OSM dynamically.
2. **Infrastructure Creation & Editing Forms**:
   - Location picker pages (`create-microgrid.html`, `edit-microgrid.html`) utilize **Leaflet with OpenStreetMap** for zero-dependency pin dropping and reverse geocoding.

---

## 2. Web Implementation (`Web/SmartMicrogrid.Web`)

- **Component:** [`js/common/microgrid-map.js`](file:///c:/Saayinath/SLIIT/Y4S1/SE4040_EAD/4_Project/Smart-Solar-Microgrid-Trading-System/SmartMicrogrid/Web/SmartMicrogrid.Web/js/common/microgrid-map.js)
- **Configuration:** [`js/config/api-config.js`](file:///c:/Saayinath/SLIIT/Y4S1/SE4040_EAD/4_Project/Smart-Solar-Microgrid-Trading-System/SmartMicrogrid/Web/SmartMicrogrid.Web/js/config/api-config.js) (`API_CONFIG.GOOGLE_MAPS_API_KEY`)
- **Styles:** [`css/dashboard.css`](file:///c:/Saayinath/SLIIT/Y4S1/SE4040_EAD/4_Project/Smart-Solar-Microgrid-Trading-System/SmartMicrogrid/Web/SmartMicrogrid.Web/css/dashboard.css)

### Key Features:
- **Default Provider**: Automatically initializes and renders the selected microgrid with Google Maps.
- **Direct Google Maps Link**: Includes `"Open in Google Maps ↗"` pointing to `https://www.google.com/maps/search/?api=1&query=${lat},${lng}`.
- **Provider Switcher**: One-click toggle between **Google Maps** and **Leaflet OSM**.

---

## 3. Android Implementation (`Android/SmartMicrogrid.Android`)

- **Google Maps View:** [`GoogleMapView.kt`](file:///c:/Saayinath/SLIIT/Y4S1/SE4040_EAD/4_Project/Smart-Solar-Microgrid-Trading-System/SmartMicrogrid/Android/SmartMicrogrid.Android/app/src/main/java/com/smartmicrogrid/ui/GoogleMapView.kt)
- **Fragment:** [`HomeFragment.kt`](file:///c:/Saayinath/SLIIT/Y4S1/SE4040_EAD/4_Project/Smart-Solar-Microgrid-Trading-System/SmartMicrogrid/Android/SmartMicrogrid.Android/app/src/main/java/com/smartmicrogrid/ui/HomeFragment.kt)
- **Layout:** [`fragment_home.xml`](file:///c:/Saayinath/SLIIT/Y4S1/SE4040_EAD/4_Project/Smart-Solar-Microgrid-Trading-System/SmartMicrogrid/Android/SmartMicrogrid.Android/app/src/main/res/layout/fragment_home.xml)

### Key Features:
- **Default Provider**: When the user opens the Home screen, Google Maps is pre-selected and rendered in `map_container`.
- **Android Intent API**: Tapping `"Open in Maps ↗"` dispatches an `Intent.ACTION_VIEW` targeting `com.google.android.apps.maps` with `geo:lat,lng` coordinates, launching the native Google Maps app directly on the device.
- **Toggle Group**: Material 3 button toggle group (`btn_map_google` vs `btn_map_leaflet`) allows instant switching.
