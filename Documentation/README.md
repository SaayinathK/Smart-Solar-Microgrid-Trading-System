# System Documentation

## Overview
The `Documentation/` folder contains comprehensive architectural and design specifications for the **Smart Microgrid Energy Management & Trading System**.

---

## Index of Documents

- **[Architecture.md](Architecture.md)**: High-level 3-tier system architecture, role model, component mappings, and M3 transaction workflow.
- **[M2-Reservation-Management.md](M2-Reservation-Management.md)**: M2 reservation lifecycle and its integration boundary with M3 transactions.
- **[Components/Component-1-Microgrid-Management.md](Components/Component-1-Microgrid-Management.md)**: M1 microgrid, capacity, battery, and energy-slot management.
- **[Components/M4-Platform-Administration.md](Components/M4-Platform-Administration.md)**: M4 administration dashboard, user/role administration, audit trail, system health, configuration, and reporting.

---

## Member Component Mapping Summary

| Member | Domain & Modules | Main Responsibilities |
| :--- | :--- | :--- |
| **Member 1 (Lead)** | Infrastructure & Capacity | User Management (Implemented), Solar Nodes, Battery Storage, Energy Slots |
| **Member 2** | Prosumer Energy Trading | Energy Search, Slot Browsing, Reservations & Modification Tracking |
| **Member 3 (M3/C3)** | Energy Transaction & Verification Management | Transactions from approved reservations, QR generation/verification, energy transfer confirmation, transaction completion, and transaction history |
| **Member 4** | Operations & Administration | System Dashboard Monitoring, Audit Logs, Operational Analytics & Reports |
