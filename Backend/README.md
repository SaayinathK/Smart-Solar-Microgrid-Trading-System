# Backend - Smart Microgrid C# ASP.NET Core Web API

## Overview
The `Backend/` folder contains the central communication layer for the **Smart Microgrid Energy Management & Trading System**. It is built using **C# ASP.NET Core Web API (.NET 8)** and interacts with **MongoDB Community Server** (`SmartMicrogridDB`).

> [!IMPORTANT]
> The Web API is the single authoritative layer. Client applications (Web Browser & Android App) **MUST NOT** connect directly to MongoDB.

---

## OpenStreetMap address geocoding

Authenticated Admin and MicrogridOperator users can call `GET /api/geocoding?address=...` and `GET /api/geocoding/reverse?latitude=...&longitude=...`. The backend uses Nominatim and returns `data: { lat, lng, formattedAddress, isApproximate }`. No API key is required.

`OpenStreetMap:GeocodingBaseUrl` in `appsettings.json` defaults to `https://nominatim.openstreetmap.org/`. Set `OpenStreetMap__GeocodingBaseUrl` to an HTTPS Nominatim-compatible provider or self-hosted endpoint to switch services. Optional machine-local settings can be kept in gitignored `appsettings.Local.json`; environment/CLI settings override them. Restart the API after deployment.

The singleton service identifies itself as SmartMicrogrid/1.0, shares a one-request-per-1.1-second limiter across search and reverse lookup, caches up to 1,000 successful results for 24 hours and bounds request queue waits. The browser searches on completed edits or explicit Search/Enter, never autocomplete. A provider rate-limit response triggers a cooldown. Network/provider errors leave manual entry available.

Review the [Nominatim usage policy](https://operations.osmfoundation.org/policies/nominatim/) before deployment: public access is limited to moderate, user-triggered requests, at most one per second **across the entire application**, with attribution and caching. Use a self-hosted/provider endpoint or shared limiter for multiple API instances or higher traffic. This implementation does not support bulk geocoding.

Errors use the normal API envelope: 400 for invalid input, 404 for no address, 429 for a busy queue, 503 for provider throttling/configuration, 504 for timeout and 502 for network/invalid provider responses. Request URL logging is disabled. Run `dotnet test Backend/SmartMicrogrid.API.Tests` for fake-provider coverage without internet access.

## 1. Architecture Flow
```text
HTTP / JSON REST Request
          │
          ▼
    [ Controllers ]     ──> Receives requests & handles HTTP responses
          │
          ▼
     [ Services ]       ──> Contains business logic & validation rules
          │
          ▼
   [ Repositories ]     ──> Handles MongoDB BSON collection operations
          │
          ▼
   [ MongoDbContext ]   ──> Manages MongoClient & MongoDB database connection
          │
          ▼
      [ MongoDB ]       ──> Local Server Database (`mongodb://localhost:27017`)
```

---

## 2. Project Directory Layout

```text
Backend/
└── SmartMicrogrid.API/
    ├── Controllers/
    │   ├── AuthController.cs          # Public login, registration, change password
    │   └── UserController.cs          # Profile management & Admin user CRUD
    ├── Data/
    │   ├── MongoDbContext.cs          # MongoDB connection & index configuration
    │   ├── MongoDbSettings.cs         # Configuration model for appsettings.json
    │   ├── MongoCollections.cs        # Collection name constants
    │   └── DbSeeder.cs                # Automatic test user seed logic
    ├── DTOs/
    │   ├── Auth/                      # RegisterDto, LoginDto, LoginResponseDto, ChangePasswordDto
    │   └── Users/                     # CreateUserDto, UpdateUserDto, UserResponseDto, UpdateStatusDto, UpdateRoleDto
    ├── Helpers/
    │   ├── PasswordHelper.cs          # BCrypt password hashing & verification
    │   └── JwtHelper.cs               # JWT Bearer token generation
    ├── Middleware/
    │   └── ExceptionMiddleware.cs     # Uniform global exception handler
    ├── Models/ Common/
    │   ├── User.cs                    # MongoDB User document model
    │   ├── Role.cs                    # Role Enum (Admin, MicrogridOperator, Prosumer)
    │   └── ApiResponse.cs             # Generic API JSON response wrapper
    ├── Repositories/
    │   ├── Interfaces/IUserRepository.cs
    │   └── Implementation/UserRepository.cs
    ├── Services/
    │   ├── Interfaces/IAuthService.cs, IUserService.cs
    │   └── Implementation/AuthService.cs, UserService.cs
    ├── appsettings.json               # Main configuration file
    ├── appsettings.Development.json
    ├── Program.cs                     # Startup DI, JWT, CORS, & Swagger setup
    └── SmartMicrogrid.API.csproj
```

---

## 3. Technology Stack & Packages

| Package / Technology | Version | Purpose |
| :--- | :--- | :--- |
| **.NET SDK** | `.NET 8.0` | Primary framework runtime |
| **`MongoDB.Driver`** | `3.12.0` | Official C# MongoDB driver |
| **`Microsoft.AspNetCore.Authentication.JwtBearer`** | `8.0.4` | JWT Token validation middleware |
| **`BCrypt.Net-Next`** | `4.2.0` | Secure password hashing algorithm |

---

## 4. Environment & Configuration (`appsettings.json`)

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "SmartMicrogridDB"
  },
  "Jwt": {
    "SecretKey": "SmartMicrogrid_Super_Secure_JWT_Secret_Key_2026_EAD_University_Project!",
    "Issuer": "SmartMicrogridAPI",
    "Audience": "SmartMicrogridClients",
    "ExpiryInMinutes": 480
  }
}
```

---

## 5. How to Run Locally

```powershell
cd Backend\SmartMicrogrid.API
dotnet run
```

- **Base URL**: `http://localhost:5050/api`
- **Swagger Documentation UI**: `http://localhost:5050/swagger`

---

## 6. How Team Members Add New Modules (Members 1 - 4)

1. **Define BSON Model**: Create new document model in `Models/` with `[BsonId]` and `[BsonRepresentation(BsonType.ObjectId)]`.
2. **Add Collection Reference**: Register new collection in `Data/MongoCollections.cs` and expose via `MongoDbContext.cs`.
3. **Build Repository**: Define interface in `Repositories/Interfaces/` and implement in `Repositories/Implementation/`.
4. **Build Service**: Define business interface in `Services/Interfaces/` and implement in `Services/Implementation/`.
5. **Add Controller**: Create new `[ApiController]` in `Controllers/` using `[Authorize]` attributes.
6. **Register DI**: Register interface and implementation in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IMyNewRepository, MyNewRepository>();
   builder.Services.AddScoped<IMyNewService, MyNewService>();
   ```
