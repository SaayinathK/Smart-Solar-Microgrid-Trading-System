# Component 1 — MongoDB Schema & Database Specification

## Database Name
`SmartMicrogridDB`

## Collections & Schemas

### 1. `microgrids` Collection
```json
{
  "_id": { "$oid": "68c123456789abcdef123456" },
  "name": "Colombo Solar Hub",
  "location": "Colombo",
  "description": "Commercial solar array",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "capacity": 500.0,
  "availableCapacity": 350.0,
  "reservedCapacity": 100.0,
  "usedCapacity": 50.0,
  "batteryCapacity": 200.0,
  "currentBatteryLevel": 160.0,
  "batteryPercentage": 80.0,
  "status": "Active",
  "isActive": true,
  "operatorId": "op-colombo-01",
  "createdAt": { "$date": "2026-09-25T00:00:00Z" },
  "updatedAt": { "$date": "2026-09-25T00:00:00Z" }
}
```

### Indexes Created
- `status` (Ascending)
- `isActive` (Ascending)
- `operatorId` (Ascending)
- `location` (Ascending)
- `2dsphere` index on `(longitude, latitude)` for geospatial search.

### 2. `energySlots` Collection
```json
{
  "_id": { "$oid": "68c123456789abcdef654321" },
  "microgridNodeId": { "$oid": "68c123456789abcdef123456" },
  "energyAmount": 100.0,
  "availableAmount": 100.0,
  "startTime": { "$date": "2026-09-25T10:00:00Z" },
  "endTime": { "$date": "2026-09-25T14:00:00Z" },
  "pricePerUnit": 25.0,
  "status": "Available",
  "createdBy": "op-colombo-01",
  "createdAt": { "$date": "2026-09-25T00:00:00Z" },
  "updatedAt": { "$date": "2026-09-25T00:00:00Z" }
}
```

### Indexes Created
- `microgridNodeId` (Ascending)
- `status` (Ascending)
- `(startTime, endTime)` Compound index
