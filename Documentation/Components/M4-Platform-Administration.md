# Component 4 — Platform Administration & System Operations

## Overview
Component 4 provides the backoffice/operations layer for the **Smart Microgrid Energy Management & Trading Platform**. It owns the administrative dashboard, user and role administration, the platform-wide audit trail, system health monitoring, system configuration, and reporting.

Component 4 is a **read-only consumer** of Components 1–3. It aggregates their data through existing repository interfaces and monitor adapters, and it never mutates M1/M2/M3 business entities or bypasses their validation.

---

## 1. System Responsibilities & Boundaries

Component 4 exclusively owns:
1. **Administrative Dashboard** (`/api/admin/dashboard`): unified platform metrics combining user counts, role distribution, and M1/M2/M3 operational telemetry.
2. **User Administration**: account lifecycle (Active, Inactive, Suspended, Pending) and role assignment.
3. **Role Administration**: read-only role distribution reporting derived from the role enum.
4. **System Activity / Audit Trail** (`/api/admin/activity`): append-only record of authentication, configuration, and user-management events.
5. **System Health Monitoring** (`/api/admin/system/health`): database, authentication, API, and server status.
6. **System Configuration** (`/api/admin/system/configuration`): platform identity, session policy, and platform-wide feature toggles.
7. **Reporting** (`/api/admin/reports/*`): user, role, activity, and platform reports with optional date ranges and CSV export in the web client.

### Out-of-Scope (Delegated Components)
- **Component 1**: Microgrid nodes, energy capacity, battery storage, energy slots, energy availability.
- **Component 2**: Energy marketplace, search, reservations.
- **Component 3**: Energy transactions, QR generation/scanning, verification.

M4 reads M1/M2/M3 data for aggregation only. All writes to those domains remain in their owning component.

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
              |                                  AdminMainActivity
              |                                  (M4 administration)
              +-------------------+-------------------+
                                  |
                           HTTP / JSON / REST (JWT Auth)
                                  |
                         IIS / ASP.NET Core API
                                  |
                         C# Web API Backend
                                  |
                  +---------------+---------------+
                  |               |               |
          AdminDashboard   SystemActivity   SystemConfiguration
                  |               |               |
                  +-------+-------+               |
                          |                       |
             M1/M2/M3 monitor adapters            |
                          |                       |
                              MongoDB
```

Android local storage:
```text
AdminMainActivity (Kotlin)
    ├── AdminDashboardCache   ── Room (offline cache for dashboard)
    ├── AdminActivityCache    ── Room (offline cache for activity page 1)
    └── SystemConfigCache     ── Room (offline cache for configuration)
```

The web client has no local persistence; it reads live from the API.

---

## 3. Roles & Access Control

| Role | M4 access | Notes |
|---|---|---|
| `Admin` (System Administrator) | Full | Only role permitted on every `/api/admin/*` route |
| `MicrogridOperator` | Denied | Receives `403 Forbidden` |
| `Prosumer` | Denied | Receives `403 Forbidden` |
| Anonymous | Denied | Receives `401 Unauthorized` |

Routes are decorated with the Admin authorization policy. There is deliberately **no MongoDB roles collection** — roles are derived from the `Role` enum stored on each user document, so role administration is a read-only projection plus role reassignment on a user.

---

## 4. Account Lifecycle

M4 introduces `AccountStatus` as the authoritative account state, with `IsActive` retained as a mirrored boolean for backwards compatibility with existing consumers.

| `AccountStatus` | `IsActive` | Meaning | Allowed transitions |
|---|---|---|---|
| `Active` | `true` | Account may authenticate and transact | → Inactive, Suspended, Pending |
| `Inactive` | `false` | Account stood down by an administrator | → Active, Suspended, Pending |
| `Suspended` | `false` | Administratively blocked | → Active, Inactive, Pending |
| `Pending` | `false` | Awaiting approval/verification | → Active, Inactive, Suspended |

**Legacy records.** Documents written before this component existed have no `accountStatus` field. The service resolves their effective status from `IsActive` on read, so a legacy `isActive: false` user is reported as `Inactive` rather than appearing as a default `Active`.

**Transitions are centralised.** All four status values funnel through a single transition core with a shared `ResolveStatusAction` helper that maps a `(previous, next)` pair to an audit action such as `UserActivated`, `UserDeactivated`, `UserSuspended`, or `UserReactivated`. This keeps the legacy boolean `PATCH /api/users/{id}/status` and the new `PATCH /api/users/{id}/account-status` endpoint behaviourally identical.

### Last-active-admin protection
The platform refuses to remove the last route into the backoffice. Three operations would otherwise leave nobody able to restore administration, since only an `Admin` can change roles or assign them:

| Attempted operation on the final active `Admin` | Result |
|---|---|
| Suspend or deactivate via `PATCH /users/{id}/account-status` | Rejected |
| Demote via `PATCH /users/{id}/role` | Rejected |
| Delete via `DELETE /users/{id}` | Rejected |

Each returns a message of the form `The last active Backoffice officer account cannot be ...`. The guard asks the repository whether any *other* active `Admin` exists and, if not, rejects the change. Acting on one administrator while another remains active is permitted, so redundancy can be rotated safely without locking the backoffice out of its own system.

---

## 5. API Reference

Base URL: `{host}/api`. All `/api/admin/*` routes require an `Admin` JWT bearer token.

### 5.1 Administrative Dashboard
| Method | Route | Description |
|---|---|---|
| GET | `/admin/dashboard` | Unified platform snapshot |

Returns `platformName`, `generatedAt`, `maintenanceMode`, `users` (total/active/inactive/suspended/pending), `roleDistribution`, and a `platform` block with M1/M2/M3 telemetry.

**Degradation behaviour.** Each upstream source (M1 telemetry, M2 reservations, M3 transactions, health, activity) is resolved through an independent safe wrapper. A failing dependency degrades its own section to an `Unavailable` component state and increments a warning list rather than failing the whole request. A partially degraded dashboard is more useful than a 500 to an administrator, and the degradation is itself visible in the response.

### 5.2 System Health
| Method | Route | Description |
|---|---|---|
| GET | `/admin/system/health` | Database, authentication, API and server status |

Includes `databaseLatencyMs`, `processMemoryBytes`, `uptime`, `version`, `environment`, and `jwtIssuer`.

### 5.3 System Configuration
| Method | Route | Description |
|---|---|---|
| GET | `/admin/system/configuration` | Read platform configuration |
| PUT | `/admin/system/configuration` | Update platform identity and session policy |
| PATCH | `/admin/system/configuration/maintenance` | Toggle maintenance mode (with optional banner) |
| PATCH | `/admin/system/configuration/registration` | Toggle open registration |

**Null semantics.** `PUT` treats a missing `maintenanceMessage` as *leave unchanged* and an explicit empty string as *clear the banner*. This matters because the web configuration form submits the details form and the toggles independently; without the distinction, saving the details form silently wipes a maintenance banner that an operator had just set.

### 5.4 System Activity (Audit Trail)
| Method | Route | Description |
|---|---|---|
| GET | `/admin/activity` | Paged activity with filters |
| GET | `/admin/activity/{id}` | Single activity record |

Query parameters: `page`, `pageSize` (1–200), `userId`, `module`, `action`, `status`, `from`, `to`.

**Actor attribution.** Audit records resolve the acting user from the current `ClaimsPrincipal`, not from the affected entity. An administrator suspending an account is recorded as the actor, with the target account identified by `entityId` and named in the `description`. The audit repository is wrapped so that a persistence failure logs an error but never propagates — an audit problem must not roll back or break the business operation that was performed.

### 5.5 Reports
| Method | Route | Description |
|---|---|---|
| GET | `/admin/reports/users` | User report; supports `role`, `status`, `from`, `to` |
| GET | `/admin/reports/roles` | Role distribution report |
| GET | `/admin/reports/activity` | Activity report grouped by action and module |
| GET | `/admin/reports/platform` | Combined platform report |

### 5.6 User Management (M4-owned additions)
| Method | Route | Description |
|---|---|---|
| PATCH | `/users/{id}/account-status` | Set `AccountStatus` (Suspended/Pending/etc.) |
| PATCH | `/users/{id}/status` | Legacy boolean `isActive` toggle (retained) |
| PATCH | `/users/{id}/role` | Change role |
| GET | `/users?accountStatus=...` | Filter by lifecycle state |

---

## 6. Data Model

### SystemActivity
| Field | Type | Notes |
|---|---|---|
| `Id` | string | Mongo ObjectId |
| `UserId` | string? | **Acting** user, from the JWT principal |
| `UserName` | string | Display name of the actor |
| `Role` | string | Actor role |
| `Action` | string | e.g. `USER_SUSPENDED`, `LOGIN_SUCCESS` |
| `Module` | string | e.g. `User Management`, `System Configuration` |
| `Description` | string | Human-readable summary naming the target |
| `EntityType` | string? | Affected entity type, e.g. `User` |
| `EntityId` | string? | Affected entity id |
| `IpAddress` | string? | First forwarded address when proxied |
| `Status` | string | `Success` / `Failure` |
| `Timestamp` | DateTime | UTC |

Indexed on `Timestamp` (descending reads), `UserId`, `Module`, and `Action`.

### SystemConfiguration
Singleton document: `PlatformName`, `PlatformDescription`, `MaintenanceMode`, `MaintenanceMessage`, `AllowRegistration`, `SessionTimeoutMinutes`, `MaxLoginAttempts`, `DefaultPageSize`, `UpdatedAt`, `UpdatedBy`.

### AccountStatus
Enum: `Active`, `Inactive`, `Suspended`, `Pending`.

---

## 7. Web Client

Six pages under `Web/SmartMicrogrid.Web/pages/M4/`, linked from the Administration section of the shared sidebar:

| Page | File | Controller |
|---|---|---|
| Admin Dashboard | `dashboard.html` | `js/m4/admin-dashboard.js` |
| Role Management | `roles.html` | `js/m4/roles.js` |
| System Activity | `activity.html` | `js/m4/activity.js` |
| System Health | `system.html` | `js/m4/system.js` |
| Configuration | `configuration.html` | `js/m4/configuration.js` |
| Reports | `reports.html` | `js/m4/reports.js` |

Shared helpers: `js/api/admin-api.js` (endpoint wrapper) and `js/common/admin-ui.js` (formatting, tables, toasts, empty states). `js/users/users.js` gained the full Active/Inactive/Suspended/Pending lifecycle controls and status badges in `css/tables.css`.

The reports page generates on demand, supports date ranges and role/status filters, and exports the currently rendered tables to CSV so the export always matches what the operator is looking at.

Login routing is unchanged: an administrator signs in through the normal login page and reaches M4 via the sidebar. Admins are the only role shown the Administration section.

---

## 8. Android Client

`AdminMainActivity` hosts four M4 fragments in a `BottomNavigationView`: Overview, Users, Health, and Settings. An administrator signing in on Android is routed here from `LoginActivity`; Prosumer and MicrogridOperator keep their existing entry points.

- **Models**: `models/AdminModels.kt` (dashboard, health, configuration, activity, report DTOs) and `models/AuthModels.kt` (user account-status fields).
- **Local cache**: `data/local/AdminCacheEntities.kt`, `RoomDaos.kt`, and `AppDatabase.kt` (version 3, `MIGRATION_2_3`). The dashboard, first activity page, and configuration are cached so the last known platform state is viewable offline.
- **Remote**: `data/remote/ApiService.kt` M4 endpoints plus the account-status endpoint.
- **Repository**: `data/repository/AdminRepository.kt` performs network-first reads with Room fallback.
- **ViewModel/UI**: `M4/AdminViewModel.kt`, `M4/AdminUserAdapter.kt`, and the four fragments with their layouts, menu, colours, and strings.

> Note: the Android module has not been compiled in this branch, so the Kotlin and resource changes are unverified by the build. Run `./gradlew :app:assembleDebug` before relying on them.

---

## 9. Testing

`dotnet test Backend/SmartMicrogrid.API.Tests` — **153 passing, 0 failing**.

M4 coverage:
- `DashboardServiceTests.cs` — aggregation shape, legacy account-status resolution, and per-source degradation when an upstream dependency throws.
- `UserAccountStatusTests.cs` — lifecycle transitions, `IsActive` mirroring, audit action mapping, the last-active-admin guard across status, role and delete, and the actor-is-not-the-target attribution rule.
- `M4ServiceTests.cs` — configuration null semantics, maintenance/registration toggles, audit actor resolution from the principal, forwarded-address handling, anonymous labelling, and repository-failure isolation.

Verified at runtime against a live API and MongoDB instance: all eight read endpoints return 200, RBAC returns 403 for non-admins (including the account-status write) and 401 for anonymous callers, the full `Active → Suspended → Inactive → Active → Pending → Active` cycle behaves correctly, a `PUT` that omits `maintenanceMessage` preserves the banner while an explicit empty string clears it, and every activity filter (module, action, status, date range) narrows results as expected. The Postman collection was executed end to end twice to confirm it is idempotent and leaves no account suspended, deleted, or demoted.

---

## 10. Known Limitations & Follow-ups

1. **Deactivation does not revoke existing tokens.** A JWT issued before an account is suspended remains valid until it expires. Revocation needs a token version or denylist checked by the JWT middleware.
2. **Audit `StatusChangedBy` is hardcoded to `Admin`** rather than resolved from the principal, so the account-status field does not distinguish which administrator made the change. The audit trail itself does record the real actor.
3. **`UpdateAccountStatusDto.AccountStatus` is `[Required]` on a non-nullable enum**, so the attribute cannot reject an omitted value; an omitted field binds to `Active`. Validate the raw JSON if strict rejection is wanted.
4. **Android M4 is uncompiled** in this branch (see section 8) and has no activity/report screens or Inactive/Pending actions yet; the API supports all of them.
5. **Android stale-cache signalling is incomplete** — `AdminViewModel.isStale` is never set true when repository data is served from Room, so the offline notice does not appear.
6. **Security debt outside M4 scope**: committed/fallback JWT secrets, exception detail returned to clients, and no rate limiting on authentication.

---

## 11. Related Documentation
- `Documentation/Components/Component-1-Microgrid-Management.md`
- `Documentation/M2-Reservation-Management.md`
- `Documentation/Architecture.md`
- `Documentation/Database/README.md`
- `Postman/SmartMicrogrid_Component4_PlatformAdministration.postman_collection.json`
- `Postman/SmartMicrogrid_UserManagement.postman_collection.json`
