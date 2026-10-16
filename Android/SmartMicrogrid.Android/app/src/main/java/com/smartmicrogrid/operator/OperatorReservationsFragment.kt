package com.smartmicrogrid.operator

import android.app.DatePickerDialog
import android.os.Bundle
import android.widget.EditText
import android.widget.LinearLayout
import androidx.appcompat.app.AlertDialog
import com.smartmicrogrid.M3.ReservationTransactionFlow
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.models.*
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale

class OperatorReservationsFragment : OperatorListFragment() {
    private var selected = 0
    private var date = ""
    private var rows: List<Reservation> = emptyList()
    private var slots: List<EnergySlot> = emptyList()
    private var counts: ReservationSummary? = null
    override fun configure(state: Bundle?) {
        selected = state?.getInt("tab") ?: 0; date = state?.getString("date").orEmpty()
        tabs(listOf("Pending", "Approved", "All bookings", "History"), selected) { selected = it; render() }
        search("Search prosumer NIC, name, hub or reference") { render() }
        val row = LinearLayout(requireContext())
        row.addView(requireContext().action("Book for prosumer", true) { OperatorForms.book(this, slots) { reload() } }, LinearLayout.LayoutParams(0, -2, 1f))
        val filter = requireContext().action(if (date.isBlank()) "Filter date" else date) {}
        fun chooseDate() {
            val calendar = Calendar.getInstance()
            DatePickerDialog(requireContext(), { _, y, m, d -> date = String.format(Locale.ROOT, "%04d-%02d-%02d", y, m + 1, d); filter.text = date; render() }, calendar.get(Calendar.YEAR), calendar.get(Calendar.MONTH), calendar.get(Calendar.DAY_OF_MONTH)).show()
        }
        filter.setOnClickListener {
            if (date.isBlank()) chooseDate()
            else AlertDialog.Builder(requireContext()).setTitle("Delivery date filter").setItems(arrayOf("Change date", "Clear date")) { _, position ->
                if (position == 0) chooseDate() else { date = ""; filter.text = "Filter date"; render() }
            }.show()
        }
        row.addView(filter, LinearLayout.LayoutParams(0, -2, 1f)); controls.addView(row)
    }
    override suspend fun load() {
        rows = repository.reservations()
        val grids = repository.grids(); slots = repository.slots(grids)
        counts = repository.api.getReservationSummary().data()
        render()
    }
    private fun render() {
        val filtered = rows.filter { row ->
            (when (selected) { 0 -> row.status == "Pending"; 1 -> row.status == "Approved"; 3 -> row.status !in setOf("Pending", "Approved"); else -> true }) &&
                (date.isBlank() || OperatorRules.timestamp(row.startTime)?.let { SimpleDateFormat("yyyy-MM-dd", Locale.ROOT).format(java.util.Date(it)) } == date) &&
                (query.isBlank() || "${row.id} RES-${row.id.takeLast(8)} ${row.prosumerId} ${row.prosumerName} ${row.microgridName} ${row.microgridNodeId} ${row.status}".contains(query, true))
        }.sortedByDescending { it.createdAt }
        summary.text = "${counts?.pendingCount ?: rows.count { it.status == "Pending" }} pending · ${counts?.approvedFutureCount ?: rows.count { it.status == "Approved" }} upcoming · ${"%.1f".format(counts?.totalEnergyReserved ?: 0.0)} kWh reserved\n${counts?.completedThisMonthCount ?: 0} completed this month · ${filtered.size} shown"
        showCards(filtered.map { row ->
            val actions = mutableListOf(OperatorAction("Booking details") { details(row) })
            if (row.status == "Pending") {
                actions.add(0, OperatorAction("Approve", true) { approve(row) }); actions.add(OperatorAction("Reject") { reject(row) })
            }
            if (row.status == "Approved") actions.add(0, OperatorAction("Process transaction", true) { perform("") { ReservationTransactionFlow.open(requireContext(), row, createIfMissing = true) } })
            if (row.status in setOf("Approved", "Completed")) actions.add(OperatorAction("Transaction / QR") { perform("") { ReservationTransactionFlow.open(requireContext(), row, preferQr = true) } })
            OperatorCard(row.prosumerName?.takeIf { it.isNotBlank() } ?: row.prosumerId, "NIC ${row.prosumerId} · RES-${row.id.takeLast(8).uppercase()}", row.status,
                listOf(row.microgridName ?: row.microgridNodeId, "${row.energyAmount} kWh · Estimated LKR ${"%.2f".format(row.totalEstimatedCost)}", "${TransactionUiFormatters.dateTime(row.startTime)} to ${TransactionUiFormatters.dateTime(row.endTime)}"), actions = actions, imageRes = com.smartmicrogrid.R.drawable.bg_energy_home)
        })
    }
    private fun approve(row: Reservation) {
        AlertDialog.Builder(requireContext()).setTitle("Approve reservation?").setMessage("${row.prosumerName ?: row.prosumerId}\n${row.energyAmount} kWh at ${row.microgridName ?: row.microgridNodeId}")
            .setNegativeButton("Keep pending", null).setPositiveButton("Approve") { _, _ -> perform("Booking approved") { repository.api.approveReservation(row.id).checked() } }.show()
    }
    private fun reject(row: Reservation) {
        val input = EditText(requireContext()).apply { hint = "Reason (optional)" }
        AlertDialog.Builder(requireContext()).setTitle("Reject reservation?").setView(input).setNegativeButton("Keep pending", null)
            .setPositiveButton("Reject") { _, _ -> perform("Booking rejected") { repository.api.rejectReservation(row.id, mapOf("reason" to input.text.toString().trim())).checked() } }.show()
    }
    private fun details(row: Reservation) {
        val history = row.statusHistory.joinToString("\n") { "${TransactionUiFormatters.dateTime(it.changedAt)} · ${it.to}${it.reason?.takeIf(String::isNotBlank)?.let { reason -> ": $reason" }.orEmpty()}" }
        AlertDialog.Builder(requireContext()).setTitle("RES-${row.id.takeLast(8).uppercase()}")
            .setMessage("${row.prosumerName ?: "Prosumer"} · ${row.prosumerId}\n${row.microgridName ?: row.microgridNodeId}\n${row.energyAmount} kWh · LKR ${"%.2f".format(row.totalEstimatedCost)}\n${row.status}\n${TransactionUiFormatters.dateTime(row.startTime)} to ${TransactionUiFormatters.dateTime(row.endTime)}\n\nStatus history\n${history.ifBlank { "No status changes recorded." }}")
            .setPositiveButton("Close", null).show()
    }
    override fun onSaveInstanceState(outState: Bundle) { super.onSaveInstanceState(outState); outState.putInt("tab", selected); outState.putString("date", date) }
}
