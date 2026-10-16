package com.smartmicrogrid.prosumer

import android.content.Intent
import android.os.Bundle
import android.text.InputType
import android.view.View
import android.widget.*
import androidx.core.widget.addTextChangedListener
import com.google.android.material.textfield.TextInputEditText
import com.google.android.material.textfield.TextInputLayout
import com.smartmicrogrid.M1.microgrid.MicrogridDetailsActivity
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.R
import com.smartmicrogrid.models.*
import com.smartmicrogrid.operator.*

abstract class ProsumerListFragment : OperatorListFragment() {
    override val workspaceRole = "Prosumer"
    protected val prosumer = ProsumerRepository()
}

class ProsumerExploreFragment : ProsumerListFragment() {
    private var mode = 0
    private var sort = 0
    private var minimum = ""
    private var selectedGrid: String? = null
    private var grids = emptyList<Microgrid>()
    private var offers = emptyList<EnergySlot>()
    private lateinit var minimumField: TextInputLayout
    private lateinit var scope: TextView
    private lateinit var sorter: Spinner
    private lateinit var statusSelector: Spinner
    private var status = "All statuses"

    override fun configure(state: Bundle?) {
        mode = state?.getInt("mode") ?: arguments?.getInt("mode") ?: 0
        sort = state?.getInt("sort") ?: 0; minimum = state?.getString("minimum").orEmpty()
        status = state?.getString("status") ?: "All statuses"
        selectedGrid = if (state != null) state.getString("grid") else arguments?.getString("grid")
        tabs(listOf("Energy offers", "Microgrids", "Capacity", "Batteries", "Energy slots"), mode) { mode = it; sort = 0; status = "All statuses"; updateControls(); reload() }
        search("Search location or microgrid") { render() }
        val ctx = requireContext(); val row = LinearLayout(ctx).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        val input = TextInputEditText(ctx).apply {
            inputType = InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_FLAG_DECIMAL
            textSize = 14f; setSingleLine(); setText(minimum)
        }
        minimumField = TextInputLayout(ctx).apply {
            hint = "Minimum kWh"; boxBackgroundMode = TextInputLayout.BOX_BACKGROUND_OUTLINE
            setBoxCornerRadii(ctx.dp(12).toFloat(), ctx.dp(12).toFloat(), ctx.dp(12).toFloat(), ctx.dp(12).toFloat()); addView(input)
        }
        row.addView(minimumField, LinearLayout.LayoutParams(0, -2, 1f))
        sorter = Spinner(ctx).apply { contentDescription = "Sort energy offers or microgrids" }
        row.addView(sorter, LinearLayout.LayoutParams(0, ctx.dp(48), 1.2f)); controls.addView(row)
        sorter.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onNothingSelected(parent: AdapterView<*>?) {}
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) { sort = position; render() }
        }
        val footer = LinearLayout(ctx).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        scope = ctx.label("", 12f); footer.addView(scope, LinearLayout.LayoutParams(0, -2, 1f))
        statusSelector = Spinner(ctx).apply {
            contentDescription = "Resource status"
            onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) {}
                override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) { status = parent?.getItemAtPosition(position)?.toString() ?: "All statuses"; render() }
            }
        }
        footer.addView(statusSelector, LinearLayout.LayoutParams(0, ctx.dp(48), 1f))
        footer.addView(ctx.action("Clear filters") { selectedGrid = null; input.setText(""); clearSearch(); sorter.setSelection(0); statusSelector.setSelection(0); render() })
        controls.addView(footer)
        input.addTextChangedListener { minimum = it?.toString().orEmpty(); render() }
        updateControls()
    }
    private fun updateControls() {
        if (!::sorter.isInitialized) return
        minimumField.visibility = if (mode == 0 || mode == 4) View.VISIBLE else View.GONE
        sorter.adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item,
            if (mode == 0 || mode == 4) listOf("Soonest delivery", "Lowest price", "Most energy") else if (mode == 3) listOf("Highest charge", "Lowest charge", "Name") else listOf("Highest capacity", "Lowest capacity", "Name"))
        sorter.setSelection(sort.coerceIn(0, 2))
        val statuses = if (mode == 4) listOf("All statuses", "Available", "PartiallyReserved", "FullyReserved", "Expired", "Cancelled") else listOf("All statuses", "Active", "Inactive", "Maintenance", "Offline")
        statusSelector.adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, statuses)
        statusSelector.setSelection(statuses.indexOf(status).coerceAtLeast(0))
        statusSelector.visibility = if (mode >= 2) View.VISIBLE else View.GONE
        scope.visibility = if (mode >= 2) View.GONE else View.VISIBLE
    }
    override suspend fun load() {
        when (mode) {
            0 -> offers = prosumer.offers()
            1 -> grids = prosumer.grids()
            4 -> offers = prosumer.api.getEnergySlots().data()
            else -> grids = prosumer.resources()
        }
        render()
    }
    private fun render() {
        if (!::scope.isInitialized) return
        scope.text = if (selectedGrid == null || mode != 0) "Pull to refresh" else "Offers from selected hub"
        val threshold = minimum.toDoubleOrNull()
        val invalid = minimum.isNotBlank() && (threshold == null || !threshold.isFinite() || threshold < 0)
        minimumField.error = if (invalid && (mode == 0 || mode == 4)) "Enter zero or greater" else null
        fun matches(name: String, location: String) = query.isBlank() || "$name $location".contains(query, true)
        fun slotStatus(slot: EnergySlot) = if (slot.status in setOf("Available", "PartiallyReserved") && (OperatorRules.timestamp(slot.endTime) ?: Long.MAX_VALUE) <= System.currentTimeMillis()) "Expired" else slot.status
        if (mode == 0 || mode == 4) {
            val rows = offers.filter { (selectedGrid == null || it.microgridNodeId == selectedGrid) && matches(it.microgridName, it.location) && !invalid && it.availableAmount >= (threshold ?: 0.0) && (mode != 4 || status == "All statuses" || slotStatus(it) == status) }
                .let { list -> when (sort) { 1 -> list.sortedBy { it.pricePerUnit }; 2 -> list.sortedByDescending { it.availableAmount }; else -> list.sortedBy { OperatorRules.timestamp(it.startTime) ?: Long.MAX_VALUE } } }
            summary.text = if (mode == 0) "${rows.size} offers · ${OperatorOverview.format(rows.sumOf { it.availableAmount })} kWh\n${rows.minOfOrNull { it.pricePerUnit }?.let { "From LKR ${"%.2f".format(it)} / kWh" } ?: "Try another location or lower the minimum energy."}"
                else "${rows.size} published slots · ${rows.count { ProsumerRules.bookingIssue(it) == null }} bookable now\nIncludes reserved, cancelled and ended delivery windows."
            showCards(rows.map { slot ->
                val issue = ProsumerRules.bookingIssue(slot)
                OperatorCard(slot.microgridName, slot.location, if (mode == 4) slotStatus(slot) else if (issue == null) "Bookable" else "Check delivery",
                    listOf("${OperatorOverview.format(slot.availableAmount)} kWh available · LKR ${"%.2f".format(slot.pricePerUnit)} / kWh", "${TransactionUiFormatters.dateTime(slot.startTime)} to ${TransactionUiFormatters.dateTime(slot.endTime)}") + listOfNotNull(issue),
                    meters = listOf("Supply remaining" to OperatorOverview.percent(slot.availableAmount, slot.energyAmount).toInt()),
                    actions = listOf(OperatorAction(if (issue == null) "Reserve energy" else "Offer details", issue == null) {
                        if (issue == null) perform("") { val fresh = prosumer.api.getEnergySlotById(slot.id).data(); ProsumerForms.book(this, fresh) { reload() } }
                        else androidx.appcompat.app.AlertDialog.Builder(requireContext()).setTitle(slot.microgridName).setMessage(issue).setPositiveButton("Done", null).show()
                    }, OperatorAction("Grid details") { gridDetails(slot.microgridNodeId) }), imageRes = R.drawable.bg_energy_home)
            })
        } else if (mode >= 2) {
            val rows = grids.filter { matches(it.name, it.location) && (status == "All statuses" || it.status == status) }.let { list ->
                when (sort) { 1 -> list.sortedBy { if (mode == 3) it.currentBatteryLevel else it.availableCapacity }; 2 -> list.sortedBy { it.name }; else -> list.sortedByDescending { if (mode == 3) it.currentBatteryLevel else it.availableCapacity } }
            }
            summary.text = if (mode == 3) "${rows.size} storage systems · ${OperatorOverview.format(rows.sumOf { it.currentBatteryLevel })} / ${OperatorOverview.format(rows.sumOf { it.batteryCapacity })} kWh stored"
                else "${rows.size} microgrids · ${OperatorOverview.format(rows.sumOf { it.capacity })} kW generation\n${OperatorOverview.format(rows.sumOf { it.availableCapacity })} available · ${OperatorOverview.format(rows.sumOf { it.reservedCapacity })} reserved · ${OperatorOverview.format(rows.sumOf { it.usedCapacity })} used kWh"
            showCards(rows.map { grid ->
                OperatorCard(grid.name, grid.location, grid.status,
                    if (mode == 3) listOf("${OperatorOverview.format(grid.currentBatteryLevel)} / ${OperatorOverview.format(grid.batteryCapacity)} kWh stored", "${OperatorOverview.format((grid.batteryCapacity - grid.currentBatteryLevel).coerceAtLeast(0.0))} kWh headroom · ${grid.batteryStorageSlots} storage slots", "Updated ${TransactionUiFormatters.dateTime(grid.updatedAt)}")
                    else listOf("${OperatorOverview.format(grid.capacity)} kW generation", "${OperatorOverview.format(grid.availableCapacity)} kWh available", "${OperatorOverview.format(grid.reservedCapacity)} kWh reserved · ${OperatorOverview.format(grid.usedCapacity)} kWh used"),
                    meters = if (mode == 3) listOf("Battery charge" to OperatorOverview.percent(grid.currentBatteryLevel, grid.batteryCapacity).toInt())
                    else listOf("Available" to OperatorOverview.percent(grid.availableCapacity, grid.capacity).toInt(), "Allocated" to OperatorOverview.percent(grid.reservedCapacity + grid.usedCapacity, grid.capacity).toInt()),
                    actions = listOf(OperatorAction("Grid details") { gridDetails(grid.id) }), imageRes = if (mode == 3) R.drawable.img_battery_storage else R.drawable.img_solar_farm)
            })
        } else {
            val rows = grids.filter { matches(it.name, it.location) }.let { list -> when (sort) { 1 -> list.sortedBy { it.availableCapacity }; 2 -> list.sortedBy { it.name }; else -> list.sortedByDescending { it.availableCapacity } } }
            summary.text = "${rows.size} active microgrids · ${OperatorOverview.format(rows.sumOf { it.availableCapacity })} kWh unallocated\nBookable energy is shown under Energy offers."
            showCards(rows.map { grid -> OperatorCard(grid.name, grid.location, "Active",
                listOf("${OperatorOverview.format(grid.availableCapacity)} kWh available capacity", "Generation ${OperatorOverview.format(grid.capacity)} kW · Battery ${OperatorOverview.format(grid.currentBatteryLevel)} kWh"),
                meters = listOf("Battery charge" to OperatorOverview.percent(grid.currentBatteryLevel, grid.batteryCapacity).toInt()),
                actions = listOf(OperatorAction("View offers", true) {
                    (activity as? com.smartmicrogrid.M2.ProsumerMainActivity)?.openExplore(0, grid.id)
                }, OperatorAction("Grid details") { gridDetails(grid.id) }), imageRes = R.drawable.img_solar_farm) })
        }
    }
    private fun gridDetails(id: String) { startActivity(Intent(requireContext(), MicrogridDetailsActivity::class.java).putExtra("MICROGRID_ID", id)) }
    override fun onSaveInstanceState(out: Bundle) { super.onSaveInstanceState(out); out.putInt("mode", mode); out.putInt("sort", sort); out.putString("minimum", minimum); out.putString("grid", selectedGrid); out.putString("status", status) }
}
