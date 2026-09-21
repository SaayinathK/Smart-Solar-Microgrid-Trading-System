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
2. **MicrogridOperator**: Infrastructure, nodes, capacity, battery status, slots.
3. **Prosumer**: Energy search, slot reservation, reservation tracking.
4. **TransactionVerifier**: Pending transactions, QR scanning, transfer verification.

## Team Member Breakdown
- **Member 1 (Current Lead)**: Project Foundation, User Management, Microgrid Nodes & Capacity.
- **Member 2**: Prosumer Energy Trading & Reservations.
- **Member 3**: Energy Transaction Verification & QR Code Scanning.
- **Member 4**: Microgrid Operations Administration & Reports.
