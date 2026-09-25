# Postman API Collection & Testing

## Overview
The `Postman/` folder contains the official importable **Postman Collection (v2.1)** for testing all REST API endpoints of the **Smart Microgrid Energy Management & Trading System**.

---

## 1. Importable Collection File

- **[SmartMicrogrid_UserManagement.postman_collection.json](SmartMicrogrid_UserManagement.postman_collection.json)**

---

## 2. Included Request Endpoints

### A. Authentication
- `POST /api/auth/register` (Multi-role registration: Prosumer with NIC primary key, Microgrid Operator, Transaction Verifier; Admin role self-registration restricted)
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
3. Set collection variable `baseUrl` (Default: `http://localhost:5000/api`).
4. Run the **Login** request (`POST /api/auth/login`).
   *The test script automatically captures the returned JWT token into the `{{authToken}}` collection variable.*
5. Run any protected endpoint request.
