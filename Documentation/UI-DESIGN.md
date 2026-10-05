# Interface patterns

The web and Android apps retain their existing royal-blue palettes and light/dark themes. Solar and battery images come from the existing bundled assets; no new image service is required.

## Web

Load `css/interface.css` after the core and page-specific styles. It supplies shared spacing, surfaces, typography, buttons, fields, responsive image headers, and reduced-motion behavior. Keep page-specific workflow layouts in their own stylesheets.

Authenticated pages use `renderAppLayout` in `js/common/sidebar.js`. Navigation is grouped by role, searchable, and marks the current page. The small-screen drawer supports a backdrop, close button, Escape, focus containment, and keyboard-inaccessible content while closed. Table containers scroll horizontally rather than widening the page.

`dashboard.html` is the shared Overview, combining workspace shortcuts with infrastructure metrics, readiness, charts, alerts, and one microgrid map. `js/overview.js` loads and refreshes its infrastructure data. The former `pages/M1/dashboard.html` URL redirects to Overview; operator transaction operations remain a separate workflow.

Use `.page-header` for the title and primary action, `.hero-image-banner` with `.hero-image-overlay` for contextual imagery, and `.card` for related controls or information. Image header content remains in normal flow so longer text can wrap. Decorative motion is disabled by the user's reduced-motion preference.

## Android

`res/values/interface.xml` defines shared dimensions and text, input, toolbar, and motion styles. Both themes reference the same component patterns. Retain theme attributes for foregrounds and surfaces so screens adapt to dark mode.

All three role activities use the shared bottom-navigation style, short labels, consistent outline icons, and an active indicator. The navigation bar sits outside the content container. Configure tab reselection handling after the initial destination has loaded. `NavigationMotion` respects disabled system animations and avoids reloading a screen when its selected tab is tapped again.

Transaction screens use flexible-height image headers like energy browsing and reservations. Admin tabs use the same eyebrow, title, caption, and content spacing. Preserve view IDs when changing layouts because fragments use view binding and ID lookups.

## Validation

- Build Android with `gradlew.bat :app:assembleDebug`.
- Run existing web regressions with `node --test Web/SmartMicrogrid.Web/tests/location-picker.test.cjs` from the repository root.
- Check authenticated pages at desktop and narrow widths, light/dark themes, keyboard navigation, sidebar search, loading/error states, and reduced motion.
- Check Android on a device/emulator with increased font size, both themes, all role tabs, keyboard-visible forms, and system animations disabled. A successful build does not replace these device checks.
