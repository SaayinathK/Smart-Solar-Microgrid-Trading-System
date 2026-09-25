# Database - Smart Microgrid MongoDB Setup

## Overview
The `Database/` folder documents the primary server-side database engine for the **Smart Microgrid Energy Management & Trading System**.

- **Database Engine**: MongoDB Community Server
- **Recommended GUI**: MongoDB Compass
- **Default Connection String**: `mongodb://localhost:27017`
- **Database Name**: `SmartMicrogridDB`

---

## 1. MongoDB Collections & Schemas

### A. `users` Collection (Implemented)
Stores user credentials, profile attributes, role assignments, and account activation states.

```json
{
  "_id": { "$oid": "66ee1234567890abcdef1234" },
  "firstName": "System",
  "lastName": "Administrator",
  "email": "admin@microgrid.com",
  "phoneNumber": "+10000000001",
  "passwordHash": "$2a$11$...",
  "role": "Admin",
  "isActive": true,
  "createdAt": { "$date": "2026-09-21T16:00:00.000Z" },
  "updatedAt": { "$date": "2026-09-21T16:00:00.000Z" }
}
```

**Indexes**:
- `Email` (Unique, Ascending) - Configured automatically by `MongoDbContext`.

---

### B. `microgrid_nodes` & `capacity` Collections (Member 1 - Future)
Stores solar node data, solar panel capacities, battery storage levels, and generated energy slots.

---

### C. `reservations` Collection (Member 2 - Future)
Stores energy slot browsing locks, prosumer reservations, and reservation modification logs.

---

### D. `transactions` Collection (Member 3 - Future)
Stores energy transfer transaction orders, QR code verification hashes, and completion confirmations.

---

### E. `audit_logs` Collection (Member 4 - Future)
Stores system audit records, administrative activity logs, and performance metric snapshots.

---

## 2. Automatic Seed Data

When the C# Web API starts up, `DbSeeder` checks if collections are empty. If empty, it populates the following test data:

### Users (12 accounts)

| Name | Email | Password | Role | Active |
| :--- | :--- | :--- | :--- | :---: |
| System Administrator | `admin@microgrid.com` | `Admin123!` | `Admin` | ✅ |
| Nimal Fernando | `nimal.admin@microgrid.com` | `Admin123!` | `Admin` | ✅ |
| Alex Perera | `operator@microgrid.com` | `Operator123!` | `MicrogridOperator` | ✅ |
| Kavindu Silva | `kavindu.op@microgrid.com` | `Operator123!` | `MicrogridOperator` | ✅ |
| Lakshmi Ratnayake | `lakshmi.op@microgrid.com` | `Operator123!` | `MicrogridOperator` | ✅ |
| Sam Jayawardena | `prosumer@microgrid.com` | `Prosumer123!` | `Prosumer` | ✅ |
| Amaya De Silva | `amaya@microgrid.com` | `Prosumer123!` | `Prosumer` | ✅ |
| Ruwan Bandara | `ruwan@microgrid.com` | `Prosumer123!` | `Prosumer` | ✅ |
| Dilini Wickramasinghe | `dilini@microgrid.com` | `Prosumer123!` | `Prosumer` | ❌ |
| Tharushi Kumari | `tharushi@microgrid.com` | `Prosumer123!` | `Prosumer` | ✅ |
| Valerie Cooray | `verifier@microgrid.com` | `Verifier123!` | `TransactionVerifier` | ✅ |
| Ishara Gunasekara | `ishara.verifier@microgrid.com` | `Verifier123!` | `TransactionVerifier` | ✅ |

### Microgrid Nodes (10)

Colombo, Kandy, Galle, Jaffna, Negombo, Nuwara Eliya, Trincomalee, Matara, Batticaloa, and Anuradhapura — with varied capacities, battery levels, and statuses (Active, Maintenance, Offline).

### Energy Slots (12)

Slots spread across active microgrids with varied availability, pricing ($18–$32/kWh), and statuses (Available, PartiallyReserved, FullyReserved).

### Reservations (12)

Mixed-status reservations (Pending, Approved, Completed, Rejected, Cancelled) linked to prosumer NICs, with full status history trails.

---

## 3. Viewing Database in MongoDB Compass

1. Open **MongoDB Compass**.
2. Connect to `mongodb://localhost:27017`.
3. In the left panel, click on **`SmartMicrogridDB`** → browse `users`, `microgrid_nodes`, `energy_slots`, or `reservations`.

