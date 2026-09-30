# M2 Energy Reservation Management

M2 connects prosumer demand to M1 energy slots and owns the reservation lifecycle. It exposes approved reservation information to M3, which consumes it when creating a transaction. Reservation rules live in the ASP.NET API; the Android and staff web clients call the same endpoints.

## Lifecycle

`Pending -> Approved -> Completed` is the successful path. Staff can reject a pending request. A prosumer or staff member can cancel a pending request. An approved reservation can be cancelled only at least 12 hours before its start. Approved reservations become expired after their end time. Rejected, cancelled, completed, and expired reservations are terminal.

## Role access

| Role | M2 permissions and client |
| --- | --- |
| Backoffice / Admin | Web: view/filter the full queue, create on behalf of a prosumer, approve/reject, modify/cancel, and see live network totals. |
| Grid Operator / MicrogridOperator | Web: view and manage reservations for assigned microgrids, create on behalf, approve/reject, modify/cancel, and see assigned-node totals. M2 reads existing M1 `operatorId` and slot `createdBy` assignments. |
| Prosumer | Android: browse live M1 slots, reserve, view current/history, search/filter, modify/cancel with notice, and see a summary after each action. Reservations are owned by the NIC from the signed-in API account. |
| TransactionVerifier | Web: read-only approved reservations on assigned microgrids for the M3 handoff. M2 does not grant this role create, edit, approve, reject, or cancel actions. |

`GET /api/reservations/access` supplies the web client's M2 capability flags; authorization remains enforced by the API. Prosumer reservation cache reads and refreshes are keyed by NIC in Room.

## API routes

All routes require a bearer JWT.

| Method | Route | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/reservations/access` | Any signed-in role | M2 role capability and assigned-node response for role-aware web navigation |
| GET | `/api/reservations` | Prosumer sees own; staff sees all | Filter by status/node and paginate with `page`, `pageSize` |
| GET | `/api/reservations/summary?nic={nic}` | Own summary or staff summary for a NIC | Pending, upcoming approved, completed this month, active reserved kWh |
| GET | `/api/reservations/{id}` | Owner or staff | Reservation detail for M3 and clients |
| POST | `/api/reservations` | Prosumer or staff | Create a pending request; staff supplies `prosumerId` |
| PUT | `/api/reservations/{id}` | Owner or staff | Change `energyAmount` while eligible and at least 12 hours before start |
| PATCH | `/api/reservations/{id}/approve` | Admin, MicrogridOperator | Approve a pending request |
| PATCH | `/api/reservations/{id}/reject` | Admin, MicrogridOperator | Reject a pending request; optional `{ "reason": "..." }` |
| PATCH | `/api/reservations/{id}/cancel` | Owner or staff | Cancel an eligible pending/approved request |
| PATCH | `/api/reservations/{id}/complete` | Admin, MicrogridOperator | Completes the reservation lifecycle in M2; transaction QR verification and transaction completion belong to M3 |
| GET | `/api/reservations/nodes/{nodeId}/has-active` | Admin, MicrogridOperator | M1 integration check before node deactivation |

Create payload:

```json
{
  "energySlotId": "MongoDB energy slot ObjectId",
  "energyAmount": 12.5
}
```

Staff creation also accepts `prosumerId`, which is the prosumer's NIC. NIC is stored on the shared user document with a unique sparse MongoDB index and returned in profile responses. Reservations reference that NIC, while the existing MongoDB ObjectId remains the users collection's internal `_id` to preserve M1 references. Existing accounts need their NIC populated by an Admin before they can make reservations.

The three system roles are `Admin`, `MicrogridOperator`, and `Prosumer`. JWTs carry the role claim used by all endpoints for authorization. `MicrogridOperator` performs M3 transaction verification, energy transfer confirmation, and transaction completion. M2 owns reservation creation, modification, cancellation, approval, and reservation status; M3 owns the separate transaction lifecycle and history.

## Rules and reliability

- New bookings must start in the future and within seven days.
- The prosumer account must exist, have the Prosumer role, and be active.
- Slot capacity is decremented with one conditional MongoDB update (`availableAmount >= requested amount`) to prevent concurrent overbooking.
- Rejecting/cancelling restores capacity; amount edits apply the capacity difference atomically and roll it back if the reservation changes concurrently.
- All changes record a status-history entry with actor, timestamp, and optional rejection reason.
- A hosted worker expires unused approved reservations every three minutes.
- Persisted reservation timestamps and status-history timestamps are UTC. Expiry leaves allocated slot energy consumed because the slot delivery window has elapsed; it does not return expired capacity to a future availability window.
- Android stores the last successful reservation list in its Room/SQLite cache and displays it when offline; online API state remains authoritative.
- Reservation amount edits change slot capacity by the amount difference. Reservation writes use a compare-and-update filter, and any capacity adjustment is rolled back when another update wins.

## Component handoffs

- M1 provides published slots and remaining capacity. Energy availability includes both `Available` and `PartiallyReserved` slots. M1 calls `IReservationService.HasActiveForNodeAsync` before update/deactivation or deletion, so pending and approved bookings block those operations.
- M2 owns every reservation transition and capacity allocation. M3 reads approved reservation details and calls `PATCH /api/reservations/{id}/complete`; it does not write the reservation collection directly.
- M4 supplies the authenticated user, active state, role, and NIC. Prosumer ownership comes from the account resolved from the JWT subject. Public and staff creation paths normalize and enforce NIC uniqueness. Existing accounts without NIC remain intact and receive a clear booking error until Backoffice updates them.

## Demo sequence

1. Register or create an active Prosumer with a unique NIC.
2. Publish an M1 slot on an active microgrid with available energy.
3. Book the slot from Android. A slot starting within seven days creates a Pending reservation and atomically reduces slot availability.
4. Approve it from the staff reservation page. Confirm Android Home and My Reservations show live values.
5. Complete it through M3's M2 completion endpoint before the slot end time.
6. Try an eight-day booking, an over-capacity request, an edit inside 12 hours, an approved cancellation inside 12 hours, and a node deactivation with an active reservation; each is rejected by the API.

## Web and Android

- Staff web page: `Web/SmartMicrogrid.Web/pages/reservations/reservations.html`.
- Android prosumer flow: browse M1 slots, review and submit a reservation summary, then use My Reservations to search/filter, modify, cancel, and view current/history data.
- The reservations page loads Bootstrap 5 before the existing project styles, so its framework-based layout retains the established theme tokens and components.
