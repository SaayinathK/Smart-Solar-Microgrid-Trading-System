# Android Client Application Placeholder

## Overview
This folder is designated for the **Native Android Application** built using **Kotlin**, **XML Layouts**, **Retrofit**, and **SQLite**.

## Architecture & Communication Flow
```
Android App (Kotlin + Retrofit)
        ↓ HTTP/JSON
IIS / C# ASP.NET Core Web API
        ↓
    MongoDB
```

> [!IMPORTANT]
> The Android application **MUST NOT** connect directly to MongoDB. All data access must route through the C# ASP.NET Core REST Web API.

## Responsibilities & Modules
- **Member 2**: Prosumer Energy Trading & Reservation Management (Android UI)
- **Member 3**: Energy Transaction Verification & QR Scanning (Android UI)

## Future Implementation Instructions
1. Initialize the Android Studio project in this `Android/` folder using Kotlin and XML layouts.
2. Use Retrofit for consuming the Web API endpoints (`http://<SERVER_IP>:5000/api/...`).
3. Store JWT tokens and session data locally using SQLite / EncryptedSharedPreferences.
