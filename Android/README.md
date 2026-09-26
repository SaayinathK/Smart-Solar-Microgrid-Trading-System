# Native Android Client Application

## Overview
The `Android/` folder is designated for the **Native Android Application** built using **Kotlin**, **XML Layouts**, **Android Studio**, **Retrofit**, and **SQLite**.

## Dashboard map

The dashboard uses **Leaflet 1.9.4 and OpenStreetMap** in a local WebView. Selecting a microgrid centers its saved coordinates and marker; selection survives tab/view recreation. No map API key or Play Services Maps dependency is needed.

The map page, library, marker images and BSD license are bundled in `SmartMicrogrid.Android/app/src/main/assets/map`. `OpenStreetMapView.kt` serves those assets from an internal HTTPS origin, forwards selected coordinates as JSON, identifies tile requests as SmartMicrogrid/1.0, and keeps normal HTTP caching. There is no JavaScript bridge. Attribution links open in the browser when tapped. Tiles require internet access; a failed tile request shows a retry action.

The tile URL is defined in `assets/map/map.js`. Keep visible attribution and follow the [OpenStreetMap tile usage policy](https://operations.osmfoundation.org/policies/tiles/): interactive viewing only, no bulk downloads or offline prefetch. For a larger deployment use an appropriate hosted or self-hosted tile service.

---

## 1. System Communication Architecture

```text
┌─────────────────────────────────┐
│  Native Android Application     │
│  (Kotlin + XML Layouts)         │
│  ├── Retrofit HTTP Client       │
│  └── SQLite Local Database      │
└────────────────┬────────────────┘
                 │ HTTP / REST JSON (JWT Authorization)
                 ▼
┌─────────────────────────────────┐
│  IIS / C# ASP.NET Core Web API  │
└────────────────┬────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│  MongoDB Server                 │
│  (SmartMicrogridDB)             │
└─────────────────────────────────┘
```

> [!IMPORTANT]
> The Android application **MUST NOT** connect directly to MongoDB. All data access must route through the C# ASP.NET Core REST Web API hosted on IIS over local LAN / Wi-Fi.

---

## 2. Responsibilities Breakdown

- **Member 2**: Prosumer Mobile Interface (Browse Energy Slots, Create & Track Reservations).
- **Member 3**: Energy Transaction Verification Interface (Camera QR Code Scanning & Verification).

---

## 3. SQLite Role
SQLite is used strictly for **Android local persistence**:
- Caching JWT authentication tokens (`SharedPreferences` / `EncryptedSharedPreferences`).
- Storing offline transaction drafts & session state.
- Caching recently viewed energy availability slots.

*MongoDB remains the authoritative server-side database.*

---

## 4. Setup Instructions for Team Members

1. Initialize your Android Studio project inside this `Android/` directory using **Kotlin** and **XML layouts**.
2. Add Retrofit dependencies in `build.gradle.kts`:
   ```kotlin
   implementation("com.squareup.retrofit2:retrofit:2.9.0")
   implementation("com.squareup.retrofit2:converter-gson:2.9.0")
   ```
3. Configure your base API URL to point to the server laptop's LAN IP address:
   ```kotlin
   object ApiConfig {
       const val BASE_URL = "http://192.168.1.50:5050/api/"
   }
   ```
4. Attach JWT Bearer tokens in Retrofit Interceptors for protected endpoints:
   ```kotlin
   val request = originalRequest.newBuilder()
       .header("Authorization", "Bearer $jwtToken")
       .build()
   ```
