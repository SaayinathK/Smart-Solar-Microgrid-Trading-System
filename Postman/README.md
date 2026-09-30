# Postman API Collection & Testing

## Overview
The `Postman/` folder contains importable **Postman Collections (v2.1)** for the endpoints included in each collection. The User Management collection covers authentication and user endpoints; it does not include M3 transaction requests.

---

## 1. Importable Collection File

- **[SmartMicrogrid_UserManagement.postman_collection.json](SmartMicrogrid_UserManagement.postman_collection.json)**
- **[SmartMicrogrid_Component4_PlatformAdministration.postman_collection.json](SmartMicrogrid_Component4_PlatformAdministration.postman_collection.json)** — M4 backoffice endpoints: dashboard, system health, configuration, activity trail, reports, and the account lifecycle. Run *Login as Administrator* first; it captures the JWT into `{{authToken}}` for every other request. Includes an Access Control Checks folder for the negative cases (401 anonymous, 403 non-admin) and is safe to run repeatedly — it leaves no account suspended, deleted, or demoted.

---

## 2. Included Request Endpoints

### A. Authentication
- `POST /api/auth/register` (Registration supports Prosumer and MicrogridOperator accounts; Admin role self-registration is restricted)
- `POST /api/auth/login` (Returns JWT token & auto-stores into `{{authToken}}`)
- `POST /api/auth/change-password` (Authenticated password change)

### B. Current User Profile
- `GET /api/users/me` (Fetch logged-in profile)
- `PUT /api/users/me` (Update logged-in profile)

### C. Admin User Management
- `GET /api/users` (Get all users with search, role & account-status filter)
- `POST /api/users` (Create user with role assignment)
- `GET /api/users/{id}` (Get user by ID)
- `PUT /api/users/{id}` (Update user details)
- `PATCH /api/users/{id}/status` (Legacy boolean activate / deactivate)
- `PATCH /api/users/{id}/account-status` (Full lifecycle: Active, Inactive, Suspended, Pending — see the M4 collection)
- `PATCH /api/users/{id}/role` (Change role assignment; refused for the last active administrator)
- `DELETE /api/users/{id}` (Delete user; refused for the last active administrator)

---

## 3. How to Use in Postman

1. Open **Postman**.
2. Click **Import** → Select `SmartMicrogrid_UserManagement.postman_collection.json`.
3. Set collection variable `baseUrl` (Default: `http://localhost:5050/api`).
4. Run the **Login** request (`POST /api/auth/login`).
   *The test script automatically captures the returned JWT token into the `{{authToken}}` collection variable.*
5. Run any protected endpoint request.
