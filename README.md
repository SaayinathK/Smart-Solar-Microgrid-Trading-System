# Smart Microgrid Energy Management & Trading System

A local-network Smart Microgrid Energy Management & Energy Trading platform built with **C# ASP.NET Core Web API**, **MongoDB**, and **Vanilla HTML5/CSS3/JavaScript**.

---

## 1. System Architecture Overview

```
                          [ Local LAN / Wi-Fi Network ]
                                        │
             ┌──────────────────────────┴──────────────────────────┐
             ▼                                                     ▼
┌──────────────────────────┐                             ┌───────────────────┐
│ Native Android App       │                             │ Web Application   │
│ (Kotlin + XML + SQLite)  │                             │ (HTML + CSS + JS) │
└────────────┬─────────────┘                             └─────────┬─────────┘
             │                                                     │
             └──────────────────────────┬──────────────────────────┘
                                        │ HTTP / JSON REST APIs
                                        ▼
                         ┌─────────────────────────────┐
                         │ IIS Web Server              │
                         │ C# ASP.NET Core Web API     │
                         └──────────────┬──────────────┘
                                        │
                                        ▼
                         ┌─────────────────────────────┐
                         │ Local MongoDB Database      │
                         │ (SmartMicrogridDB)          │
                         └─────────────────────────────┘
```

> [!IMPORTANT]
> Neither the Web Application nor the Android Application connect directly to MongoDB. The **C# Web API** is the sole authoritative communication layer.

---

## 2. University Team Member Division

The complete system is split into four distinct component responsibilities:

| Member | Component & Feature Domain | Status | Folder / Location |
| :--- | :--- | :--- | :--- |
| **Member 1 (Lead)** | **User Management**, Microgrid Infrastructure & Energy Capacity Management | **User Management Implemented** | `Backend/`, `Web/`, `Android/` |
| **Member 2** | Prosumer Energy Trading & Reservation Management | Prepared for integration | `Web/`, `Android/` |
| **Member 3** | Energy Transaction Verification & Operator Fulfilment (QR Scanning) | Prepared for integration | `Web/`, `Android/` |
| **Member 4** | Microgrid Operations Administration & Analytics Reports | Prepared for integration | `Web/`, `Backend/` |

---

## 3. Technology Stack

- **Backend API**: C# (.NET 8), ASP.NET Core Web API
- **Database**: MongoDB (MongoDB Community Server `mongodb://localhost:27017`)
- **Database Driver**: `MongoDB.Driver`
- **Security & Auth**: JWT Bearer Tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`), `BCrypt.Net-Next`
- **Web Frontend**: HTML5, Vanilla CSS3, JavaScript ES6
- **Android App (Future)**: Kotlin, XML, Retrofit, SQLite
- **API Documentation & Testing**: Swagger UI (`/swagger`), Postman Collection
- **Web Server Hosting**: IIS (Internet Information Services)

---

## 4. Current Implementation Status

### ✅ Currently Implemented:
- **Repository Architecture**: Clean 4-member shared structure (`Backend/`, `Web/`, `Android/`, `Database/`, `Documentation/`, `Deployment/`).
- **MongoDB Integration**: `MongoDbContext`, `MongoDbSettings`, indexing for `users` collection.
- **Authentication**: Registration (Prosumer default), Login with JWT token issuance, Password hashing with BCrypt.
- **Role Enforcement**: `Admin`, `MicrogridOperator`, `Prosumer`, `TransactionVerifier` backend authorization rules.
- **User Profile Management**: View profile (`/api/users/me`), update profile, change password.
- **Admin User Management**: View all users, search/filter, create user, update details, activate/deactivate account, change role, delete user.
- **Web Frontend Application**: Responsive, high-aesthetic glassmorphism web UI connecting to REST API endpoints.

### ⏳ Future Modules (Members 1-4):
- Microgrid Nodes & Solar Capacity Management
- Battery Storage Monitoring
- Energy Time Slot Creation & Browsing
- Reservation & Cancellation Management
- Transaction Processing & QR Scanning Verification
- Administrative Dashboards & Operational Reports

---

## 5. Repository Directory Structure

```text
SmartMicrogrid/
├── Backend/
│   └── SmartMicrogrid.API/            # C# ASP.NET Core Web API (.NET 8)
│       ├── Controllers/                # AuthController & UserController
│       ├── Data/                       # MongoDbContext & Connection Settings
│       ├── DTOs/                       # Request/Response Data Transfer Objects
│       ├── Helpers/                    # BCrypt Password & JWT Helpers
│       ├── Middleware/                 # Global Exception Handler
│       ├── Models/ Common/             # User, Role Enum, ApiResponse Models
│       ├── Repositories/               # UserRepository Implementation
│       └── Services/                   # AuthService & UserService
├── Web/
│   └── SmartMicrogrid.Web/             # Pure HTML5 / CSS3 / JS Web App
│       ├── css/                        # Core Design System & Component Styles
│       ├── js/ config/                 # Centralized api-config.js (Base URL)
│       ├── js/ api/                    # ApiClient, AuthApi, UserApi
│       ├── js/ common/                 # AuthGuard, SessionManager, Navbar
│       ├── pages/ users/               # Users Table, Details, Create, Edit UI
│       └── index.html, login.html, register.html, dashboard.html
├── Android/                            # Placeholder & Guide for Kotlin Android App
├── Database/                           # MongoDB Setup & Seed Instructions
├── Documentation/                      # Architecture & API Specifications
├── Deployment/                         # IIS Hosting & Configuration Guide
├── Postman/                            # Importable Postman v2.1 Collection
├── README.md                           # Root Documentation
└── .gitignore                          # Global Ignore Rules
```

---

## 6. How to Run the Project Locally

### Step 1: Start MongoDB
Ensure MongoDB Community Server is installed and running on `localhost:27017`.
Database `SmartMicrogridDB` will be created automatically upon initial request.

### Step 2: Run C# Web API Backend
Navigate to the API folder and run:
```powershell
cd Backend\SmartMicrogrid.API
dotnet run
```
The Web API will launch at:
- **HTTP**: `http://localhost:5050`
- **Swagger Documentation**: `http://localhost:5050/swagger`

### Step 3: Run Web Application
You can open `Web\SmartMicrogrid.Web\index.html` directly in any standard browser or use a lightweight local HTTP server (such as VS Code Live Server or `python -m http.server 8080`).

---

## 7. Configurable LAN API Setup

To run over Wi-Fi / local network for multi-device testing:

1. Locate your laptop local LAN IP address (e.g. `192.168.1.50`).
2. Update `Web/SmartMicrogrid.Web/js/config/api-config.js`:
   ```javascript
   const API_CONFIG = {
     BASE_URL: 'http://192.168.1.50:5050/api'
   };
   ```
3. Update Android app API base URL similarly.

---

## 8. API Endpoint Reference

| Method | Endpoint | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | Register new Prosumer account |
| `POST` | `/api/auth/login` | Public | Authenticate user & return JWT Token |
| `POST` | `/api/auth/change-password` | Authenticated | Change authenticated user password |
| `GET` | `/api/users/me` | Authenticated | Get current logged-in user profile |
| `PUT` | `/api/users/me` | Authenticated | Update current logged-in profile details |
| `GET` | `/api/users` | Admin Only | Get all system users (supports search/filter) |
| `GET` | `/api/users/{id}` | Admin or Self | Get specific user by BSON ID |
| `POST` | `/api/users` | Admin Only | Create user with assigned role |
| `PUT` | `/api/users/{id}` | Admin Only | Update user details & status |
| `PATCH` | `/api/users/{id}/status` | Admin Only | Activate or deactivate account |
| `PATCH` | `/api/users/{id}/role` | Admin Only | Change user role assignment |
| `DELETE`| `/api/users/{id}` | Admin Only | Delete user account |

---

## 9. API Testing with Postman

1. Open Postman.
2. Import `Postman/SmartMicrogrid_UserManagement.postman_collection.json`.
3. Run the **Login** request (`POST /api/auth/login`).
4. The test script automatically stores the JWT token in `{{authToken}}` collection variable.
5. All protected endpoints will automatically include `Authorization: Bearer {{authToken}}`.

---

## 10. Git Collaboration Rules for Team Members

### Recommended Branching Convention
- `main`: Stable release code ready for demonstration.
- `develop`: Shared integration branch.
- `feature/member1-user-management`: User management & infrastructure.
- `feature/member2-reservation`: Prosumer trading & reservations.
- `feature/member3-transaction`: Verification & QR scanner.
- `feature/member4-admin`: Administrative dashboards & analytics.

### Guidelines for Team Members 2, 3, and 4
1. **Backend Extensions**: Add your new Controllers in `Backend/SmartMicrogrid.API/Controllers/`, Models in `Models/`, and Repositories in `Repositories/`.
2. **Frontend Extensions**: Add your web feature pages inside `Web/SmartMicrogrid.Web/pages/<your_module>/` and reuse `ApiClient` (`js/api/api-client.js`).
3. **Android Application**: Build native Kotlin screens under `Android/` using Retrofit connecting to the C# Web API endpoints.
