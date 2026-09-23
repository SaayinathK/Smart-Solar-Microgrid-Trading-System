# Component 1 — Microgrid & Energy Resource Management

## Overview
Component 1 owns all core solar generation nodes, energy capacity allocations, battery storage telemetry, energy availability discovery, energy slot publishing, operational statuses, and infrastructure analytics for the **Smart Microgrid Energy Management & Trading Platform**.

---

## 1. System Responsibilities & Boundaries
Component 1 exclusively owns:
1. **Microgrid Node Management** (`MicrogridNode`): Registration, location parameters, GPS coordinates, generation capacity, operational statuses (Active, Inactive, Maintenance, Offline).
2. **Energy Capacity Management**: Allocation tracking across Total Capacity, Available Capacity, Reserved Capacity, and Used Capacity.
3. **Battery Storage Management**: Battery capacity, stored energy levels, percentage state-of-charge calculation, and battery status.
4. **Energy Slot Management** (`EnergySlot`): Publishing, updating, status changes, and slot cancellation.
5. **Energy Availability Discovery**: Searchable endpoint exposing real-time available energy slots for Component 2 consumption.
6. **Infrastructure Dashboard**: Comprehensive metrics and telemetry charts.

### Out-of-Scope (Delegated Components):
- **Component 2**: Energy Marketplace, Search Business Logic, Reservations.
- **Component 3**: Energy Transactions, QR Code Generation/Scanning, Verification.
- **Component 4**: Global User CRUD, Role Administration, System Audit Reports.

---

## 2. Multi-Platform Architecture
```text
                         SMART MICROGRID PLATFORM
                                  |
              +-------------------+-------------------+
              |                                       |
          WEB CLIENT                            ANDROID CLIENT
       HTML/CSS/JavaScript                    Kotlin + XML
              |                                       |
              +-------------------+-------------------+
                                  |
                           HTTP / JSON / REST (JWT Auth)
                                  |
                         IIS / ASP.NET Core API
                                  |
                         C# Web API Backend
                                  |
                              MongoDB
```

Android Local Storage:
```text
Android Native Client (Kotlin)
    └── Room Local SQLite Database (Offline Cache)
```
