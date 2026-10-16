package com.smartmicrogrid.prosumer

import com.smartmicrogrid.models.*
import com.smartmicrogrid.operator.OperatorRules
import org.junit.Assert.*
import org.junit.Test

class ProsumerRulesTest {
    private val now = OperatorRules.timestamp("2026-10-16T12:00:00Z")!!
    private val offer = EnergySlot(id = "offer-1", availableAmount = 15.0, energyAmount = 20.0,
        pricePerUnit = 25.0, startTime = "2026-10-17T12:00:00Z", endTime = "2026-10-17T15:00:00Z")

    @Test fun onlyAnAccountsOwnIdentifiersMatchSavedBookings() {
        val user = User(id = "my-id", nic = "901234567V", role = "Prosumer")
        assertTrue(ProsumerRules.owns(user.nic, user))
        assertTrue(ProsumerRules.owns(user.nic.lowercase(), user))
        assertTrue(ProsumerRules.owns(user.id, user))
        assertFalse(ProsumerRules.owns("other-id", user))
        assertFalse(ProsumerRules.owns("199012345678", user))
        assertFalse(ProsumerRules.owns("", User()))
        assertFalse(ProsumerRules.owns("my-id", null))
    }
    @Test fun changesAndCancellationsHonorTheTwelveHourBoundary() {
        val row = Reservation(status = "Pending", startTime = "2026-10-17T00:00:00Z")
        assertTrue(ProsumerRules.canChange(row, now))
        assertTrue(ProsumerRules.canChange(row.copy(status = "Approved"), now))
        assertFalse(ProsumerRules.canChange(row, now + 1))
        for (status in listOf("Cancelled", "Rejected", "Completed", "Expired", "Unknown")) assertFalse(status, ProsumerRules.canChange(row.copy(status = status), now))
        assertFalse(ProsumerRules.canChange(row.copy(startTime = "bad date"), now))
    }
    @Test fun bookingMustStartInTheFutureAndWithinSevenDays() {
        assertNull(ProsumerRules.bookingIssue(offer, now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(startTime = "2026-10-16T12:00:00Z"), now))
        assertNull(ProsumerRules.bookingIssue(offer.copy(startTime = "2026-10-23T12:00:00Z", endTime = "2026-10-23T15:00:00Z"), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(startTime = "2026-10-23T12:00:01Z", endTime = "2026-10-23T15:00:00Z"), now))
    }
    @Test fun unavailableOrMalformedSupplyCannotBeBooked() {
        assertNull(ProsumerRules.bookingIssue(offer.copy(status = "PartiallyReserved"), now))
        for (status in listOf("Cancelled", "Expired", "FullyReserved", "Unknown")) assertNotNull(status, ProsumerRules.bookingIssue(offer.copy(status = status), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(availableAmount = .09), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(availableAmount = Double.NaN), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(pricePerUnit = Double.POSITIVE_INFINITY), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(pricePerUnit = -1.0), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(startTime = "invalid"), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(endTime = offer.startTime), now))
        assertNotNull(ProsumerRules.bookingIssue(offer.copy(id = ""), now))
        assertNull(ProsumerRules.bookingIssue(offer.copy(pricePerUnit = 0.0), now))
    }
    @Test fun quantityValidationRejectsNonfiniteAndOversubscribedAmounts() {
        assertNull(ProsumerRules.amountIssue(.1, 15.0))
        assertNull(ProsumerRules.amountIssue(15.0, 15.0))
        for (value in listOf(null, -1.0, 0.0, .09, 15.01, Double.NaN, Double.POSITIVE_INFINITY)) assertNotNull(ProsumerRules.amountIssue(value, 15.0))
        assertNull(ProsumerRules.amountIssue(.01, 1.0, .000001)) // Existing bookings can be reduced below the new-booking minimum.
    }
    @Test fun bookingTabsKeepActiveAndClosedRecordsDistinct() {
        for (status in listOf("Pending", "Approved")) {
            assertTrue(ProsumerRules.matchesBooking(Reservation(status = status), 0))
            assertFalse(ProsumerRules.matchesBooking(Reservation(status = status), 3))
        }
        for (status in listOf("Completed", "Rejected", "Cancelled", "Expired")) {
            assertFalse(ProsumerRules.matchesBooking(Reservation(status = status), 0))
            assertTrue(ProsumerRules.matchesBooking(Reservation(status = status), 3))
        }
        assertTrue(ProsumerRules.matchesBooking(Reservation(status = "Approved"), 1))
        assertFalse(ProsumerRules.matchesBooking(Reservation(status = "Pending"), 1))
    }
    @Test fun monthlyCompletionUsesTheServersUtcUpdatedAtBoundary() {
        val rows = listOf(Reservation(status = "Completed", updatedAt = "2026-09-30T23:59:59Z"),
            Reservation(status = "Completed", updatedAt = "2026-10-01T00:00:00Z"),
            Reservation(status = "Completed", updatedAt = "2026-10-16T00:00:00Z"),
            Reservation(status = "Pending", updatedAt = "2026-10-16T00:00:00Z"))
        assertEquals(2, ProsumerRules.completedThisMonth(rows, now))
    }
}
