package com.smartmicrogrid.prosumer

import android.view.View
import android.widget.*
import androidx.appcompat.app.AlertDialog
import androidx.core.widget.addTextChangedListener
import androidx.fragment.app.Fragment
import com.smartmicrogrid.R
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.models.*
import com.smartmicrogrid.operator.*
import java.text.SimpleDateFormat
import java.util.*

internal object ProsumerForms {
    fun book(owner: Fragment, slot: EnergySlot, done: () -> Unit) {
        val ctx = owner.requireContext(); val repository = ProsumerRepository()
        val form = OperatorForm(owner, R.drawable.bg_energy_home, "Prosumer")
        form.note("${slot.microgridName}\n${slot.location}\n${TransactionUiFormatters.dateTime(slot.startTime)} to ${TransactionUiFormatters.dateTime(slot.endTime)}")
        form.note("${OperatorOverview.format(slot.availableAmount)} kWh available · LKR ${"%.2f".format(slot.pricePerUnit)} / kWh")
        ProsumerRules.bookingIssue(slot)?.let { form.note(it) }
        val amount = form.field("Energy amount (kWh)", minOf(5.0, slot.availableAmount).toString(), number = true)
        val estimate = ctx.label("", 17f, true).apply { setPadding(0, ctx.dp(10), 0, 0); accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE }
        form.body.addView(estimate)
        fun price() { estimate.text = "Estimated total · LKR ${"%.2f".format((amount.text.toString().toDoubleOrNull()?.takeIf { it.isFinite() && it > 0 } ?: 0.0) * slot.pricePerUnit)}" }
        amount.addTextChangedListener { price() }; price()
        form.note("Your request needs operator approval. Changes and cancellations require at least 12 hours' notice. Delivery must start within seven days.")
        form.show("Reserve clean energy", "Send request", done) {
            repository.ensureAccess()
            val fresh = repository.api.getEnergySlotById(slot.id).data()
            ProsumerRules.bookingIssue(fresh)?.let { error(it) }
            check(kotlin.math.abs(fresh.pricePerUnit - slot.pricePerUnit) < .000001) { "The price has changed. Reopen the offer to review the new total." }
            val value = amount.text.toString().toDoubleOrNull()
            ProsumerRules.amountIssue(value, fresh.availableAmount)?.let { error(it) }
            val saved = repository.api.createReservation(CreateReservationRequest(slot.id, value!!)).data()
            AlertDialog.Builder(ctx).setTitle("Request sent").setMessage("${slot.microgridName}\n${OperatorOverview.format(saved.energyAmount)} kWh · LKR ${"%.2f".format(saved.totalEstimatedCost)}\n${TransactionUiFormatters.dateTime(saved.startTime)}\nStatus: ${saved.status}\n\nTrack approval and your transaction QR in Bookings.").setPositiveButton("Done", null).show()
        }
    }

    fun modify(owner: Fragment, row: Reservation, slots: List<EnergySlot>, done: () -> Unit) {
        val ctx = owner.requireContext(); val repository = ProsumerRepository()
        val form = OperatorForm(owner, R.drawable.bg_energy_home, "Prosumer")
        form.note("${row.microgridName ?: "Microgrid"} · RES-${row.id.takeLast(8).uppercase()}\nCurrent delivery: ${TransactionUiFormatters.dateTime(row.startTime)}")
        val alternatives = slots.filter { it.id != row.energySlotId && it.microgridNodeId == row.microgridNodeId && ProsumerRules.bookingIssue(it) == null }
        val choice = form.choice("Delivery slot", listOf("Keep current slot") + alternatives.map { "${TransactionUiFormatters.dateTime(it.startTime)} · ${it.availableAmount} kWh" })
        val amount = form.field("Energy amount (kWh)", row.energyAmount.toString(), number = true)
        val (_, getDate) = form.date("New delivery time (optional)")
        form.note("Leave the time unchanged to keep the selected slot's delivery time. Times are shown in your local timezone.")
        val estimate = ctx.label("", 16f, true); form.body.addView(estimate)
        fun price() {
            val rate = alternatives.getOrNull(choice.selectedItemPosition - 1)?.pricePerUnit ?: row.pricePerUnit
            estimate.text = "Estimated total · LKR ${"%.2f".format((amount.text.toString().toDoubleOrNull()?.takeIf { it.isFinite() && it > 0 } ?: 0.0) * rate)}"
        }
        amount.addTextChangedListener { price() }
        choice.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onNothingSelected(parent: AdapterView<*>?) {}
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) { price() }
        }; price()
        form.show("Modify booking", "Save changes", done) {
            check(ProsumerRules.canChange(row)) { "Changes require at least 12 hours before delivery." }
            val selected = alternatives.getOrNull(choice.selectedItemPosition - 1)
            val fresh = repository.api.getEnergySlotById(selected?.id ?: row.energySlotId).data()
            val available = fresh.availableAmount + if (selected == null) row.energyAmount else 0.0
            val value = amount.text.toString().toDoubleOrNull()
            ProsumerRules.amountIssue(value, available, .000001)?.let { error(it) }
            val start = getDate() ?: OperatorRules.timestamp(selected?.startTime ?: row.startTime) ?: error("Choose a valid delivery time.")
            check(start > System.currentTimeMillis() && start <= System.currentTimeMillis() + 7 * ProsumerRules.DAY) { "Delivery must start in the future and within seven days." }
            if (selected != null) check(start < (OperatorRules.timestamp(fresh.endTime) ?: 0)) { "Delivery must start before the selected slot ends." }
            val utc = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'", Locale.ROOT).apply { timeZone = TimeZone.getTimeZone("UTC") }.format(Date(start))
            repository.api.updateReservation(row.id, UpdateReservationRequest(selected?.id ?: row.energySlotId, value!!, utc)).checked()
        }
    }

    fun details(owner: Fragment, row: Reservation, offline: Boolean = false) {
        val history = row.statusHistory.joinToString("\n") { "${TransactionUiFormatters.dateTime(it.changedAt)} · ${it.to}${it.reason?.let { reason -> ": $reason" }.orEmpty()}" }
        AlertDialog.Builder(owner.requireContext()).setTitle("RES-${row.id.takeLast(8).uppercase()}")
            .setMessage("${row.microgridName ?: row.microgridNodeId}\n${row.energyAmount} kWh · ${if (offline) "Cost unavailable offline" else "LKR ${"%.2f".format(row.totalEstimatedCost)}"}\n${row.status}\n${TransactionUiFormatters.dateTime(row.startTime)} to ${TransactionUiFormatters.dateTime(row.endTime)}\n\nStatus history\n${if (offline) "History unavailable offline. Refresh for the latest details." else history.ifBlank { "No status changes recorded." }}")
            .setPositiveButton("Close", null).show()
    }
}
