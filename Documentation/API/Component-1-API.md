# Component 1 — REST API Documentation

## Base URL
- Local Development: `http://localhost:5050/api`
- LAN Deployment: `http://192.168.x.x:5050/api`
- Swagger UI: `http://localhost:5050/swagger`

---

## Endpoint Summary

### Microgrid Management
- `GET /api/microgrids`: Retrieve microgrids (QueryParams: `status`, `isActive`, `location`, `search`).
- `GET /api/microgrids/{id}`: Get microgrid details by ID.
- `POST /api/microgrids`: Create microgrid (Role: `MicrogridOperator`, `Admin`).
- `PUT /api/microgrids/{id}`: Update microgrid (Role: `MicrogridOperator`, `Admin`).
- `DELETE /api/microgrids/{id}`: Delete microgrid (Role: `MicrogridOperator`, `Admin`).
- `PATCH /api/microgrids/{id}/status`: Change operational status (Active, Inactive, Maintenance, Offline).

### Capacity Management
- `GET /api/microgrids/{id}/capacity`: Retrieve capacity breakdown.
- `PUT /api/microgrids/{id}/capacity`: Update capacity allocation (Role: `MicrogridOperator`, `Admin`).

### Battery Storage Management
- `GET /api/microgrids/{id}/battery`: Retrieve battery telemetry.
- `PUT /api/microgrids/{id}/battery`: Update battery charge level (Role: `MicrogridOperator`, `Admin`).

### Energy Slot Management
- `GET /api/energy-slots`: List energy slots (QueryParams: `microgridId`, `status`, `startTime`, `endTime`, `minEnergy`).
- `GET /api/energy-slots/{id}`: Get energy slot by ID.
- `POST /api/energy-slots`: Publish new slot (Role: `MicrogridOperator`, `Admin`).
- `PUT /api/energy-slots/{id}`: Update energy slot (Role: `MicrogridOperator`, `Admin`).
- `DELETE /api/energy-slots/{id}`: Delete energy slot (Role: `MicrogridOperator`, `Admin`).
- `PATCH /api/energy-slots/{id}/status`: Update slot status.

### Energy Availability (Component 2 Integration)
- `GET /api/energy-availability`: Get active available slots (QueryParams: `location`, `minimumEnergy`, `startTime`, `endTime`).

### Infrastructure Dashboard
- `GET /api/microgrid-dashboard`: Get infrastructure statistics summary.
