# Prosumer mobile workspace

The prosumer navigation, availability and booking scripts, transaction pages, account screens and API rules were reviewed before changing the Android workspace.

| Existing feature | Upgraded mobile destination |
| --- | --- |
| Personal overview and reservation counts | Home: pending requests, upcoming deliveries, reserved kWh and completed bookings |
| Bookings and transaction QR | Home: next delivery and ready-QR shortcuts; Bookings: transaction/QR action |
| Active microgrid map, Google/Leaflet selection and directions | Home: compact map with node and provider selection |
| Active hubs, location/name search and capacity sorting | Explore → Microgrids, with images, battery meters and grid details |
| Read-only generation/available/reserved/used capacity and storage comparisons | Explore → Capacity / Batteries, with status filters and animated meters |
| Published slot catalogue and lifecycle statuses | Explore → Energy slots, including partially reserved supply and ended windows |
| Available supply, location/minimum energy filters, delivery/price/quantity sorting | Explore → Energy offers, including hub-specific offers |
| Booking creation | Reserve energy: refreshed supply, LKR estimate, quantity validation and explicit submission |
| Pending/approved bookings and closed history | Bookings: Current, Approved, All bookings and History, status/search/delivery-date filters |
| Booking modification, alternate slot and custom delivery time | Modify: same-hub slot options, amount and local time picker with UTC submission |
| Cancellation and returned capacity | Cancel booking: confirmation and server-validated capacity release |
| Booking details and status history | Booking details; cached copies explicitly identify unavailable cost/history |
| Offline reservation viewing | Account-scoped SQLite cache; changes are unavailable while viewing the offline copy |
| All transactions, progress and completed history | Transfers: All transfers, In progress and History, status/search/created-date filters |
| QR display, image download/share and transaction-ID copy | Show QR and existing QR screen; booking delivery times replace transaction creation times |
| Personal profile, contact edits, password and account deactivation | Profile, including read-only NIC, existing account forms and explicit deactivation confirmation |
| Theme selection and sign out | Profile: System, Light, Dark and sign out |

Native photos, compact cards, status badges and short contextual instructions are shared with the operator presentation. Home adds animated booking-status and energy charts. Lists and meters animate as they enter view. All new motion follows Android's animation settings and ends when views detach.

New bookings must start in the future and within seven days. Amounts are checked against freshly fetched remaining supply; changed prices require another review. Booking edits and cancellations require at least twelve hours before delivery. The API remains authoritative for identity, authorization, availability and concurrent changes. The prosumer workspace never exposes operator approval, transaction creation, QR generation, verification or completion controls.

Reservations are paged until every record is loaded and additionally checked against the signed-in user's NIC/ID. The same ownership check applies to transfer results and cached bookings. Legacy availability and reservation entry points route prosumers into the upgraded workspace.

Prosumer web sign-in now routes directly to Android app guidance without saving a browser session. The shared web route guard also clears legacy prosumer sessions and blocks protected-page navigation. Grid Operator and Administrator web access remains available; Android authentication and backend role permissions are unchanged. Web account registration remains available. Run the web session regression checks with `node --test Web/SmartMicrogrid.Web/tests/auth-guard.test.cjs`.

Verification includes pure booking-window, quantity, ownership and history-boundary tests; native layout/drawing checks in both themes with larger text; and an opt-in demo prosumer navigation check. The demo check reads real APIs, captures screen previews, and restores the original phone session and reservation cache. It never submits a booking, alters a profile, cancels a reservation or advances a transfer.
