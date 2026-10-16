package com.smartmicrogrid.operator

import com.smartmicrogrid.models.EnergySlot
import com.smartmicrogrid.models.Microgrid
import java.text.SimpleDateFormat
import java.util.Locale
import java.util.TimeZone

/** The same status boundaries used by the web operator workspace. */
object OperatorRules {
    val attentionStatuses = setOf("Pending", "QRGenerated", "VerificationPending", "Verified", "EnergyTransferInProgress")
    val closedStatuses = setOf("Completed", "Rejected", "Cancelled")
    fun canManage(role: String) = role == "MicrogridOperator"
    fun matchesTransaction(status: String, view: Int) = when (view) {
        0 -> status in attentionStatuses
        2 -> status in closedStatuses
        else -> true
    }
    fun nextAction(status: String) = when (status) {
        "Pending" -> "Generate QR"
        "QRGenerated", "VerificationPending" -> "Scan & verify"
        "Verified", "EnergyTransferInProgress" -> "Confirm transfer"
        else -> "View record"
    }
    fun validNic(value: String) = value.matches(Regex("(?:[0-9]{9}[VvXx]|[0-9]{12})"))
    fun timestamp(value: String): Long? {
        val normalized = value.replace(Regex("\\.(\\d{3})\\d+"), ".$1")
        return listOf("yyyy-MM-dd'T'HH:mm:ss.SSSXXX", "yyyy-MM-dd'T'HH:mm:ssXXX").firstNotNullOfOrNull { pattern ->
            runCatching { SimpleDateFormat(pattern, Locale.US).apply { isLenient = false; timeZone = TimeZone.getTimeZone("UTC") }.parse(normalized)?.time }.getOrNull()
        }
    }
    fun isLive(slot: EnergySlot, now: Long = System.currentTimeMillis()) =
        slot.status in setOf("Available", "PartiallyReserved") && slot.availableAmount > 0 && (timestamp(slot.endTime) ?: 0) > now
    fun slotError(amount: Double?, price: Double?, start: Long?, end: Long?, grid: Microgrid?, now: Long = System.currentTimeMillis()): String? = when {
        grid == null || grid.status != "Active" || !grid.isActive -> "Choose an active microgrid."
        amount == null || !amount.isFinite() || amount < 0.01 || amount > 100000 -> "Enter an energy amount between 0.01 and 100,000 kWh."
        amount > grid.capacity -> "The amount exceeds this microgrid's generation capacity."
        price == null || !price.isFinite() || price < 0 || price > 10000 -> "Enter a price between LKR 0 and 10,000 per kWh."
        start == null || start <= now -> "Choose a future start time."
        end == null || end <= start -> "End time must be later than start time."
        else -> null
    }
}
