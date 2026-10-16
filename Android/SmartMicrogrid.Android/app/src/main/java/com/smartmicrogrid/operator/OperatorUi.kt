package com.smartmicrogrid.operator

import android.content.Context
import android.content.res.ColorStateList
import android.graphics.Typeface
import android.os.Bundle
import android.view.Gravity
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.core.widget.addTextChangedListener
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.google.android.material.button.MaterialButton
import com.google.android.material.card.MaterialCardView
import com.google.android.material.tabs.TabLayout
import com.google.android.material.textfield.TextInputEditText
import com.google.android.material.textfield.TextInputLayout
import com.smartmicrogrid.R
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch

internal fun Context.dp(value: Int) = (value * resources.displayMetrics.density).toInt()
internal fun Context.column() = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL }
internal fun Context.label(value: String, size: Float = 14f, bold: Boolean = false) = TextView(this).apply {
    text = value; textSize = size
    setTextColor(context.getColor(if (bold) R.color.m3_text_primary else R.color.m3_text_secondary))
    if (bold) setTypeface(typeface, Typeface.BOLD)
}
internal fun Context.action(label: String, primary: Boolean = false, click: () -> Unit) = MaterialButton(this).apply {
    text = label; textSize = 13f; isAllCaps = false; minHeight = context.dp(48)
    cornerRadius = context.dp(12)
    backgroundTintList = ColorStateList.valueOf(context.getColor(if (primary) R.color.primary else R.color.surface_soft))
    val foreground = if (primary) android.graphics.Color.WHITE else context.getColor(R.color.m3_text_primary)
    setTextColor(foreground); iconTint = ColorStateList.valueOf(foreground)
    setOnClickListener { click() }
}

data class OperatorAction(val label: String, val primary: Boolean = false, val run: () -> Unit)
data class OperatorCard(
    val title: String,
    val subtitle: String = "",
    val badge: String = "",
    val lines: List<String> = emptyList(),
    val meters: List<Pair<String, Int>> = emptyList(),
    val actions: List<OperatorAction> = emptyList(),
    val imageRes: Int? = null
)

internal class OperatorCardAdapter : RecyclerView.Adapter<OperatorCardAdapter.Holder>() {
    private var cards: List<OperatorCard> = emptyList()
    private val entered = mutableSetOf<String>()
    class Holder(val card: MaterialCardView, val body: LinearLayout) : RecyclerView.ViewHolder(card)
    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder {
        val ctx = parent.context
        val body = ctx.column().apply { setPadding(ctx.dp(14), ctx.dp(12), ctx.dp(14), ctx.dp(10)) }
        val card = MaterialCardView(ctx).apply {
            radius = ctx.dp(18).toFloat(); cardElevation = 0f; strokeWidth = ctx.dp(1)
            strokeColor = ctx.getColor(R.color.surface_outline)
            layoutParams = RecyclerView.LayoutParams(-1, -2).apply { bottomMargin = ctx.dp(10) }
            addView(body)
        }
        return Holder(card, body)
    }
    override fun getItemCount() = cards.size
    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = cards[position]; val body = holder.body; val ctx = body.context
        body.removeAllViews()
        val heading = LinearLayout(ctx).apply { gravity = Gravity.CENTER_VERTICAL }
        item.imageRes?.let { heading.addView(OperatorVisuals.thumbnail(ctx, it)) }
        val identity = ctx.column()
        identity.addView(ctx.label(item.title, 16f, true))
        if (item.badge.isNotBlank()) identity.addView(
            OperatorVisuals.pill(ctx, item.badge, item.badge in setOf("Low charge", "Offline", "Maintenance", "Pending", "Rejected", "Cancelled", "Expired")),
            LinearLayout.LayoutParams(-2, -2).apply { topMargin = ctx.dp(5) })
        heading.addView(identity, LinearLayout.LayoutParams(0, -2, 1f))
        body.addView(heading)
        if (item.subtitle.isNotBlank()) body.addView(ctx.label(item.subtitle, 12f).apply { setPadding(0, ctx.dp(3), 0, ctx.dp(4)) })
        item.lines.forEach { body.addView(ctx.label(it, if (it.contains("kWh")) 14f else 13f, it.contains("kWh")).apply { setPadding(0, ctx.dp(5), 0, ctx.dp(4)) }) }
        item.meters.forEach { (label, percentage) ->
            body.addView(ctx.label("$label · $percentage%", 12f).apply { setPadding(0, ctx.dp(6), 0, ctx.dp(3)) })
            body.addView(OperatorMeterView(ctx, percentage, if (percentage < 20) OperatorVisuals.amber else OperatorVisuals.blue), LinearLayout.LayoutParams(-1, ctx.dp(7)))
        }
        item.actions.chunked(2).forEach { actions ->
            val row = LinearLayout(ctx).apply { setPadding(0, ctx.dp(6), 0, 0) }
            actions.forEachIndexed { index, action ->
                row.addView(ctx.action(action.label, action.primary, action.run).apply {
                    contentDescription = "${action.label}: ${item.title}"
                }, LinearLayout.LayoutParams(0, -2, 1f).apply { if (index > 0) marginStart = ctx.dp(8) })
            }
            body.addView(row)
        }
    }
    override fun onViewAttachedToWindow(holder: Holder) {
        val item = cards.getOrNull(holder.adapterPosition) ?: return
        if (entered.add(item.title + item.subtitle + item.badge)) OperatorMotion.enter(holder.card)
        else { holder.card.alpha = 1f; holder.card.translationY = 0f }
    }
    override fun onViewDetachedFromWindow(holder: Holder) { holder.card.animate().cancel(); holder.card.alpha = 1f; holder.card.translationY = 0f }
    fun submit(items: List<OperatorCard>) { cards = items; notifyDataSetChanged() }
}

/** Consistent native list, refresh, search, errors and touch targets across operator tools. */
abstract class OperatorListFragment : Fragment() {
    protected open val workspaceRole = "MicrogridOperator"
    private fun canEnter() = com.smartmicrogrid.utils.SessionManager.getUserRole() == workspaceRole
    protected val repository = OperatorRepository()
    protected lateinit var controls: LinearLayout
    protected lateinit var summary: TextView
    private lateinit var notice: TextView
    private lateinit var refresh: SwipeRefreshLayout
    private lateinit var empty: LinearLayout
    private val adapter = OperatorCardAdapter()
    private var loadJob: Job? = null
    private var actionJob: Job? = null
    protected var query: String = ""
    private var searchInput: TextInputEditText? = null

    override fun onCreate(savedInstanceState: Bundle?) { super.onCreate(savedInstanceState); query = savedInstanceState?.getString("query").orEmpty() }
    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, state: Bundle?): View {
        val ctx = requireContext()
        val root = ctx.column().apply { setPadding(ctx.dp(16), ctx.dp(8), ctx.dp(16), 0) }
        val (controlCard, controlBody) = OperatorVisuals.panel(ctx, 10)
        controls = controlBody; root.addView(controlCard, LinearLayout.LayoutParams(-1, -2).apply { bottomMargin = ctx.dp(8) })
        summary = ctx.label("", 13f).apply { setPadding(0, ctx.dp(4), 0, ctx.dp(10)); accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE }
        root.addView(summary)
        notice = ctx.label("", 13f).apply { visibility = View.GONE; setTextColor(ctx.getColor(R.color.m3_error)); setPadding(0, 0, 0, ctx.dp(8)); accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE }
        root.addView(notice)
        val frame = FrameLayout(ctx)
        refresh = SwipeRefreshLayout(ctx).apply {
            setColorSchemeResources(R.color.primary)
            addView(RecyclerView(ctx).apply { layoutManager = LinearLayoutManager(ctx); adapter = this@OperatorListFragment.adapter; clipToPadding = false; setPadding(0, 0, 0, ctx.dp(12)) })
            setOnRefreshListener { reload() }
        }
        frame.addView(refresh, FrameLayout.LayoutParams(-1, -1))
        empty = ctx.column().apply {
            gravity = Gravity.CENTER; setPadding(ctx.dp(20), ctx.dp(12), ctx.dp(20), ctx.dp(12))
            addView(ctx.label("No matching records", 16f, true).apply { gravity = Gravity.CENTER })
            addView(ctx.label("Try another filter, or refresh to load the latest records.", 13f).apply { gravity = Gravity.CENTER })
            addView(ctx.action("Refresh") { reload() })
        }
        frame.addView(empty, FrameLayout.LayoutParams(-1, -1))
        root.addView(frame, LinearLayout.LayoutParams(-1, 0, 1f))
        return root
    }
    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        if (!canEnter()) { showError("Sign in with the correct account to open this workspace."); return }
        configure(savedInstanceState)
    }
    protected abstract fun configure(state: Bundle?)
    protected abstract suspend fun load()
    override fun onResume() { super.onResume(); reload() }
    protected fun reload() {
        if (!isAdded || view == null || !canEnter()) return
        loadJob?.cancel()
        loadJob = viewLifecycleOwner.lifecycleScope.launch {
            refresh.isRefreshing = true; notice.visibility = View.GONE; empty.visibility = View.GONE
            try { load() }
            catch (error: CancellationException) { throw error }
            catch (error: Exception) { showCards(emptyList()); showError(error.message ?: "Unable to connect. Refresh to retry.") }
            finally { refresh.isRefreshing = false }
        }
    }
    protected fun showCards(cards: List<OperatorCard>) { adapter.submit(cards); empty.visibility = if (cards.isEmpty()) View.VISIBLE else View.GONE }
    protected fun showError(message: String) { notice.text = message; notice.setTextColor(requireContext().getColor(R.color.m3_error)); notice.visibility = View.VISIBLE }
    protected fun perform(success: String = "Saved", operation: suspend () -> Unit) {
        if (actionJob?.isActive == true) return
        notice.text = "Working…"; notice.setTextColor(requireContext().getColor(R.color.m3_text_secondary)); notice.visibility = View.VISIBLE
        actionJob = viewLifecycleOwner.lifecycleScope.launch {
            try { check(canEnter()) { "Sign in again to continue." }; operation(); if (success.isNotBlank()) Toast.makeText(requireContext(), success, Toast.LENGTH_SHORT).show(); reload() }
            catch (error: CancellationException) { throw error }
            catch (error: Exception) { showError(error.message ?: "The action failed. Refresh and retry.") }
        }
    }
    protected fun search(hint: String, changed: () -> Unit) {
        val ctx = requireContext()
        val input = TextInputEditText(ctx).apply { setSingleLine(); setText(query); textSize = 14f; minHeight = ctx.dp(48) }
        searchInput = input
        controls.addView(TextInputLayout(ctx).apply {
            this.hint = hint; boxBackgroundMode = TextInputLayout.BOX_BACKGROUND_OUTLINE; endIconMode = TextInputLayout.END_ICON_CLEAR_TEXT
            setBoxCornerRadii(ctx.dp(12).toFloat(), ctx.dp(12).toFloat(), ctx.dp(12).toFloat(), ctx.dp(12).toFloat())
            setStartIconDrawable(R.drawable.ic_nav_search)
            addView(input)
        })
        input.addTextChangedListener { query = it?.toString().orEmpty(); changed() }
    }
    protected fun clearSearch() { query = ""; searchInput?.setText("") }
    protected fun tabs(labels: List<String>, selected: Int, changed: (Int) -> Unit): TabLayout {
        return TabLayout(requireContext()).apply {
            tabMode = TabLayout.MODE_SCROLLABLE; tabGravity = TabLayout.GRAVITY_START
            labels.forEach { addTab(newTab().setText(it)) }; getTabAt(selected)?.select()
            addOnTabSelectedListener(object : TabLayout.OnTabSelectedListener {
                override fun onTabSelected(tab: TabLayout.Tab) { changed(tab.position) }
                override fun onTabUnselected(tab: TabLayout.Tab) {}
                override fun onTabReselected(tab: TabLayout.Tab) {}
            })
            controls.addView(this, LinearLayout.LayoutParams(-1, requireContext().dp(48)))
        }
    }
    override fun onSaveInstanceState(outState: Bundle) { super.onSaveInstanceState(outState); outState.putString("query", query) }
    override fun onDestroyView() { loadJob?.cancel(); actionJob?.cancel(); searchInput = null; adapter.submit(emptyList()); super.onDestroyView() }
}
