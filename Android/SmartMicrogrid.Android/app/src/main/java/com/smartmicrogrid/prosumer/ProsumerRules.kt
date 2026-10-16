package com.smartmicrogrid.prosumer

import com.smartmicrogrid.models.*
import com.smartmicrogrid.operator.OperatorRules

/** The same booking windows and ownership identifiers enforced by the API. */
internal object ProsumerRules {
    const val DAY = 86400000L
    val current = setOf("Pending", "Approved")
    val closedTransfers = setOf("Completed", "Cancelled", "Rejected")
    fun owns(prosumerId: String, user: User?) = user != null &&
        ((user.nic.isNotBlank() && prosumerId.equals(user.nic, true)) || (user.id.isNotBlank() && prosumerId == user.id))
    fun canChange(row: Reservation, now: Long = System.currentTimeMillis()) = row.status in current &&
        (OperatorRules.timestamp(row.startTime) ?: 0) >= now + 12 * 3600000L
    fun bookingIssue(slot: EnergySlot, now: Long = System.currentTimeMillis()): String? = when {
        slot.id.isBlank() -> "Refresh to load this energy offer."
        slot.status !in setOf("Available", "PartiallyReserved") -> "This offer is no longer available."
        !slot.availableAmount.isFinite() || slot.availableAmount < .1 -> "Less than the minimum 0.1 kWh remains."
        !slot.pricePerUnit.isFinite() || slot.pricePerUnit < 0 -> "The price is unavailable. Refresh this offer."
        OperatorRules.timestamp(slot.startTime) == null || OperatorRules.timestamp(slot.endTime) == null -> "The delivery window is unavailable."
        OperatorRules.timestamp(slot.endTime)!! <= OperatorRules.timestamp(slot.startTime)!! -> "The delivery window is invalid."
        OperatorRules.timestamp(slot.startTime)!! <= now -> "Delivery has already started. Choose a future offer."
        OperatorRules.timestamp(slot.startTime)!! > now + 7 * DAY -> "Booking opens seven days before delivery."
        else -> null
    }
    fun amountIssue(amount: Double?, available: Double, minimum: Double = .1) = when {
        amount == null || !amount.isFinite() || amount < minimum || amount > 1000000 -> "Enter an amount from $minimum to 1,000,000 kWh."
        amount > available -> "Only ${com.smartmicrogrid.operator.OperatorOverview.format(available)} kWh is available."
        else -> null
    }
    fun matchesBooking(row: Reservation, tab: Int) = when (tab) {
        0 -> row.status in current
        1 -> row.status == "Approved"
        3 -> row.status !in current
        else -> true
    }
    fun completedThisMonth(rows: List<Reservation>, now: Long = System.currentTimeMillis()): Int {
        val start = java.util.Calendar.getInstance(java.util.TimeZone.getTimeZone("UTC")).apply {
            timeInMillis = now; set(java.util.Calendar.DAY_OF_MONTH, 1); set(java.util.Calendar.HOUR_OF_DAY, 0)
            set(java.util.Calendar.MINUTE, 0); set(java.util.Calendar.SECOND, 0); set(java.util.Calendar.MILLISECOND, 0)
        }.timeInMillis
        return rows.count { it.status == "Completed" && (OperatorRules.timestamp(it.updatedAt) ?: 0) >= start }
    }
    fun step(status: String) = when (status) {
        "Pending" -> "Your operator is preparing the transaction QR."
        "QRGenerated", "VerificationPending" -> "Show your QR to the operator for verification."
        "Verified" -> "QR verified. Your energy transfer is next."
        "EnergyTransferInProgress" -> "Your operator is completing the energy transfer."
        "Completed" -> "Energy transfer completed. Keep this record for reference."
        "Cancelled", "Rejected" -> "This transfer is closed. View the record for details."
        else -> "Open the record to check the latest status."
    }
    fun EnergyAvailabilitySlot.asSlot() = EnergySlot(id = energySlotId, microgridNodeId = microgridNodeId,
        microgridName = microgridName, location = location, energyAmount = energyAmount,
        availableAmount = availableAmount, startTime = startTime, endTime = endTime, pricePerUnit = pricePerUnit, status = status)
}
