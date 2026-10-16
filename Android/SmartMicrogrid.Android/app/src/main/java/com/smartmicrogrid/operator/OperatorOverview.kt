package com.smartmicrogrid.operator

import com.smartmicrogrid.models.EnergySlot
import com.smartmicrogrid.models.Microgrid
import kotlin.math.roundToInt

/** Mirrors dashboard.html + js/overview.js for an operator's assigned fleet. */
data class OperatorOverview(
    val grids: List<Microgrid>,
    val active: Int,
    val inactive: Int,
    val maintenance: Int,
    val offline: Int,
    val capacity: Double,
    val available: Double,
    val reserved: Double,
    val used: Double,
    val batteryCapacity: Double,
    val batteryStored: Double,
    val batteryPercentage: Double,
    val activeSlots: Int,
    val marketEnergy: Double
) {
    val headroom: Double get() = percent(available, capacity)
    val readiness: Int? get() = if (grids.isEmpty()) null else (headroom * .4 + batteryPercentage * .35 + percent(active.toDouble(), grids.size.toDouble()) * .25).roundToInt()
    val alerts: List<OverviewAlert> get() = buildList {
        if (grids.isEmpty()) {
            add(OverviewAlert("No managed microgrids", "Register a microgrid or contact an administrator to be assigned a solar hub.", false))
            return@buildList
        }
        if (offline > 0) add(OverviewAlert("$offline microgrid${if (offline == 1) "" else "s"} offline", "Inspect connectivity and restore service.", true))
        if (maintenance > 0) add(OverviewAlert("$maintenance node${if (maintenance == 1) "" else "s"} in maintenance", "Confirm expected completion before new allocations.", true))
        if (batteryPercentage < 30) add(OverviewAlert("Battery resilience is low", "Average battery level is ${batteryPercentage.roundToInt()}%.", true))
        if (headroom < 20) add(OverviewAlert("Limited capacity headroom", "Only ${format(available)} kWh is unallocated.", true))
        if (isEmpty()) add(OverviewAlert("All core signals are healthy", "Capacity, storage, and node availability are within target ranges.", false))
    }

    companion object {
        fun percent(part: Double, total: Double) = if (total > 0) (part / total * 100).coerceIn(0.0, 100.0) else 0.0
        fun format(value: Double): String = java.text.NumberFormat.getNumberInstance().apply { maximumFractionDigits = 1 }.format(value)
        fun from(allGrids: List<Microgrid>, allSlots: List<EnergySlot>, operatorId: String): OperatorOverview {
            val grids = allGrids.filter { operatorId.isNotBlank() && it.operatorId == operatorId }
            val ids = grids.map { it.id }.toSet()
            // Dashboard inventory follows the web status definition. Booking separately checks expiry.
            val slots = allSlots.filter { it.microgridNodeId in ids && it.status in setOf("Available", "PartiallyReserved") }
            val storage = grids.sumOf { it.batteryCapacity }
            val stored = grids.sumOf { it.currentBatteryLevel }
            val average = if (grids.isEmpty()) 0.0 else grids.map { it.batteryPercentage }.average()
            return OperatorOverview(grids,
                grids.count { it.status == "Active" }, grids.count { it.status == "Inactive" },
                grids.count { it.status == "Maintenance" }, grids.count { it.status == "Offline" },
                grids.sumOf { it.capacity }, grids.sumOf { it.availableCapacity }, grids.sumOf { it.reservedCapacity }, grids.sumOf { it.usedCapacity },
                storage, stored, if (average != 0.0) average else percent(stored, storage), slots.size, slots.sumOf { it.availableAmount })
        }
    }
}

data class OverviewAlert(val title: String, val description: String, val warning: Boolean)
