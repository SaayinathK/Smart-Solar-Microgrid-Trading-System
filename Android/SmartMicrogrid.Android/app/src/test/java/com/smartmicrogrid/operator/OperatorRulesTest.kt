package com.smartmicrogrid.operator

import com.smartmicrogrid.M3.TransactionQrPayload
import com.smartmicrogrid.models.EnergySlot
import com.smartmicrogrid.models.Microgrid
import org.junit.Assert.*
import org.junit.Test

class OperatorRulesTest {
    @Test fun unfinishedTransfersRemainInTheActionQueue() {
        listOf("Pending", "QRGenerated", "VerificationPending", "Verified", "EnergyTransferInProgress").forEach {
            assertTrue(it, OperatorRules.matchesTransaction(it, 0))
            assertFalse(it, OperatorRules.matchesTransaction(it, 2))
            assertTrue(it, OperatorRules.matchesTransaction(it, 1))
        }
    }
    @Test fun historyContainsOnlyClosedTransactions() {
        listOf("Completed", "Rejected", "Cancelled").forEach {
            assertTrue(it, OperatorRules.matchesTransaction(it, 2))
            assertFalse(it, OperatorRules.matchesTransaction(it, 0))
        }
        assertFalse(OperatorRules.matchesTransaction("Unknown", 0))
        assertFalse(OperatorRules.matchesTransaction("Unknown", 2))
    }
    @Test fun completionIsTheNextActionForVerifiedAndInProgressTransfers() {
        assertEquals("Confirm transfer", OperatorRules.nextAction("Verified"))
        assertEquals("Confirm transfer", OperatorRules.nextAction("EnergyTransferInProgress"))
        assertEquals("Scan & verify", OperatorRules.nextAction("QRGenerated"))
        assertEquals("Generate QR", OperatorRules.nextAction("Pending"))
        assertEquals("View record", OperatorRules.nextAction("Completed"))
    }
    @Test fun onlyTheOperatorRoleCanEnterTheOperatorWorkspace() {
        assertTrue(OperatorRules.canManage("MicrogridOperator"))
        listOf("Admin", "Prosumer", "GridOperator", "", "microgridoperator").forEach { assertFalse(it, OperatorRules.canManage(it)) }
    }
    @Test fun acceptsBothSupportedNicFormatsAndRejectsMalformedIdentifiers() {
        listOf("199012345678", "901234567V", "901234567x").forEach { assertTrue(it, OperatorRules.validNic(it)) }
        listOf("901234567", "19901234567", "1990123456789", "90123456VV", "../users", "").forEach { assertFalse(it, OperatorRules.validNic(it)) }
    }
    @Test fun endedCancelledOrEmptySlotsCannotBeBooked() {
        val now = OperatorRules.timestamp("2026-10-16T12:00:00Z")!!
        val live = EnergySlot(availableAmount = 10.0, endTime = "2026-10-16T13:00:00Z")
        assertTrue(OperatorRules.isLive(live, now))
        assertTrue(OperatorRules.isLive(live.copy(status = "PartiallyReserved"), now))
        assertFalse(OperatorRules.isLive(live.copy(status = "FullyReserved"), now))
        assertFalse(OperatorRules.isLive(live.copy(status = "PartiallyReserved", endTime = "2026-10-16T12:00:00Z"), now))
        assertFalse(OperatorRules.isLive(live.copy(endTime = "2026-10-16T12:00:00Z"), now))
        assertFalse(OperatorRules.isLive(live.copy(status = "Cancelled"), now))
        assertFalse(OperatorRules.isLive(live.copy(availableAmount = 0.0), now))
        assertFalse(OperatorRules.isLive(live.copy(endTime = "invalid"), now))
    }
    @Test fun validatesSlotCapacityPriceAndDeliveryWindow() {
        val grid = Microgrid(capacity = 100.0, availableCapacity = 75.0)
        assertNull(OperatorRules.slotError(10.0, 35.0, 2000, 3000, grid, 1000))
        assertNotNull(OperatorRules.slotError(101.0, 35.0, 2000, 3000, grid, 1000))
        assertNotNull(OperatorRules.slotError(Double.NaN, 35.0, 2000, 3000, grid, 1000))
        assertNotNull(OperatorRules.slotError(10.0, -1.0, 2000, 3000, grid, 1000))
        assertNotNull(OperatorRules.slotError(10.0, Double.POSITIVE_INFINITY, 2000, 3000, grid, 1000))
        assertNotNull(OperatorRules.slotError(10.0, 35.0, 1000, 3000, grid, 1000))
        assertNotNull(OperatorRules.slotError(10.0, 35.0, 2000, 2000, grid, 1000))
        assertNotNull(OperatorRules.slotError(10.0, 35.0, 2000, 3000, grid.copy(status = "Maintenance"), 1000))
        assertNotNull(OperatorRules.slotError(10.0, 35.0, 2000, 3000, grid.copy(isActive = false), 1000))
    }
    @Test fun timestampsRespectOffsetsAndDotNetFractionalSeconds() {
        val expected = OperatorRules.timestamp("2026-10-16T12:00:00Z")
        assertNotNull(expected)
        assertEquals(expected, OperatorRules.timestamp("2026-10-16T17:30:00+05:30"))
        assertEquals(expected, OperatorRules.timestamp("2026-10-16T12:00:00.0000000Z"))
        assertNull(OperatorRules.timestamp("not a date"))
    }
    @Test fun scanAcceptsOnlyServerTransactionPayloads() {
        val id = "0123456789abcdef01234567"
        val parsed = TransactionQrPayload.parse("SMART-MICROGRID|TRANSACTION|$id|TX-12345")
        assertEquals(id, parsed?.transactionId)
        assertEquals("TX-12345", parsed?.transactionCode)
        listOf("https://example.com", "RES-123", "SMART-MICROGRID|TRANSACTION|invalid|TX-123", "SMART-MICROGRID|TRANSACTION|$id|TX-123|extra").forEach { assertNull(it, TransactionQrPayload.parse(it)) }
    }
}
