# System Architecture & Component Mapping

## High Level Architecture
The **Smart Microgrid Energy Management & Trading System** is designed as a modular 3-tier architecture operating over a local LAN/Wi-Fi network.

```
+--------------------------+       +--------------------------+
|  Native Android App      |       |  Web Application         |
|  (Kotlin + XML + SQLite) |       |  (HTML5 + CSS3 + JS)     |
+--------------------------+       +--------------------------+
             |                                  |
             +----------------+-----------------+
                              | HTTP / REST JSON
                              v
             +----------------------------------+
             |  IIS Web Server                  |
             |  ASP.NET Core 8 Web API          |
             +----------------------------------+
                              |
                              v
             +----------------------------------+
             |  MongoDB Server                  |
             |  (SmartMicrogridDB)              |
             +----------------------------------+
```

## System Roles
1. **Admin**: User management, system monitoring, audit logs.
2. **MicrogridOperator**: Infrastructure, nodes, capacity, battery status, slots, and assigned M3 transaction operations.
3. **Prosumer**: Energy search, slot reservation, reservation tracking.

These are the three system roles. Transaction verification is an operational responsibility of `MicrogridOperator`, not a separate role. Admin has monitoring/view access to transactions; Prosumer views their own transaction information through the Mobile/Android application.

## Team Member Breakdown
- **Member 1 (Current Lead)**: Project Foundation, User Management, Microgrid Nodes & Capacity.
- **Member 2**: Prosumer Energy Trading & Reservations.
- **Member 3 (M3/C3)**: Owns transaction management, QR generation and verification, energy transfer confirmation, transaction completion, and transaction history. M3 consumes approved reservation information from M2 and required microgrid/energy-slot information from M1. The workflow is: approved reservation → create transaction and transaction ID → generate QR → Prosumer displays QR → MicrogridOperator verifies QR and validates reservation/user/microgrid/energy slot/time → confirm energy transfer → transaction completed → transaction history. M3 does not own reservation lifecycle, microgrid management, or user/role management.
- **Member 4**: Microgrid Operations Administration & Reports.
