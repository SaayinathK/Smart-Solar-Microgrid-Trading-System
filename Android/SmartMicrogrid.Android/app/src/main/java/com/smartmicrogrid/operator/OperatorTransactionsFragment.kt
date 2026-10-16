package com.smartmicrogrid.operator

import android.app.DatePickerDialog
import android.content.Intent
import android.os.Bundle
import android.widget.LinearLayout
import android.widget.Spinner
import android.widget.ArrayAdapter
import android.widget.AdapterView
import com.google.android.material.button.MaterialButton
import android.view.View
import androidx.appcompat.app.AlertDialog
import com.smartmicrogrid.M3.EnergyTransferConfirmationActivity
import com.smartmicrogrid.M3.TransactionDetailsActivity
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.models.Transaction
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale

class OperatorTransactionsFragment : OperatorListFragment() {
    private var selected = 0
    private var date = ""
    private var status = "All statuses"
    private var rows: List<Transaction> = emptyList()
    private var names: Map<String, String> = emptyMap()
    private lateinit var statusSpinner: Spinner
    private lateinit var dateButton: MaterialButton
    override fun configure(state: Bundle?) {
        selected = state?.getInt("tab") ?: arguments?.getInt("tab") ?: 0
        date = state?.getString("date").orEmpty()
        status = state?.getString("status") ?: "All statuses"
        tabs(listOf("Needs attention", "All transactions", "History"), selected) {
            selected = it; status = "All statuses"; date = ""; clearSearch()
            if (::statusSpinner.isInitialized) statusSpinner.setSelection(0)
            if (::dateButton.isInitialized) dateButton.text = "Filter date"
            render()
        }
        search("Search reference, reservation or microgrid") { render() }
        val actions = LinearLayout(requireContext())
        dateButton = requireContext().action(if (date.isBlank()) "Filter date" else date) {}
        fun chooseDate() {
            val now = Calendar.getInstance()
            DatePickerDialog(requireContext(), { _, year, month, day ->
                date = String.format(Locale.ROOT, "%04d-%02d-%02d", year, month + 1, day); dateButton.text = date; render()
            }, now.get(Calendar.YEAR), now.get(Calendar.MONTH), now.get(Calendar.DAY_OF_MONTH)).show()
        }
        dateButton.setOnClickListener {
            if (date.isBlank()) chooseDate()
            else AlertDialog.Builder(requireContext()).setTitle("Date filter").setItems(arrayOf("Change date", "Clear date")) { _, position ->
                if (position == 0) chooseDate() else { date = ""; dateButton.text = "Filter date"; render() }
            }.show()
        }
        val statuses = listOf("All statuses", "Pending", "QRGenerated", "VerificationPending", "Verified", "EnergyTransferInProgress", "Completed", "Rejected", "Cancelled")
        statusSpinner = Spinner(requireContext()).apply {
            adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, statuses.map { if (it == "All statuses") it else TransactionUiFormatters.statusLabel(it) })
            setSelection(statuses.indexOf(status).coerceAtLeast(0))
            onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) {}
                override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) { status = statuses[position]; render() }
            }
        }
        actions.addView(statusSpinner, LinearLayout.LayoutParams(0, requireContext().dp(48), 1f))
        actions.addView(dateButton, LinearLayout.LayoutParams(0, -2, 1f))
        controls.addView(actions)
    }
    override suspend fun load() { rows = repository.transactions(); names = repository.grids().associate { it.id to it.name }; render() }
    private fun render() {
        val filtered = rows.filter { row ->
            OperatorRules.matchesTransaction(row.status, selected) &&
                (status == "All statuses" || row.status == status) &&
                (date.isBlank() || localDate(row.createdAt) == date) &&
                (query.isBlank() || "${row.transactionCode} ${row.id} ${row.reservationId} ${row.prosumerId} ${row.microgridNodeId} ${names[row.microgridNodeId]} ${row.energySlotId} ${row.status}".contains(query, true))
        }.sortedByDescending { it.createdAt }
        summary.text = "${filtered.size} shown · ${rows.count { it.status in OperatorRules.attentionStatuses }} need attention · ${rows.count { it.status == "Completed" }} completed"
        showCards(filtered.map { row ->
            val open = { startActivity(Intent(requireContext(), TransactionDetailsActivity::class.java).putExtra("TRANSACTION_ID", row.id)); Unit }
            OperatorCard(row.transactionCode.ifBlank { "TX-${row.id.takeLast(8).uppercase()}" }, names[row.microgridNodeId] ?: row.microgridNodeId, TransactionUiFormatters.statusLabel(row.status),
                listOf("${row.energyAmount} kWh · Reservation ${row.reservationId.takeLast(8).uppercase()}",
                    "Created ${TransactionUiFormatters.dateTime(row.createdAt)}",
                    "Verified ${TransactionUiFormatters.dateTime(row.verificationTime)} · Transfer ${TransactionUiFormatters.dateTime(row.energyTransferTime)}"),
                actions = if (row.status in OperatorRules.attentionStatuses) listOf(
                    OperatorAction(OperatorRules.nextAction(row.status), true) {
                        if (row.status in setOf("Verified", "EnergyTransferInProgress")) startActivity(Intent(requireContext(), EnergyTransferConfirmationActivity::class.java).putExtra("TRANSACTION_ID", row.id))
                        else startActivity(Intent(requireContext(), TransactionDetailsActivity::class.java).putExtra("TRANSACTION_ID", row.id).putExtra("OPERATOR_ACTION", row.status))
                    }, OperatorAction("Details", run = open)
                ) else listOf(OperatorAction("View record", run = open)), imageRes = com.smartmicrogrid.R.drawable.bg_web_dashboard)
        })
    }
    private fun localDate(value: String) = OperatorRules.timestamp(value)?.let { SimpleDateFormat("yyyy-MM-dd", Locale.ROOT).format(java.util.Date(it)) }.orEmpty()
    override fun onSaveInstanceState(outState: Bundle) { super.onSaveInstanceState(outState); outState.putInt("tab", selected); outState.putString("date", date); outState.putString("status", status) }
}
