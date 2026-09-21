# Smart Microgrid Database Setup (MongoDB)

## Primary Server Database
- **Database Engine**: MongoDB Community Server
- **Recommended Tools**: MongoDB Compass / `mongosh`
- **Default Connection String**: `mongodb://localhost:27017`
- **Database Name**: `SmartMicrogridDB`

## Main Collections Overview

### 1. `users` (User Management - Implemented)
Stores user credentials, profiles, roles, and activation status.
- **Indexes**:
  - `Email` (Unique, Ascending)

### 2. `microgrid_nodes` / `capacity` (Member 1 - Future)
Stores solar node data, battery storage capacities, availability slots.

### 3. `reservations` (Member 2 - Future)
Stores prosumer energy reservations and slot locks.

### 4. `transactions` (Member 3 - Future)
Stores energy transfer transactions, verification statuses, and QR validation tokens.

### 5. `audit_logs` / `system_reports` (Member 4 - Future)
Stores system activity logs and operational analytics snapshots.

## Seed Admin Credentials
To initialize an initial Admin user via MongoDB Compass or shell, run the API endpoint `/api/auth/register` or create a seed user using the backend user creation service.
