package com.smartmicrogrid.prosumer

import android.os.Bundle
import android.view.*
import android.widget.*
import androidx.core.view.ViewCompat
import androidx.core.widget.NestedScrollView
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.smartmicrogrid.M2.ProsumerMainActivity
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.R
import com.smartmicrogrid.models.*
import com.smartmicrogrid.operator.*
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.*

class ProsumerHomeFragment : Fragment() {
    private val repository = ProsumerRepository()
    private lateinit var refresh: SwipeRefreshLayout
    private lateinit var scroll: NestedScrollView
    private lateinit var body: LinearLayout
    private lateinit var readings: LinearLayout
    private lateinit var notice: TextView
    private var request: Job? = null
    private var bookings: List<Reservation>? = null
    private var transfers: List<Transaction>? = null
    private var grids: List<Microgrid>? = null
    private var offers: List<EnergySlot>? = null
    private var map: OperatorFleetMap? = null
    private var selectedNode: String? = null
    private var provider = "google"
    private var scrollPosition = 0
    private val host get() = requireActivity() as ProsumerMainActivity

    override fun onCreate(state: Bundle?) {
        super.onCreate(state); selectedNode = state?.getString("node"); provider = state?.getString("provider") ?: "google"; scrollPosition = state?.getInt("scroll") ?: 0
    }
    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, state: Bundle?): View {
        val ctx = requireContext()
        body = ctx.column().apply { setPadding(ctx.dp(14), ctx.dp(8), ctx.dp(14), ctx.dp(16)) }
        scroll = NestedScrollView(ctx).apply { isFillViewport = true; isVerticalScrollBarEnabled = false; addView(body) }
        refresh = SwipeRefreshLayout(ctx).apply { setColorSchemeResources(R.color.primary); addView(scroll); setOnChildScrollUpCallback { _, _ -> scroll.canScrollVertically(-1) }; setOnRefreshListener { load() } }
        return refresh
    }
    override fun onViewCreated(view: View, state: Bundle?) {
        val ctx = requireContext(); val name = SessionManager.getUser()?.firstName.orEmpty().ifBlank { "there" }
        body.addView(OperatorVisuals.photo(ctx, "Hello, $name", "Clean energy, planned around you.", R.drawable.bg_energy_home, 88, compact = true), gap())
        val actions = LinearLayout(ctx).apply { isBaselineAligned = false }
        actions.addView(ctx.action("Find energy", true) { host.openExplore() }.apply { setIconResource(R.drawable.ic_solar_panel); letterSpacing = 0f }, LinearLayout.LayoutParams(0, -1, 1f))
        actions.addView(ctx.action("My bookings") { host.openBookings() }.apply { setIconResource(R.drawable.ic_nav_reservations); letterSpacing = 0f }, LinearLayout.LayoutParams(0, -1, 1f).apply { marginStart = ctx.dp(8) })
        body.addView(actions, gap())
        notice = ctx.label("Loading your energy workspace…", 12f).apply { accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE; setPadding(0, 0, 0, ctx.dp(8)) }
        body.addView(notice)
        readings = ctx.column(); body.addView(readings)
    }
    override fun onResume() { super.onResume(); load() }
    private suspend fun <T> fetch(block: suspend () -> T): Result<T> = try { Result.success(block()) } catch (cancelled: CancellationException) { throw cancelled } catch (error: Exception) { Result.failure(error) }
    private fun load() {
        request?.cancel()
        request = viewLifecycleOwner.lifecycleScope.launch {
            refresh.isRefreshing = true
            try {
                repository.ensureAccess()
                val context = requireContext()
                val results = coroutineScope {
                    val b = async { fetch { repository.reservations(context) } }
                    val t = async { fetch { repository.transactions() } }
                    val g = async { fetch { repository.grids() } }
                    val o = async { fetch { repository.offers() } }
                    Quad(b.await(), t.await(), g.await(), o.await())
                }
                val offline = results.b.isFailure
                bookings = results.b.getOrNull() ?: repository.cached(context).takeIf { it.isNotEmpty() } ?: bookings
                transfers = results.t.getOrNull() ?: transfers; grids = results.g.getOrNull() ?: grids; offers = results.o.getOrNull() ?: offers
                val errors = listOf(results.b.exceptionOrNull(), results.t.exceptionOrNull(), results.g.exceptionOrNull(), results.o.exceptionOrNull()).filterNotNull()
                notice.text = if (errors.isEmpty()) "Updated just now · Pull to refresh" else (if (offline && bookings != null) "Saved booking copy. " else "") + "Some information could not refresh. ${errors.first().message.orEmpty()}"
                notice.setTextColor(ctxColor(if (errors.isEmpty()) R.color.m3_text_secondary else R.color.m3_status_warning_foreground))
                render()
            } catch (cancelled: CancellationException) { throw cancelled }
            catch (error: Exception) { notice.text = error.message ?: "Unable to connect. Pull to refresh." }
            finally { refresh.isRefreshing = false }
        }
    }
    private fun render() {
        val ctx = requireContext(); val oldScroll = if (readings.childCount > 0) scroll.scrollY else scrollPosition
        map?.let { selectedNode = it.selectedId; provider = it.provider; it.release() }; map = null
        readings.removeAllViews()
        section("Your bookings")
        val rows = bookings
        val upcoming = rows?.filter { it.status == "Approved" && (OperatorRules.timestamp(it.startTime) ?: 0) > System.currentTimeMillis() }
        val stats = listOf(
            stat("Awaiting approval", rows?.count { it.status == "Pending" }?.toDouble(), "", "Requests with your operator", R.drawable.ic_nav_pending, OperatorVisuals.amber),
            stat("Upcoming", upcoming?.size?.toDouble(), "", "Approved deliveries", R.drawable.ic_nav_reservations, OperatorVisuals.blue),
            stat("Reserved energy", rows?.filter { it.status in ProsumerRules.current }?.sumOf { it.energyAmount }, " kWh", "Pending and approved", R.drawable.ic_solar_panel, OperatorVisuals.green),
            stat("Completed this month", rows?.let { ProsumerRules.completedThisMonth(it).toDouble() }, "", rows?.let { "${it.count { row -> row.status == "Completed" }} completed overall" } ?: "Completed bookings", R.drawable.ic_nav_transactions, OperatorVisuals.purple))
        stats.chunked(2).forEach { pair ->
            val row = LinearLayout(ctx).apply { isBaselineAligned = false }
            pair.forEachIndexed { i, card -> row.addView(card, LinearLayout.LayoutParams(0, -1, 1f).apply { if (i > 0) marginStart = ctx.dp(8) }) }; readings.addView(row, gap())
        }
        val next = upcoming?.minByOrNull { OperatorRules.timestamp(it.startTime) ?: Long.MAX_VALUE }
            ?: rows?.filter { it.status == "Pending" && (OperatorRules.timestamp(it.startTime) ?: 0) > System.currentTimeMillis() }?.minByOrNull { OperatorRules.timestamp(it.startTime) ?: Long.MAX_VALUE }
        val (nextCard, nextBody) = OperatorVisuals.panel(ctx, 12)
        nextBody.addView(ctx.label("Next delivery", 16f, true))
        if (next != null) {
            nextBody.addView(OperatorVisuals.pill(ctx, next.status, next.status == "Pending"))
            nextBody.addView(ctx.label(next.microgridName ?: "Your microgrid", 15f, true).apply { setPadding(0, ctx.dp(6), 0, ctx.dp(3)) })
            nextBody.addView(ctx.label("${OperatorOverview.format(next.energyAmount)} kWh · ${TransactionUiFormatters.dateTime(next.startTime)}", 13f))
            val transaction = transfers?.firstOrNull { it.reservationId == next.id }
            nextBody.addView(ctx.label(if (next.status == "Pending") "Waiting for operator approval." else transaction?.let { ProsumerRules.step(it.status) } ?: "Your operator will prepare the transaction QR.", 12f))
            nextBody.addView(ctx.action(if (transaction?.qrCodeData?.isNotBlank() == true) "Show delivery QR" else "View booking", true) {
                if (transaction?.qrCodeData?.isNotBlank() == true) ProsumerTransferNavigation.qr(ctx, transaction, "${TransactionUiFormatters.dateTime(next.startTime)} to ${TransactionUiFormatters.dateTime(next.endTime)}") else host.openBookings(if (next.status == "Approved") 1 else 0, "RES-${next.id.takeLast(8)}")
            })
        } else {
            nextBody.addView(ctx.label(if (rows == null) "Booking information is unavailable. Refresh to try again." else "No upcoming delivery. Browse offers to plan your next energy booking.", 13f))
            nextBody.addView(ctx.action("Explore available energy", true) { host.openExplore() })
        }
        readings.addView(nextCard, gap())
        val ready = transfers?.filter { it.status !in ProsumerRules.closedTransfers && it.qrCodeData.isNotBlank() }
        if (!ready.isNullOrEmpty()) {
            val (card, detail) = OperatorVisuals.panel(ctx, 12)
            detail.addView(ctx.label("${ready.size} delivery QR${if (ready.size == 1) "" else "s"} ready", 15f, true))
            detail.addView(ctx.label("Show your transaction QR when you meet the operator.", 12f))
            detail.addView(ctx.action("Open transfers") { host.openTransfers(1) }); readings.addView(card, gap())
        }
        section("Find your energy")
        workspace("Available energy", offers?.let { "${it.size} offers · ${OperatorOverview.format(it.sumOf { slot -> slot.availableAmount })} kWh available" } ?: "Compare delivery times, supply and LKR prices.", R.drawable.img_solar_farm, "Browse offers") { host.openExplore() }
        workspace("My transfers & QR", "Track verification, delivery and completed records.", R.drawable.bg_web_dashboard, "View transfers") { host.openTransfers() }
        if (rows != null) {
            section("Your energy activity")
            val statuses = listOf("Pending", "Approved", "Completed", "Cancelled")
            val colors = listOf(OperatorVisuals.amber, OperatorVisuals.blue, OperatorVisuals.green, OperatorVisuals.red)
            chart("Booking progress", true, statuses.mapIndexed { i, s -> OperatorSeries(s, rows.count { it.status == s }.toDouble(), colors[i]) } +
                OperatorSeries("Other", rows.count { it.status !in statuses }.toDouble(), OperatorVisuals.purple), rows.isNotEmpty())
            chart("Energy by booking status", false, statuses.mapIndexed { i, s -> OperatorSeries(s, rows.filter { it.status == s }.sumOf { it.energyAmount }, colors[i]) }, rows.isNotEmpty())
        }
        if (grids != null) {
            val (card, mapBody) = OperatorVisuals.panel(ctx, 12)
            map = OperatorFleetMap(ctx, grids.orEmpty(), selectedNode, provider).also { mapBody.addView(it) }
            mapBody.addView(ctx.action("Browse microgrids") { host.openExplore(1) }); readings.addView(card, gap())
        }
        scroll.post { scroll.scrollTo(0, oldScroll) }; OperatorMotion.enter(readings)
    }
    private fun stat(title: String, value: Double?, suffix: String, caption: String, icon: Int, color: Int): View {
        if (value != null) return OperatorVisuals.stat(requireContext(), title, value, suffix, caption, color, icon)
        val (card, body) = OperatorVisuals.panel(requireContext(), 10)
        body.addView(requireContext().label(title, 12f)); body.addView(requireContext().label("—", 21f, true)); body.addView(requireContext().label("Refresh to load", 11f)); return card
    }
    private fun section(title: String) { readings.addView(requireContext().label(title, 16f, true).apply { ViewCompat.setAccessibilityHeading(this, true); setPadding(0, context.dp(4), 0, context.dp(8)) }) }
    private fun workspace(title: String, caption: String, image: Int, action: String, click: () -> Unit) {
        val ctx = requireContext(); val (card, inner) = OperatorVisuals.panel(ctx, 12)
        val row = LinearLayout(ctx).apply { gravity = Gravity.CENTER_VERTICAL }; row.addView(OperatorVisuals.thumbnail(ctx, image))
        row.addView(ctx.column().apply { addView(ctx.label(title, 15f, true)); addView(ctx.label(caption, 12f)) }, LinearLayout.LayoutParams(0, -2, 1f))
        inner.addView(row); inner.addView(ctx.action(action, click = click)); readings.addView(card, gap())
    }
    private fun chart(title: String, donut: Boolean, series: List<OperatorSeries>, hasData: Boolean) {
        val ctx = requireContext(); val (card, inner) = OperatorVisuals.panel(ctx, 12)
        inner.addView(ctx.label(title, 15f, true))
        if (hasData) inner.addView(OperatorChartView(ctx, donut, series, countLabel = "bookings"), LinearLayout.LayoutParams(-1, ctx.dp(176))) else inner.addView(ctx.label("Your activity will appear after your first booking.", 12f))
        inner.addView(ctx.label(series.joinToString(" · ") { "${it.label}: ${OperatorOverview.format(it.value)}${if (donut) "" else " kWh"}" }, 12f))
        readings.addView(card, gap())
    }
    private fun gap() = LinearLayout.LayoutParams(-1, -2).apply { bottomMargin = requireContext().dp(8) }
    private fun ctxColor(id: Int) = requireContext().getColor(id)
    override fun onSaveInstanceState(out: Bundle) { super.onSaveInstanceState(out); out.putString("node", map?.selectedId ?: selectedNode); out.putString("provider", map?.provider ?: provider); out.putInt("scroll", scroll.scrollY) }
    override fun onDestroyView() { request?.cancel(); map?.release(); map = null; super.onDestroyView() }
    private data class Quad(val b: Result<List<Reservation>>, val t: Result<List<Transaction>>, val g: Result<List<Microgrid>>, val o: Result<List<EnergySlot>>)
}
