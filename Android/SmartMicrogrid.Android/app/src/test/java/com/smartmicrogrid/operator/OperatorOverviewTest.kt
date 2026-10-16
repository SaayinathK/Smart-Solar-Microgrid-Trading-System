package com.smartmicrogrid.operator

import com.smartmicrogrid.models.EnergySlot
import com.smartmicrogrid.models.Microgrid
import org.junit.Assert.*
import org.junit.Test

/** Dashboard definitions from Web/js/overview.js, including its inventory/booking distinction. */
class OperatorOverviewTest {
    private val owner = "operator-1"
    private fun grid(id: String, status: String = "Active") = Microgrid(
        id = id, operatorId = owner, status = status, capacity = 100.0,
        availableCapacity = 50.0, reservedCapacity = 30.0, usedCapacity = 20.0,
        batteryCapacity = 100.0, currentBatteryLevel = 60.0, batteryPercentage = 60.0)

    @Test fun dashboardAggregatesOnlyAssignedNodesAndTheirSlots() {
        val grids = listOf(grid("a"), grid("b", "Inactive"), grid("c", "Maintenance"), grid("d", "Offline"),
            grid("foreign").copy(operatorId = "operator-2", capacity = 9999.0))
        val slots = listOf(EnergySlot(microgridNodeId = "a", availableAmount = 20.0),
            EnergySlot(microgridNodeId = "foreign", availableAmount = 9000.0))
        val result = OperatorOverview.from(grids, slots, owner)
        assertEquals(4, result.grids.size)
        assertEquals(listOf(1, 1, 1, 1), listOf(result.active, result.inactive, result.maintenance, result.offline))
        assertEquals(400.0, result.capacity, 0.0)
        assertEquals(200.0, result.available, 0.0)
        assertEquals(120.0, result.reserved, 0.0)
        assertEquals(80.0, result.used, 0.0)
        assertEquals(240.0, result.batteryStored, 0.0)
        assertEquals(1, result.activeSlots)
        assertEquals(20.0, result.marketEnergy, 0.0)
        assertEquals(47, result.readiness) // round(50*.4 + 60*.35 + 25*.25)
    }

    @Test fun inventoryMatchesWebStatusesEvenWhenDeliveryHasEnded() {
        val slots = listOf(
            EnergySlot(microgridNodeId = "a", status = "Available", availableAmount = 10.0, endTime = "2020-01-01T00:00:00Z"),
            EnergySlot(microgridNodeId = "a", status = "PartiallyReserved", availableAmount = 5.0),
            EnergySlot(microgridNodeId = "a", status = "Available", availableAmount = 0.0),
            EnergySlot(microgridNodeId = "a", status = "FullyReserved", availableAmount = 40.0),
            EnergySlot(microgridNodeId = "a", status = "Cancelled", availableAmount = 50.0),
            EnergySlot(microgridNodeId = "a", status = "Expired", availableAmount = 60.0))
        val result = OperatorOverview.from(listOf(grid("a")), slots, owner)
        assertEquals(3, result.activeSlots)
        assertEquals(15.0, result.marketEnergy, 0.0)
        assertFalse(OperatorRules.isLive(slots.first()))
    }

    @Test fun batteryReadinessUsesTheWebAverageBeforeWeightedStorage() {
        val grids = listOf(grid("a").copy(batteryCapacity = 100.0, currentBatteryLevel = 20.0, batteryPercentage = 20.0),
            grid("b").copy(batteryCapacity = 900.0, currentBatteryLevel = 720.0, batteryPercentage = 80.0))
        assertEquals(50.0, OperatorOverview.from(grids, emptyList(), owner).batteryPercentage, 0.0)
        assertEquals(74.0, OperatorOverview.from(grids.map { it.copy(batteryPercentage = 0.0) }, emptyList(), owner).batteryPercentage, 0.0)
    }

    @Test fun emptyOrMissingOperatorNeverShowsAnotherOperatorsMetrics() {
        for (id in listOf("", "unassigned")) {
            val result = OperatorOverview.from(listOf(grid("a")), listOf(EnergySlot(microgridNodeId = "a")), id)
            assertTrue(result.grids.isEmpty())
            assertNull(result.readiness)
            assertEquals(0.0, result.headroom, 0.0)
            assertEquals(0, result.activeSlots)
            assertEquals(listOf("No managed microgrids"), result.alerts.map { it.title })
            assertFalse(result.alerts.single().warning)
        }
    }

    @Test fun alertThresholdsMatchWebAndPercentagesStayBounded() {
        val healthy = grid("a").copy(availableCapacity = 20.0, batteryPercentage = 30.0)
        assertEquals(listOf("All core signals are healthy"), OperatorOverview.from(listOf(healthy), emptyList(), owner).alerts.map { it.title })
        val low = healthy.copy(status = "Offline", availableCapacity = 19.99, batteryPercentage = 29.99)
        val result = OperatorOverview.from(listOf(low, low.copy(id = "b", status = "Maintenance")), emptyList(), owner)
        assertEquals(listOf("1 microgrid offline", "1 node in maintenance", "Battery resilience is low", "Limited capacity headroom"), result.alerts.map { it.title })
        assertTrue(result.alerts.all { it.warning })
        assertEquals(100.0, OperatorOverview.percent(150.0, 100.0), 0.0)
        assertEquals(0.0, OperatorOverview.percent(-1.0, 100.0), 0.0)
        assertEquals(0.0, OperatorOverview.percent(1.0, 0.0), 0.0)
    }
}
