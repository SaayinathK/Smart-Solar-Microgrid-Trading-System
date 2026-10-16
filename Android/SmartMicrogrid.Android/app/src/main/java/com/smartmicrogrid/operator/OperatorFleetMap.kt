package com.smartmicrogrid.operator

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.view.View
import android.widget.*
import com.google.android.material.button.MaterialButton
import com.smartmicrogrid.models.Microgrid
import com.smartmicrogrid.ui.GoogleMapView
import com.smartmicrogrid.ui.OpenStreetMapView

internal class OperatorFleetMap(context: Context, nodes: List<Microgrid>, initialId: String?, initialProvider: String) : LinearLayout(context) {
    private val grids = nodes.filter { it.status == "Active" && it.isActive }
    private val frame = FrameLayout(context)
    private val message = context.label("", 13f)
    private val selection = context.label("", 12f)
    private var google: GoogleMapView? = null
    private var leaflet: OpenStreetMapView? = null
    private val openButton: MaterialButton
    var provider: String = initialProvider
        private set
    var selectedId: String? = initialId
        private set

    init {
        orientation = VERTICAL
        addView(context.label("Microgrid locations", 16f, true).apply { androidx.core.view.ViewCompat.setAccessibilityHeading(this, true) })
        addView(context.label("${grids.size} active sites in your network", 12f).apply { setPadding(0, context.dp(3), 0, 0) })
        val controls = LinearLayout(context).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        if (grids.isNotEmpty()) {
            controls.addView(Spinner(context).apply {
                contentDescription = "Select microgrid location"
                adapter = ArrayAdapter(context, android.R.layout.simple_spinner_dropdown_item, grids.map { it.name })
                val index = grids.indexOfFirst { it.id == selectedId }.takeIf { it >= 0 } ?: grids.indexOfFirst(::hasLocation).coerceAtLeast(0)
                selectedId = grids[index].id; setSelection(index)
                onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                    override fun onNothingSelected(parent: AdapterView<*>?) {}
                    override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) { selectedId = grids[position].id; render() }
                }
            }, LayoutParams(0, context.dp(48), 1f))
        }
        controls.addView(Spinner(context).apply {
            contentDescription = "Map provider"
            adapter = ArrayAdapter(context, android.R.layout.simple_spinner_dropdown_item, listOf("Google Maps", "Leaflet OSM"))
            setSelection(if (provider == "leaflet") 1 else 0)
            onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) {}
                override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                    val selected = if (position == 1) "leaflet" else "google"
                    if (provider != selected) { provider = selected; render() }
                }
            }
        }, LayoutParams(context.dp(132), context.dp(48)))
        addView(controls)
        addView(frame, LayoutParams(-1, context.dp(if (grids.isEmpty()) 72 else 168)))
        frame.addView(message, FrameLayout.LayoutParams(-1, -1))
        val locationRow = LinearLayout(context).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        locationRow.addView(selection.apply { setPadding(0, context.dp(6), context.dp(8), 0); accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE }, LayoutParams(0, -2, 1f))
        openButton = context.action("Directions") {
            val node = grids.firstOrNull { it.id == selectedId } ?: return@action
            runCatching { context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse("https://www.google.com/maps/search/?api=1&query=${node.latitude},${node.longitude}"))) }
                .onFailure { Toast.makeText(context, "No map app is available.", Toast.LENGTH_SHORT).show() }
        }
        openButton.contentDescription = "Open selected microgrid in Google Maps"
        locationRow.addView(openButton)
        addView(locationRow)
        render()
    }
    private fun hasLocation(node: Microgrid) = node.latitude.isFinite() && node.longitude.isFinite() && node.latitude in -90.0..90.0 && node.longitude in -180.0..180.0 && (node.latitude != 0.0 || node.longitude != 0.0)
    private fun render() {
        val node = grids.firstOrNull { it.id == selectedId }
        val valid = node != null && hasLocation(node)
        message.visibility = if (valid) View.GONE else View.VISIBLE
        message.text = if (node == null) "No active microgrids assigned to display." else "Location unavailable for ${node.name}. Edit the microgrid to set its coordinates."
        selection.text = node?.location.orEmpty()
        openButton.visibility = if (valid) View.VISIBLE else View.GONE
        google?.visibility = View.GONE; leaflet?.visibility = View.GONE
        if (!valid) return
        if (provider == "google") {
            val map = google ?: GoogleMapView(context).also { google = it; frame.addView(it, 0, FrameLayout.LayoutParams(-1, -1)) }
            map.visibility = View.VISIBLE; map.showLocation(node.latitude, node.longitude, node.name)
        } else {
            val map = leaflet ?: OpenStreetMapView(context).also { leaflet = it; frame.addView(it, 0, FrameLayout.LayoutParams(-1, -1)) }
            map.visibility = View.VISIBLE; map.showLocation(node.latitude, node.longitude, node.name)
        }
    }
    fun release() { frame.removeAllViews(); google?.destroy(); leaflet?.destroy(); google = null; leaflet = null }
}
