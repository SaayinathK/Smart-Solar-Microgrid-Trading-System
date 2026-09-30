# Local Area Network (LAN) Configuration Guide

## Architecture Overview
```text
  [ Web Client / Browser ]            [ Native Android Phone ]
   (Connected to Wi-Fi)                 (Connected to Wi-Fi)
            │                                    │
            └─────────────────┬──────────────────┘
                              │ HTTP / REST (Port 5050)
                              ▼
                     [ Server Laptop ]
                     IP: 192.168.1.50 (Example)
                     IIS / ASP.NET Core API
                              │
                              ▼
                        [ MongoDB ]
```

---

## 1. Finding Your Server Laptop's LAN IP
Run command on host laptop:
```cmd
ipconfig
```
Locate `IPv4 Address` under your active Wi-Fi adapter (e.g., `192.168.1.50`).

---

## 2. Windows Firewall Configuration
Allow inbound traffic on port `5050`:
```powershell
New-NetFirewallRule -DisplayName "SmartMicrogrid API Port 5050" -Direction Inbound -LocalPort 5050 -Protocol TCP -Action Allow
```

---

## 3. Configuring Clients

### Web Client (`js/config/api-config.js`)
Update `BASE_URL`:
```javascript
const API_CONFIG = {
  BASE_URL: 'http://192.168.1.50:5050/api'
};
```

### Android Client (`utils/Constants.kt`)
Update `API_BASE_URL`:
```kotlin
object Constants {
    const val API_BASE_URL = "http://192.168.1.50:5050/api/"
}
```
*(For Android Studio emulator, use `http://10.0.2.2:5050/api/`)*
