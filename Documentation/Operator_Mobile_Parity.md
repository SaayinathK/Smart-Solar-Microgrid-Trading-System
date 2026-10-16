# Grid Operator mobile workspace

The web operator navigation and role checks were reviewed in `js/common/sidebar.js`, `js/overview.js`, `js/common/energy-resources.js`, `js/common/energy-slots.js`, `js/common/microgrid-registration.js`, `js/reservations/reservations.js`, and the transaction scripts. Permissions and payloads were checked against the API controllers and DTOs.

| Web operator feature | Android destination |
| --- | --- |
| Welcome, four shortcuts, operational pulse, fleet readiness and four infrastructure totals | Home dashboard in the same section order, with mobile card pairs |
| Capacity headroom, battery resilience and market-ready energy | Home insight cards with animated meters |
| Assigned active microgrid locations, Google/Leaflet selection and external directions | Home map with node selection and both map providers |
| Operational status doughnut and capacity distribution bar chart | Home native animated charts with the same series, colors and figures |
| Attention queue and three image workspace modules | Home alerts and Generation / Marketplace / Energy Transfers cards |
| Register/edit a microgrid, operating status, address search, coordinates and map selection | Grids → Microgrids → Register / Edit |
| Microgrid details and capacity allocation comparison | Grids → Microgrids → Details; Grids → Capacity |
| Battery reserves, headroom, charge comparison and updating current charge | Grids → Batteries |
| Publish future energy slots with quantity, local delivery dates/times and LKR rate | Grids → Energy slots → Publish |
| List/filter slots, identify ended windows and cancel live available slots | Grids → Energy slots |
| Browse available supply, location search, minimum kWh and soonest/price/energy sorting; book for an active prosumer by NIC | Grids → Availability; Bookings → Book for prosumer |
| Booking search by NIC/name/reference/hub, delivery date, current/history states and summary | Bookings |
| Approve/reject pending bookings, optional rejection reason | Bookings → Pending |
| Booking details, status history, transaction creation and transaction QR | Booking card actions |
| Needs attention, all transactions, closed history, search, status and created-date filters | Transfers |
| Pending → Generate QR | Transfer card → Generate QR; existing QR page supports download/share |
| Scan or paste a server-issued QR, match transaction/code and explicitly verify | Home scan button, central Scan QR navigation action, or transfer card |
| Verified / transfer-in-progress → explicit physical-delivery confirmation | Transfer card → Confirm transfer |
| Profile, password, appearance and sign out | Home → My profile & appearance |

Operator navigation is **Home / Grids / Scan QR / Bookings / Transfers**. Scanning preserves the selected tab and opens the scanned transaction after the API checks access. It never verifies or completes a transfer automatically.

The workspace requires `MicrogridOperator`. Assigned-grid lists use the authenticated server scope and additionally match `operatorId` against the signed-in account. Reservations load every page of that operator's server-scoped records. The operator UI does not expose admin account/system management or prosumer-only booking modification/cancellation. Mutations use the existing authenticated APIs and display server validation errors. No new backend permissions were added.

Dashboard calculations mirror `js/overview.js`: only assigned nodes and their slots contribute; readiness weights headroom 40%, average battery percentage 35% and active-node percentage 25%. The battery calculation uses the web's weighted-storage fallback when its reported average is zero. Dashboard slot inventory counts `Available` and `PartiallyReserved`, including records whose end date has passed, exactly as the web dashboard does. Booking independently requires remaining energy and an unexpired window. Availability uses the same API as the web, with an additional local assignment check.

Presentation uses native Material controls, 48 dp minimum action targets, the compact shared page header, light/dark/system themes, explicit empty/error/retry states and pull-to-refresh. Solar, battery and energy images appear in the dashboard, lists, forms and transfer workflow. Charts animate when scrolled into view; figures count up and cards enter with a short fade/slide. Animations respect Android's disabled-animation settings, stop when views detach and have no repeating effects. Chart values are available as text and accessibility descriptions. Capacity/battery detail views retain per-grid figures and animated native meters. The map uses bundled Leaflet with OpenStreetMap attribution and HTTPS tiles; manual coordinates remain usable when map/geocoding services are unavailable.

Validation covers dashboard aggregation and assignment scope, web inventory semantics, readiness/fallback calculations, alert thresholds, action-queue/history boundaries, next actions, role gating, NIC formats, expired and partially reserved slots, capacity/price/date validation, timezone parsing and QR payload validation. Instrumentation checks native measurement/drawing at 320 dp with larger text in both themes and transfer workflow layout inflation. End-to-end camera scanning and physical transfer completion require an unlocked device and a real operator workflow; do not mark actual customer bookings complete merely to test the UI.
