package com.smartmicrogrid.prosumer

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.widget.*
import com.smartmicrogrid.M3.*
import com.smartmicrogrid.models.Transaction
import com.smartmicrogrid.operator.*
import java.text.SimpleDateFormat
import java.util.*

class ProsumerTransfersFragment : ProsumerListFragment() {
    private var tab = 0
    private var status = "All statuses"
    private var date = ""
    private var rows = emptyList<Transaction>()
    override fun configure(state: Bundle?) {
        tab = state?.getInt("tab") ?: arguments?.getInt("tab") ?: 0
        status = state?.getString("status") ?: "All statuses"; date = state?.getString("date").orEmpty()
        tabs(listOf("All transfers", "In progress", "History"), tab) { tab = it; render() }
        search("Search transaction or booking reference") { render() }
        val ctx = requireContext()
        val statuses = listOf("All statuses", "Pending", "QRGenerated", "VerificationPending", "Verified", "EnergyTransferInProgress", "Completed", "Rejected", "Cancelled")
        controls.addView(Spinner(ctx).apply {
            contentDescription = "Transfer status"; adapter = ArrayAdapter(ctx, android.R.layout.simple_spinner_dropdown_item, statuses.map(TransactionUiFormatters::statusLabel)); setSelection(statuses.indexOf(status).coerceAtLeast(0))
            onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) {}
                override fun onItemSelected(parent: AdapterView<*>?, view: android.view.View?, position: Int, id: Long) { status = statuses[position]; render() }
            }
        }, LinearLayout.LayoutParams(-1, ctx.dp(48)))
        controls.addView(dateFilter(ctx, date, "Created date") { date = it; render() })
    }
    override suspend fun load() { rows = prosumer.transactions(); render() }
    private fun render() {
        val filtered = rows.filter { row ->
            (tab == 0 || (row.status in ProsumerRules.closedTransfers) == (tab == 2)) &&
                (status == "All statuses" || row.status == status) &&
                (date.isBlank() || OperatorRules.timestamp(row.createdAt)?.let { SimpleDateFormat("yyyy-MM-dd", Locale.ROOT).format(Date(it)) } == date) &&
                (query.isBlank() || "${row.transactionCode} ${row.id} RES-${row.reservationId.takeLast(8)} ${row.reservationId} ${row.status}".contains(query, true))
        }.sortedByDescending { OperatorRules.timestamp(it.createdAt) ?: 0 }
        summary.text = "${rows.count { it.status !in ProsumerRules.closedTransfers }} in progress · ${rows.count { it.status == "Completed" }} completed\n${filtered.size} shown · Pull to refresh"
        showCards(filtered.map { row ->
            val actions = mutableListOf(OperatorAction("View record") { ProsumerTransferNavigation.details(requireContext(), row) })
            if (row.qrCodeData.isNotBlank()) actions.add(0, OperatorAction("Show QR", true) { perform("") {
                val booking = prosumer.api.getReservationById(row.reservationId).data()
                ProsumerTransferNavigation.qr(requireContext(), row, "${TransactionUiFormatters.dateTime(booking.startTime)} to ${TransactionUiFormatters.dateTime(booking.endTime)}")
            } })
            OperatorCard(row.transactionCode.ifBlank { "TX-${row.id.takeLast(8).uppercase()}" }, "RES-${row.reservationId.takeLast(8).uppercase()}", TransactionUiFormatters.statusLabel(row.status),
                listOf("${OperatorOverview.format(row.energyAmount)} kWh", ProsumerRules.step(row.status), "Updated ${TransactionUiFormatters.dateTime(row.updatedAt)}"),
                actions = actions, imageRes = com.smartmicrogrid.R.drawable.bg_web_dashboard)
        })
    }
    override fun onSaveInstanceState(out: Bundle) { super.onSaveInstanceState(out); out.putInt("tab", tab); out.putString("status", status); out.putString("date", date) }
}

internal object ProsumerTransferNavigation {
    fun details(context: Context, row: Transaction) { context.startActivity(Intent(context, ProsumerTransactionDetailsActivity::class.java).putExtra(ProsumerTransactionDetailsActivity.EXTRA_TRANSACTION_ID, row.id)) }
    fun qr(context: Context, row: Transaction, schedule: String? = null) {
        context.startActivity(Intent(context, TransactionQrActivity::class.java).apply {
            putExtra(TransactionQrActivity.EXTRA_QR_CODE_DATA, row.qrCodeData)
            putExtra(TransactionQrActivity.EXTRA_TRANSACTION_ID, row.id)
            putExtra(TransactionQrActivity.EXTRA_TRANSACTION_CODE, row.transactionCode)
            putExtra(TransactionQrActivity.EXTRA_ENERGY_AMOUNT, row.energyAmount)
            putExtra(TransactionQrActivity.EXTRA_STATUS, row.status)
            putExtra(TransactionQrActivity.EXTRA_MICROGRID_NODE_ID, row.microgridNodeId)
            putExtra(TransactionQrActivity.EXTRA_ENERGY_SLOT_ID, row.energySlotId)
            putExtra(TransactionQrActivity.EXTRA_SCHEDULE_LABEL, schedule)
        })
    }
}
