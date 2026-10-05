# Postman API Collection & Testing

## Overview
The `Postman/` folder contains importable **Postman Collections (v2.1)** for the endpoints included in each collection. The User Management collection covers authentication and user endpoints; it does not include M3 transaction requests.

---

## 1. Importable Collection File

- **[SmartMicrogrid_UserManagement.postman_collection.json](SmartMicrogrid_UserManagement.postman_collection.json)**

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
- `GET /api/users` (Get all users with search & role filter)
- `POST /api/users` (Create user with role assignment)
- `GET /api/users/{id}` (Get user by ID)
- `PUT /api/users/{id}` (Update user details)
- `PATCH /api/users/{id}/status` (Activate / deactivate account)
- `PATCH /api/users/{id}/role` (Change role assignment)
- `DELETE /api/users/{id}` (Delete user)

---

## 3. How to Use in Postman

1. Open **Postman**.
2. Click **Import** → Select `SmartMicrogrid_UserManagement.postman_collection.json`.
3. Set collection variable `baseUrl` (Default: `http://localhost:5050/api`).
4. Run the **Login** request (`POST /api/auth/login`).
   *The test script automatically captures the returned JWT token into the `{{authToken}}` collection variable.*
5. Run any protected endpoint request.
