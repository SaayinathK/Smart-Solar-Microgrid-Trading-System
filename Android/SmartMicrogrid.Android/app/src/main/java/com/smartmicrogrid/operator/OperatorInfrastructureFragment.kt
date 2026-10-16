package com.smartmicrogrid.operator

import android.content.Intent
import android.os.Bundle
import android.text.InputType
import androidx.core.widget.addTextChangedListener
import com.google.android.material.textfield.TextInputEditText
import com.google.android.material.textfield.TextInputLayout
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.LinearLayout
import android.widget.Spinner
import androidx.appcompat.app.AlertDialog
import com.google.android.material.button.MaterialButton
import com.smartmicrogrid.M1.microgrid.MicrogridDetailsActivity
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.models.EnergySlot
import com.smartmicrogrid.models.Microgrid

class OperatorInfrastructureFragment : OperatorListFragment() {
    private var mode = 0
    private var status = "All statuses"
    private var minimum = ""
    private var sort = 0
    private lateinit var supplyFilters: LinearLayout
    private lateinit var minimumInput: TextInputEditText
    private lateinit var minimumField: TextInputLayout
    private lateinit var sortSpinner: Spinner
    private lateinit var clearFilters: MaterialButton
    private var grids: List<Microgrid> = emptyList()
    private var slots: List<EnergySlot> = emptyList()
    private lateinit var primary: MaterialButton
    private lateinit var statusSpinner: Spinner

    override fun configure(state: Bundle?) {
        mode = state?.getInt("mode") ?: arguments?.getInt("mode") ?: 0
        status = state?.getString("status") ?: "All statuses"
        minimum = state?.getString("minimum").orEmpty()
        sort = state?.getInt("sort") ?: 0
        tabs(listOf("Microgrids", "Capacity", "Batteries", "Energy slots", "Availability"), mode) {
            mode = it; status = "All statuses"; if (::statusSpinner.isInitialized) statusSpinner.setSelection(0); updateAction(); reload()
        }
        search("Search microgrid or location") { render() }
        val row = LinearLayout(requireContext())
        statusSpinner = Spinner(requireContext()).apply {
            onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) {}
                override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) { status = parent?.getItemAtPosition(position)?.toString() ?: "All statuses"; render() }
            }
        }
        row.addView(statusSpinner, LinearLayout.LayoutParams(0, requireContext().dp(48), 1f))
        primary = requireContext().action("Register", true) {
            when (mode) {
                0 -> OperatorForms.grid(this) { reload() }
                3 -> OperatorForms.publish(this, grids) { reload() }
                4 -> OperatorForms.book(this, slots) { reload() }
            }
        }
        row.addView(primary)
        clearFilters = requireContext().action("Clear filters") {
            clearSearch(); minimumInput.setText(""); sortSpinner.setSelection(0)
            statusSpinner.setSelection(0); render()
        }
        row.addView(clearFilters); controls.addView(row)
        supplyFilters = LinearLayout(requireContext()).apply { gravity = android.view.Gravity.CENTER_VERTICAL }
        minimumInput = TextInputEditText(requireContext()).apply {
            inputType = InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_FLAG_DECIMAL
            textSize = 14f; setSingleLine(); setText(minimum)
        }
        minimumField = TextInputLayout(requireContext()).apply {
            hint = "Minimum kWh"; boxBackgroundMode = TextInputLayout.BOX_BACKGROUND_OUTLINE
            setBoxCornerRadii(12f * resources.displayMetrics.density, 12f * resources.displayMetrics.density, 12f * resources.displayMetrics.density, 12f * resources.displayMetrics.density)
            addView(minimumInput)
        }
        supplyFilters.addView(minimumField, LinearLayout.LayoutParams(0, -2, 1f))
        sortSpinner = Spinner(requireContext()).apply {
            contentDescription = "Sort available energy"
            adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item,
                listOf("Soonest delivery", "Lowest price", "Most energy"))
            setSelection(sort)
            onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) {}
                override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) { sort = position; render() }
            }
        }
        supplyFilters.addView(sortSpinner, LinearLayout.LayoutParams(0, requireContext().dp(48), 1.3f))
        controls.addView(supplyFilters)
        minimumInput.addTextChangedListener { minimum = it?.toString().orEmpty(); render() }
        updateAction()
    }
    private fun updateAction() {
        if (!::primary.isInitialized) return
        primary.visibility = if (mode in listOf(0, 3, 4)) View.VISIBLE else View.GONE
        primary.text = when (mode) { 0 -> "+ Register"; 3 -> "+ Publish"; else -> "+ Book" }
        val statuses = if (mode < 3) listOf("All statuses", "Active", "Inactive", "Maintenance", "Offline") else listOf("All statuses", "Available", "PartiallyReserved", "FullyReserved", "Cancelled", "Expired")
        statusSpinner.adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, statuses)
        statusSpinner.setSelection(statuses.indexOf(status).coerceAtLeast(0))
        statusSpinner.visibility = if (mode == 4) View.GONE else View.VISIBLE
        if (::supplyFilters.isInitialized) supplyFilters.visibility = if (mode == 4) View.VISIBLE else View.GONE
        if (::clearFilters.isInitialized) clearFilters.visibility = if (mode == 4) View.VISIBLE else View.GONE
    }
    override suspend fun load() {
        grids = repository.grids()
        slots = when (mode) { 4 -> repository.available(grids); 3 -> repository.slots(grids); else -> emptyList() }
        render()
    }
    private fun matches(name: String, location: String, rowStatus: String) =
        (status == "All statuses" || rowStatus == status) && (query.isBlank() || "$name $location".contains(query, true))
    private fun render() {
        val filtered = grids.filter { matches(it.name, it.location, it.status) }
        val threshold = minimum.toDoubleOrNull()
        val invalidMinimum = minimum.isNotBlank() && (threshold == null || !threshold.isFinite() || threshold < 0)
        if (::minimumField.isInitialized) minimumField.error = if (mode == 4 && invalidMinimum) "Enter zero or greater" else null
        val matchingSlots = slots.filter { slot ->
            (mode != 4 || (OperatorRules.isLive(slot) && !invalidMinimum && slot.availableAmount >= (threshold ?: 0.0))) &&
                matches(slot.microgridName, "${slot.location} ${slot.id}", slotStatus(slot))
        }.let { rows ->
            if (mode != 4) rows.sortedByDescending { OperatorRules.timestamp(it.startTime) ?: 0 }
            else when (sort) {
                1 -> rows.sortedBy { it.pricePerUnit }
                2 -> rows.sortedByDescending { it.availableAmount }
                else -> rows.sortedBy { OperatorRules.timestamp(it.startTime) ?: Long.MAX_VALUE }
            }
        }
        val cards = when (mode) {
            0 -> filtered.map { grid ->
                OperatorCard(grid.name, grid.location, grid.status,
                    listOf("Generation ${grid.capacity} kW · Available ${grid.availableCapacity} kWh", "Battery ${grid.currentBatteryLevel} / ${grid.batteryCapacity} kWh"),
                    actions = listOf(
                        OperatorAction("Details") { startActivity(Intent(requireContext(), MicrogridDetailsActivity::class.java).putExtra("MICROGRID_ID", grid.id)) },
                        OperatorAction("Edit", true) { OperatorForms.grid(this, grid) { reload() } },
                        OperatorAction(if (grid.status == "Active") "Deactivate" else "Activate") { changeStatus(grid) },
                        OperatorAction("Publish energy") { OperatorForms.publish(this, grids, grid.id) { reload() } }
                    ), imageRes = com.smartmicrogrid.R.drawable.img_solar_farm)
            }
            1 -> filtered.map { grid ->
                val capacity = grid.capacity.takeIf { it > 0 } ?: 1.0
                OperatorCard(grid.name, grid.location, grid.status,
                    listOf("Generation capacity ${grid.capacity} kW", "Available ${grid.availableCapacity} kWh", "Reserved ${grid.reservedCapacity} kWh · Used ${grid.usedCapacity} kWh"),
                    listOf("Available capacity" to (grid.availableCapacity / capacity * 100).toInt(), "Allocated capacity" to ((grid.reservedCapacity + grid.usedCapacity) / capacity * 100).toInt()),
                    listOf(OperatorAction("View details") { startActivity(Intent(requireContext(), MicrogridDetailsActivity::class.java).putExtra("MICROGRID_ID", grid.id)) }), imageRes = com.smartmicrogrid.R.drawable.img_solar_farm)
            }
            2 -> filtered.map { grid ->
                val pct = if (grid.batteryCapacity > 0) (grid.currentBatteryLevel / grid.batteryCapacity * 100).toInt() else 0
                OperatorCard(grid.name, grid.location, if (grid.batteryCapacity <= 0) "Not configured" else if (pct < 20) "Low charge" else "Stored energy",
                    listOf("Stored ${grid.currentBatteryLevel} / ${grid.batteryCapacity} kWh", "Headroom ${(grid.batteryCapacity - grid.currentBatteryLevel).coerceAtLeast(0.0)} kWh · ${grid.batteryStorageSlots} storage slots", "Updated ${TransactionUiFormatters.dateTime(grid.updatedAt)}"),
                    listOf("Battery charge" to pct),
                    listOf(OperatorAction(if (grid.batteryCapacity > 0) "Update charge" else "Configure battery", true) {
                        if (grid.batteryCapacity > 0) OperatorForms.battery(this, grid) { reload() } else OperatorForms.grid(this, grid) { reload() }
                    }), imageRes = com.smartmicrogrid.R.drawable.img_battery_storage)
            }
            else -> matchingSlots.map { slot ->
                val actions = if (mode == 4) listOf(OperatorAction("Book for prosumer", true) { OperatorForms.book(this, listOf(slot)) { reload() } })
                else if (slot.status == "Available" && (OperatorRules.timestamp(slot.endTime) ?: 0) > System.currentTimeMillis()) listOf(OperatorAction("Cancel slot") { cancelSlot(slot) }) else emptyList()
                OperatorCard(slot.microgridName, "${slot.location} · Slot ${slot.id.takeLast(8).uppercase()}", slotStatus(slot),
                    listOf("${slot.availableAmount} / ${slot.energyAmount} kWh available · LKR ${"%.2f".format(slot.pricePerUnit)}/kWh", "${TransactionUiFormatters.dateTime(slot.startTime)}", "Until ${TransactionUiFormatters.dateTime(slot.endTime)}"), actions = actions, imageRes = com.smartmicrogrid.R.drawable.bg_energy_home)
            }
        }
        summary.text = when (mode) {
            0 -> "${grids.size} assigned microgrids · ${grids.count { it.status == "Active" }} active"
            1 -> "Generation ${"%.1f".format(grids.sumOf { it.capacity })} kW · Available ${"%.1f".format(grids.sumOf { it.availableCapacity })} kWh"
            2 -> "Stored ${"%.1f".format(grids.sumOf { it.currentBatteryLevel })} / ${"%.1f".format(grids.sumOf { it.batteryCapacity })} kWh · ${grids.count { it.batteryCapacity > 0 && it.batteryPercentage < 20 }} low-charge systems"
            4 -> "${matchingSlots.size} offers · ${"%.1f".format(matchingSlots.sumOf { it.availableAmount })} kWh · From LKR ${matchingSlots.minOfOrNull { it.pricePerUnit }?.let { "%.2f".format(it) } ?: "—"}/kWh"
            else -> "${slots.count { OperatorRules.isLive(it) }} live slots · ${"%.1f".format(slots.filter { OperatorRules.isLive(it) }.sumOf { it.availableAmount })} kWh available"
        } + " · Pull to refresh"
        showCards(cards)
    }
    private fun slotStatus(slot: EnergySlot) = if ((OperatorRules.timestamp(slot.endTime) ?: Long.MAX_VALUE) <= System.currentTimeMillis() && slot.status in setOf("Available", "PartiallyReserved")) "Expired" else slot.status
    private fun changeStatus(grid: Microgrid) {
        val next = if (grid.status == "Active") "Inactive" else "Active"
        AlertDialog.Builder(requireContext()).setTitle("${if (next == "Active") "Activate" else "Deactivate"} ${grid.name}?")
            .setMessage("The server will check active reservations before allowing a status change.")
            .setNegativeButton("Keep current", null).setPositiveButton("Confirm") { _, _ ->
                perform("Status updated") { repository.api.updateMicrogridStatus(grid.id, mapOf("status" to next)).checked() }
            }.show()
    }
    private fun cancelSlot(slot: EnergySlot) {
        AlertDialog.Builder(requireContext()).setTitle("Cancel this energy slot?")
            .setMessage("${slot.microgridName} · ${slot.energyAmount} kWh\nThe server will check reservations before cancelling.")
            .setNegativeButton("Keep slot", null).setPositiveButton("Cancel slot") { _, _ ->
                perform("Slot cancelled") { repository.api.updateSlotStatus(slot.id, mapOf("status" to "Cancelled")).checked() }
            }.show()
    }
    override fun onSaveInstanceState(outState: Bundle) { super.onSaveInstanceState(outState); outState.putInt("mode", mode); outState.putString("status", status); outState.putString("minimum", minimum); outState.putInt("sort", sort) }
}
