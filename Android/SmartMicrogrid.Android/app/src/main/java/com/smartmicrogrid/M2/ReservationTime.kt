package com.smartmicrogrid.M2

import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

internal object ReservationTime {
    private val formatter = DateTimeFormatter.ofPattern("MMM d, yyyy · h:mm a", Locale.getDefault())
    private val dateFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd", Locale.ROOT)

    fun display(utcValue: String): String = try {
        Instant.parse(utcValue).atZone(ZoneId.systemDefault()).format(formatter)
    } catch (_: Exception) { utcValue.replace('T', ' ').take(16) }

    fun localDate(utcValue: String): String = try {
        Instant.parse(utcValue).atZone(ZoneId.systemDefault()).format(dateFormatter)
    } catch (_: Exception) { utcValue.take(10) }
}
