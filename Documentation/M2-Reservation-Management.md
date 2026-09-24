# M2 Energy Reservation Management

M2 connects prosumer demand to M1 energy slots and exposes approved reservation details to M3. Reservation rules live in the ASP.NET API; the Android and staff web clients call the same endpoints.

## Lifecycle

`Pending -> Approved -> Completed` is the successful path. Staff can reject a pending request. A prosumer or staff member can cancel a pending request. An approved reservation can be cancelled only at least 12 hours before its start. Approved reservations become expired after their end time. Rejected, cancelled, completed, and expired reservations are terminal.

## API routes

All routes require a bearer JWT.

| Method | Route | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/reservations` | Prosumer sees own; staff sees all | Filter by status/node and paginate with `page`, `pageSize` |
| GET | `/api/reservations/summary?nic={nic}` | Own summary or staff summary for a NIC | Pending, upcoming approved, completed this month, active reserved kWh |
| GET | `/api/reservations/{id}` | Owner or staff | Reservation detail for M3 and clients |
| POST | `/api/reservations` | Prosumer or staff | Create a pending request; staff supplies `prosumerId` |
| PUT | `/api/reservations/{id}` | Owner or staff | Change `energyAmount` while eligible and at least 12 hours before start |
| PATCH | `/api/reservations/{id}/approve` | Admin, MicrogridOperator | Approve a pending request |
| PATCH | `/api/reservations/{id}/reject` | Admin, MicrogridOperator | Reject a pending request; optional `{ "reason": "..." }` |
| PATCH | `/api/reservations/{id}/cancel` | Owner or staff | Cancel an eligible pending/approved request |
| PATCH | `/api/reservations/{id}/complete` | Admin, MicrogridOperator, TransactionVerifier | M3 marks delivery complete |
| GET | `/api/reservations/nodes/{nodeId}/has-active` | Admin, MicrogridOperator | M1 integration check before node deactivation |

Create payload:

```json
{
  "energySlotId": "MongoDB energy slot ObjectId",
  "energyAmount": 12.5
}
```

Staff creation also accepts `prosumerId`, which is the prosumer's NIC. NIC is stored on the shared user document with a unique sparse MongoDB index and returned in profile responses. Reservations reference that NIC, while the existing MongoDB ObjectId remains the users collection's internal `_id` to preserve M1 references. Existing accounts need their NIC populated by Backoffice before they can make reservations.

Public assignment-facing roles are `Backoffice`, `GridOperator`, and `Prosumer`. Existing legacy stored role names remain readable and JWTs carry both legacy and assignment-facing role claims so M1 endpoints keep working during migration. New staff account forms use the assignment role names.

## Rules and reliability

- New bookings must start in the future and within seven days.
- The prosumer account must exist, have the Prosumer role, and be active.
- Slot capacity is decremented with one conditional MongoDB update (`availableAmount >= requested amount`) to prevent concurrent overbooking.
- Rejecting/cancelling restores capacity; amount edits apply the capacity difference atomically and roll it back if the reservation changes concurrently.
- All changes record a status-history entry with actor, timestamp, and optional rejection reason.
- A hosted worker expires unused approved reservations every three minutes.
- Android stores the last successful reservation list in its Room/SQLite cache and displays it when offline; online API state remains authoritative.

## Web and Android

- Staff web page: `Web/SmartMicrogrid.Web/pages/reservations/reservations.html`.
- Android prosumer flow: browse M1 slots, review and submit a reservation summary, then use My Reservations to search/filter, modify, cancel, and view current/history data.
- The reservations page loads Bootstrap 5 before the existing project styles, so its framework-based layout retains the established theme tokens and components.
