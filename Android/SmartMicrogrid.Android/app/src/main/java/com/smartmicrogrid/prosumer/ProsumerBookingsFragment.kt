package com.smartmicrogrid.prosumer

import android.app.DatePickerDialog
import android.os.Bundle
import android.widget.*
import androidx.appcompat.app.AlertDialog
import com.smartmicrogrid.M3.ReservationTransactionFlow
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.models.Reservation
import com.smartmicrogrid.operator.*
import kotlinx.coroutines.CancellationException
import java.text.SimpleDateFormat
import java.util.*

class ProsumerBookingsFragment : ProsumerListFragment() {
    private var tab = 0
    private var status = "All statuses"
    private var date = ""
    private var rows = emptyList<Reservation>()
    private var cached = false
    override fun configure(state: Bundle?) {
        tab = state?.getInt("tab") ?: arguments?.getInt("tab") ?: 0
        status = state?.getString("status") ?: "All statuses"; date = state?.getString("date").orEmpty()
        if (state == null) query = arguments?.getString("query").orEmpty()
        tabs(listOf("Current", "Approved", "All bookings", "History"), tab) { tab = it; render() }
        search("Search hub, booking reference or status") { render() }
        val ctx = requireContext(); val filterRow = LinearLayout(ctx)
        val statuses = listOf("All statuses", "Pending", "Approved", "Completed", "Cancelled", "Rejected", "Expired")
        filterRow.addView(Spinner(ctx).apply {
            contentDescription = "Booking status"; adapter = ArrayAdapter(ctx, android.R.layout.simple_spinner_dropdown_item, statuses); setSelection(statuses.indexOf(status).coerceAtLeast(0))
            onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) {}
                override fun onItemSelected(parent: AdapterView<*>?, view: android.view.View?, position: Int, id: Long) { status = statuses[position]; render() }
            }
        }, LinearLayout.LayoutParams(0, ctx.dp(48), 1f))
        filterRow.addView(ctx.action("Find energy", true) { (activity as? com.smartmicrogrid.M2.ProsumerMainActivity)?.openExplore() })
        controls.addView(filterRow)
        controls.addView(dateFilter(ctx, date, "Delivery date") { date = it; render() })
    }
    override suspend fun load() {
        try { rows = prosumer.reservations(requireContext()); cached = false }
        catch (cancelled: CancellationException) { throw cancelled }
        catch (error: Exception) {
            rows = prosumer.cached(requireContext()); cached = true
            if (rows.isEmpty()) throw error
            showError("Offline copy. Refresh before changing a booking. ${error.message.orEmpty()}")
        }
        render()
    }
    private fun render() {
        val filtered = rows.filter { row -> ProsumerRules.matchesBooking(row, tab) && (status == "All statuses" || row.status == status) &&
            (date.isBlank() || OperatorRules.timestamp(row.startTime)?.let { SimpleDateFormat("yyyy-MM-dd", Locale.ROOT).format(Date(it)) } == date) &&
            (query.isBlank() || "RES-${row.id.takeLast(8)} ${row.id} ${row.microgridName} ${row.microgridNodeId} ${row.energySlotId} ${row.status}".contains(query, true)) }
            .sortedByDescending { OperatorRules.timestamp(it.createdAt) ?: 0 }
        summary.text = "${rows.count { it.status == "Pending" }} awaiting approval · ${rows.count { it.status == "Approved" && (OperatorRules.timestamp(it.startTime) ?: 0) > System.currentTimeMillis() }} upcoming\n${OperatorOverview.format(rows.filter { it.status in ProsumerRules.current }.sumOf { it.energyAmount })} kWh reserved · ${filtered.size} shown${if (cached) " · Offline" else ""}"
        showCards(filtered.map { row ->
            val actions = mutableListOf(OperatorAction("Booking details") { ProsumerForms.details(this, row, cached) })
            if (!cached && row.status in setOf("Approved", "Completed")) actions.add(0, OperatorAction("Transaction / QR", true) { perform("") { ReservationTransactionFlow.open(requireContext(), row, preferQr = true) } })
            if (!cached && ProsumerRules.canChange(row)) {
                actions.add(OperatorAction("Modify") { perform("") { val slots = prosumer.api.getEnergySlots(microgridId = row.microgridNodeId).data(); ProsumerForms.modify(this, row, slots) { reload() } } })
                actions.add(OperatorAction("Cancel booking") { cancel(row) })
            }
            OperatorCard(row.microgridName ?: row.microgridNodeId, "RES-${row.id.takeLast(8).uppercase()}", row.status,
                listOf("${OperatorOverview.format(row.energyAmount)} kWh · ${if (cached) "Saved booking" else "LKR ${"%.2f".format(row.totalEstimatedCost)}"}", "${TransactionUiFormatters.dateTime(row.startTime)} to ${TransactionUiFormatters.dateTime(row.endTime)}") +
                    if (row.status in ProsumerRules.current && !ProsumerRules.canChange(row)) listOf("Changes are closed within 12 hours of delivery.") else emptyList(),
                actions = actions, imageRes = com.smartmicrogrid.R.drawable.bg_energy_home)
        })
    }
    private fun cancel(row: Reservation) {
        AlertDialog.Builder(requireContext()).setTitle("Cancel this booking?")
            .setMessage("${row.microgridName ?: "Microgrid"}\n${row.energyAmount} kWh · ${TransactionUiFormatters.dateTime(row.startTime)}\nCapacity will be returned to the energy slot.")
            .setNegativeButton("Keep booking", null).setPositiveButton("Cancel booking") { _, _ ->
                perform("Booking cancelled. Capacity has been released.") { check(ProsumerRules.canChange(row)) { "Cancellation requires 12 hours' notice." }; prosumer.api.cancelReservation(row.id).checked() }
            }.show()
    }
    override fun onSaveInstanceState(out: Bundle) { super.onSaveInstanceState(out); out.putInt("tab", tab); out.putString("status", status); out.putString("date", date) }
}

internal fun dateFilter(ctx: android.content.Context, initial: String, title: String, changed: (String) -> Unit): com.google.android.material.button.MaterialButton {
    var selected = initial
    val button = ctx.action(initial.ifBlank { title }) {}
    fun pick() {
        val today = Calendar.getInstance()
        DatePickerDialog(ctx, { _, y, m, d -> selected = String.format(Locale.ROOT, "%04d-%02d-%02d", y, m + 1, d); button.text = "$title · $selected"; changed(selected) }, today.get(Calendar.YEAR), today.get(Calendar.MONTH), today.get(Calendar.DAY_OF_MONTH)).show()
    }
    button.setOnClickListener {
        if (selected.isBlank()) pick() else AlertDialog.Builder(ctx).setTitle(title).setItems(arrayOf("Change date", "Clear date")) { _, index ->
            if (index == 0) pick() else { selected = ""; button.text = title; changed("") }
        }.show()
    }
    return button
}
