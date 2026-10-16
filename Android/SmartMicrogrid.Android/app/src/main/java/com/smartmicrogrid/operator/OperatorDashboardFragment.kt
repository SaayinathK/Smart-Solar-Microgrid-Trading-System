package com.smartmicrogrid.operator

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.core.widget.NestedScrollView
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.smartmicrogrid.M3.MicrogridOperatorMainActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import java.text.DateFormat
import java.util.Date
import kotlin.math.roundToInt

/** The web operator Overview, arranged as a single native mobile scroll surface. */
class OperatorDashboardFragment : Fragment() {
    private val repository = OperatorRepository()
    private lateinit var refresh: SwipeRefreshLayout
    private lateinit var content: LinearLayout
    private lateinit var readings: LinearLayout
    private lateinit var updated: TextView
    private lateinit var error: TextView
    private lateinit var refreshButton: com.google.android.material.button.MaterialButton
    private var request: Job? = null
    private var fleetMap: OperatorFleetMap? = null
    private var selectedNodeId: String? = null
    private var provider = "google"
    private var scrollPosition = 0
    private lateinit var scroll: NestedScrollView
    private val host get() = requireActivity() as MicrogridOperatorMainActivity
    private fun number(value: Double) = OperatorOverview.format(value)

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        selectedNodeId = savedInstanceState?.getString("map_node")
        provider = savedInstanceState?.getString("map_provider") ?: "google"
        scrollPosition = savedInstanceState?.getInt("scroll") ?: 0
    }
    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, state: Bundle?): View {
        val ctx = requireContext()
        content = ctx.column().apply { setPadding(ctx.dp(14), ctx.dp(8), ctx.dp(14), ctx.dp(16)) }
        scroll = NestedScrollView(ctx).apply { isFillViewport = true; isVerticalScrollBarEnabled = false; addView(content) }
        refresh = SwipeRefreshLayout(ctx).apply {
            setColorSchemeResources(R.color.primary); addView(scroll)
            setOnChildScrollUpCallback { _, _ -> scroll.canScrollVertically(-1) }
            setOnRefreshListener { load() }
        }
        return refresh
    }
    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val ctx = requireContext()
        val name = SessionManager.getUser()?.let { "${it.firstName} ${it.lastName}".trim() }.orEmpty().ifBlank { "Operator" }
        content.addView(OperatorVisuals.photo(ctx, "Welcome, $name!", "Your network, bookings and energy transfers.", R.drawable.bg_energy_home, 88, compact = true), gap())
        val quick = listOf(
            Triple("Grids", R.drawable.ic_nav_grid) { host.openGrids(0) },
            Triple("Energy", R.drawable.ic_solar_panel) { host.openGrids(4) },
            Triple("Bookings", R.drawable.ic_nav_reservations) { host.openReservations() },
            Triple("Transfers", R.drawable.ic_nav_transactions) { host.openTransfers() }
        )
        val shortcuts = LinearLayout(ctx).apply { isBaselineAligned = false }
        quick.forEachIndexed { index, item ->
            shortcuts.addView(ctx.action(item.first, click = item.third).apply {
                setIconResource(item.second); iconSize = ctx.dp(18); iconPadding = ctx.dp(4)
                iconTint = android.content.res.ColorStateList.valueOf(ctx.getColor(R.color.primary))
                iconGravity = com.google.android.material.button.MaterialButton.ICON_GRAVITY_TEXT_TOP
                textSize = 11.5f; minimumWidth = 0; minWidth = 0; minHeight = ctx.dp(64)
                letterSpacing = 0f; setSingleLine(true); ellipsize = android.text.TextUtils.TruncateAt.END
                setPaddingRelative(ctx.dp(2), ctx.dp(8), ctx.dp(2), ctx.dp(8))
                insetTop = 0; insetBottom = 0
            }, LinearLayout.LayoutParams(0, -1, 1f).apply { if (index > 0) marginStart = ctx.dp(6) })
        }
        content.addView(shortcuts, gap())
        val (pulse, pulseBody) = OperatorVisuals.panel(ctx, 10)
        val pulseHeader = LinearLayout(ctx).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        val pulseTitle = ctx.column()
        pulseTitle.addView(ctx.label("Operational pulse", 15f, true).apply { androidx.core.view.ViewCompat.setAccessibilityHeading(this, true) })
        updated = ctx.label("Loading infrastructure data…", 12f).apply { accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE }
        pulseTitle.addView(updated)
        pulseHeader.addView(pulseTitle, LinearLayout.LayoutParams(0, -2, 1f))
        refreshButton = iconButton(R.drawable.ic_operator_refresh, "Refresh dashboard") { load() }
        pulseHeader.addView(refreshButton, LinearLayout.LayoutParams(ctx.dp(48), ctx.dp(48)))
        pulseBody.addView(pulseHeader)
        val scan = LinearLayout(ctx).apply { isBaselineAligned = false; setPadding(0, ctx.dp(6), 0, 0) }
        scan.addView(ctx.action("Scan QR", true) { host.startQrScan() }.apply { setIconResource(R.drawable.ic_nav_scan); setPaddingRelative(ctx.dp(8), 0, ctx.dp(8), 0); letterSpacing = 0f }, LinearLayout.LayoutParams(0, -1, 1f))
        scan.addView(ctx.action("Energy slots") { host.openGrids(3) }.apply { setPaddingRelative(ctx.dp(8), 0, ctx.dp(8), 0); letterSpacing = 0f }, LinearLayout.LayoutParams(0, -1, 1f).apply { marginStart = ctx.dp(8) })
        val more = iconButton(R.drawable.ic_operator_more, "More operator actions") {}.apply {
            setOnClickListener { anchor ->
                PopupMenu(ctx, anchor).apply {
                    menu.add("Paste QR").setOnMenuItemClickListener { host.pasteQr(); true }
                    menu.add("Profile & appearance").setOnMenuItemClickListener { host.openProfile(); true }
                }.show()
            }
        }
        scan.addView(more, LinearLayout.LayoutParams(ctx.dp(48), -1).apply { marginStart = ctx.dp(6) })
        pulseBody.addView(scan)
        content.addView(pulse, gap())
        error = ctx.label("", 13f).apply { visibility = View.GONE; setTextColor(ctx.getColor(R.color.m3_error)); accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE }
        content.addView(error, gap())
        readings = ctx.column(); content.addView(readings)
        content.addView(ctx.action("My profile & appearance") { host.openProfile() })
    }
    override fun onResume() { super.onResume(); load() }
    private fun load() {
        if (!isAdded || view == null) return
        request?.cancel()
        request = viewLifecycleOwner.lifecycleScope.launch {
            refresh.isRefreshing = true; refreshButton.isEnabled = false; updated.text = "Refreshing infrastructure data…"; error.visibility = View.GONE
            try {
                repository.ensureOperator()
                val grids = repository.grids()
                val slots = repository.slots(grids)
                val overview = OperatorOverview.from(grids, slots, SessionManager.getUser()?.id.orEmpty())
                render(overview)
                updated.text = "Updated ${DateFormat.getTimeInstance(DateFormat.SHORT).format(Date())}"
            } catch (cancelled: CancellationException) { throw cancelled }
            catch (failure: Exception) {
                updated.text = "Could not refresh infrastructure data. Please try again."
                error.text = (failure.message ?: "Unable to connect.") + if (readings.childCount > 0) " Showing the last successful readings." else " Tap Refresh data to retry."
                error.visibility = View.VISIBLE
            } finally { refresh.isRefreshing = false; refreshButton.isEnabled = true }
        }
    }
    private fun render(data: OperatorOverview) {
        val ctx = requireContext()
        val previousScroll = if (readings.childCount > 0) scroll.scrollY else scrollPosition
        fleetMap?.let { selectedNodeId = it.selectedId; provider = it.provider; it.release() }
        readings.removeAllViews()
        heading(readings, "Fleet overview", "Readiness ${data.readiness?.let { "$it%" } ?: "—"}")
        readings.getChildAt(0).layoutParams = gap()
        val stats = listOf(
            OperatorVisuals.stat(ctx, "Generation capacity", data.capacity, " kWh", "${number(data.available)} kWh available", OperatorVisuals.green, R.drawable.ic_solar_panel),
            OperatorVisuals.stat(ctx, "Battery storage", data.batteryStored, " kWh", "Of ${number(data.batteryCapacity)} kWh · ${number(data.batteryPercentage)}%", OperatorVisuals.cyan, R.drawable.ic_nav_health),
            OperatorVisuals.stat(ctx, "Microgrids", data.grids.size.toDouble(), "", "${data.active} active · ${data.inactive} inactive", OperatorVisuals.amber, R.drawable.ic_nav_grid),
            OperatorVisuals.stat(ctx, "Active slots", data.activeSlots.toDouble(), "", "${number(data.marketEnergy)} kWh available", OperatorVisuals.purple, R.drawable.ic_nav_pending)
        )
        stats.chunked(2).forEach { pair ->
            val row = LinearLayout(ctx).apply { isBaselineAligned = false }
            pair.forEachIndexed { index, card -> row.addView(card, LinearLayout.LayoutParams(0, -1, 1f).apply { if (index > 0) marginStart = ctx.dp(8) }) }
            readings.addView(row, gap())
        }
        val (insights, insightsBody) = OperatorVisuals.panel(ctx, 12)
        insight(insightsBody, "Capacity headroom", "${data.headroom.roundToInt()}%", data.headroom.roundToInt(), OperatorVisuals.green,
            if (data.grids.isEmpty()) "No assigned microgrids."
            else if (data.headroom < 20) "Low headroom. Review allocations."
            else "${number(data.available)} kWh ready to allocate.")
        divider(insightsBody)
        insight(insightsBody, "Battery resilience", "${data.batteryPercentage.roundToInt()}%", data.batteryPercentage.roundToInt(), OperatorVisuals.cyan,
            if (data.grids.isEmpty()) "No assigned battery storage."
            else if (data.batteryPercentage < 30) "Low storage. Schedule charging."
            else "Storage within the operating target.")
        divider(insightsBody)
        heading(insightsBody, "Market-ready energy", "${number(data.marketEnergy)} kWh")
        val supply = LinearLayout(ctx).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        supply.addView(ctx.label("${data.activeSlots} active slots", 12f), LinearLayout.LayoutParams(0, -2, 1f))
        supply.addView(ctx.action("View offers") { host.openGrids(4) }.apply { textSize = 12f })
        insightsBody.addView(supply)
        readings.addView(insights, gap())
        val (mapCard, mapBody) = OperatorVisuals.panel(ctx, 12)
        fleetMap = OperatorFleetMap(ctx, data.grids, selectedNodeId, provider).also { mapBody.addView(it) }
        readings.addView(mapCard, gap())
        val statusSeries = listOf(OperatorSeries("Active", data.active.toDouble(), OperatorVisuals.green), OperatorSeries("Inactive", data.inactive.toDouble(), OperatorVisuals.amber), OperatorSeries("Maintenance", data.maintenance.toDouble(), OperatorVisuals.blue), OperatorSeries("Offline", data.offline.toDouble(), OperatorVisuals.red))
        chart("Operational status", "Fleet condition by microgrid node", true, statusSeries, data.grids.isNotEmpty())
        chart("Capacity distribution", "Available, reserved, used and total energy", false,
            listOf(OperatorSeries("Available", data.available, OperatorVisuals.green), OperatorSeries("Reserved", data.reserved, OperatorVisuals.amber), OperatorSeries("Used", data.used, OperatorVisuals.purple), OperatorSeries("Total", data.capacity, OperatorVisuals.cyan)), data.grids.isNotEmpty())
        val (alerts, alertsBody) = OperatorVisuals.panel(ctx, 12)
        val open = data.alerts.count { it.warning }
        heading(alertsBody, "Attention queue", if (open == 0) "Clear" else "$open open")
        data.alerts.forEach { alert ->
            alertsBody.addView(ctx.label(alert.title, 13f, true).apply { setTextColor(ctx.getColor(if (alert.warning) R.color.m3_status_warning_foreground else R.color.m3_success)); setPadding(0, ctx.dp(8), 0, ctx.dp(2)) })
            alertsBody.addView(ctx.label(alert.description, 12f))
        }
        readings.addView(alerts, gap())
        readings.addView(ctx.label("Your energy workspace", 16f, true).apply { androidx.core.view.ViewCompat.setAccessibilityHeading(this, true); setPadding(0, ctx.dp(4), 0, ctx.dp(8)) })
        module("Solar Microgrids & Assets", "GENERATION", "Generation, battery reserves and energy slots.", R.drawable.img_solar_farm,
            listOf(OperatorAction("View Grids", true) { host.openGrids(0) }, OperatorAction("Storage") { host.openGrids(2) }))
        module("Energy Trading & Booking", "MARKETPLACE", "Prosumer bookings and available supply.", R.drawable.bg_energy_home,
            listOf(OperatorAction("Reservations", true) { host.openReservations() }, OperatorAction("Availability") { host.openGrids(4) }))
        module("Transaction Ledger & QR", "ENERGY TRANSFERS", "QR verification, transfers and transaction history.", R.drawable.bg_web_dashboard,
            listOf(OperatorAction("Transactions", true) { host.openTransfers() }, OperatorAction("History") { host.openTransfers(2) }))
        scroll.post { scroll.scrollTo(0, previousScroll) }
        OperatorMotion.enter(readings)
    }
    private fun insight(body: LinearLayout, title: String, value: String, progress: Int, accent: Int, description: String) {
        val ctx = requireContext()
        heading(body, title, value)
        body.addView(OperatorMeterView(ctx, progress, accent), LinearLayout.LayoutParams(-1, ctx.dp(5)).apply { topMargin = ctx.dp(6); bottomMargin = ctx.dp(5) })
        body.addView(ctx.label(description, 12f))
    }
    private fun divider(body: LinearLayout) {
        val ctx = requireContext()
        body.addView(View(ctx).apply { setBackgroundResource(R.color.surface_outline) }, LinearLayout.LayoutParams(-1, ctx.dp(1)).apply { topMargin = ctx.dp(10); bottomMargin = ctx.dp(10) })
    }
    private fun chart(title: String, caption: String, donut: Boolean, series: List<OperatorSeries>, hasData: Boolean) {
        val ctx = requireContext(); val (card, body) = OperatorVisuals.panel(ctx, 12)
        body.addView(ctx.label(title, 16f, true).apply { androidx.core.view.ViewCompat.setAccessibilityHeading(this, true) }); body.addView(ctx.label(caption, 12f))
        if (hasData) body.addView(OperatorChartView(ctx, donut, series), LinearLayout.LayoutParams(-1, ctx.dp(176)))
        else body.addView(ctx.label("No microgrids assigned to display operational distribution.", 13f).apply { setPadding(0, ctx.dp(16), 0, ctx.dp(16)) })
        series.chunked(2).forEach { pair ->
            val row = LinearLayout(ctx)
            pair.forEach { part ->
                val legend = android.text.SpannableString("● ${part.label}: ${number(part.value)}${if (donut) "" else " kWh"}")
                legend.setSpan(android.text.style.ForegroundColorSpan(part.color), 0, 1, android.text.Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
                row.addView(ctx.label("", 12f).apply { text = legend; setPadding(0, ctx.dp(4), 0, ctx.dp(4)) }, LinearLayout.LayoutParams(0, -2, 1f))
            }
            body.addView(row)
        }
        readings.addView(card, gap())
    }
    private fun heading(body: LinearLayout, title: String, value: String) {
        val ctx = requireContext(); val row = LinearLayout(ctx).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        row.addView(ctx.label(title, 14f, true), LinearLayout.LayoutParams(0, -2, 1f).apply { marginEnd = ctx.dp(6) })
        row.addView(OperatorVisuals.pill(ctx, value)); body.addView(row)
    }
    private fun module(title: String, tag: String, description: String, image: Int, actions: List<OperatorAction>) {
        val ctx = requireContext(); val (card, body) = OperatorVisuals.panel(ctx, 12)
        val identity = LinearLayout(ctx).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        identity.addView(OperatorVisuals.thumbnail(ctx, image, 52))
        val labels = ctx.column().apply {
            addView(ctx.label(tag, 10f, true).apply { setTextColor(ctx.getColor(R.color.m3_text_secondary)) })
            addView(ctx.label(title, 15f, true))
            addView(ctx.label(description, 12f).apply { setPadding(0, ctx.dp(3), 0, 0) })
        }
        identity.addView(labels, LinearLayout.LayoutParams(0, -2, 1f)); body.addView(identity)
        val row = LinearLayout(ctx)
        actions.forEachIndexed { index, action -> row.addView(ctx.action(action.label, action.primary, action.run), LinearLayout.LayoutParams(0, -2, 1f).apply { if (index > 0) marginStart = ctx.dp(8) }) }
        body.addView(row); readings.addView(card, gap())
    }
    private fun gap() = LinearLayout.LayoutParams(-1, -2).apply { bottomMargin = requireContext().dp(8) }
    private fun iconButton(icon: Int, description: String, click: () -> Unit) = requireContext().action("", click = click).apply {
        contentDescription = description; setIconResource(icon); iconSize = context.dp(20); iconPadding = 0
        iconGravity = com.google.android.material.button.MaterialButton.ICON_GRAVITY_TEXT_START
        minimumWidth = 0; minWidth = 0; setPaddingRelative(context.dp(12), 0, context.dp(12), 0)
    }
    override fun onSaveInstanceState(outState: Bundle) {
        super.onSaveInstanceState(outState)
        outState.putString("map_node", fleetMap?.selectedId ?: selectedNodeId); outState.putString("map_provider", fleetMap?.provider ?: provider)
        outState.putInt("scroll", if (::scroll.isInitialized) scroll.scrollY else 0)
    }
    override fun onDestroyView() {
        request?.cancel(); fleetMap?.release(); fleetMap = null
        super.onDestroyView()
    }
}
