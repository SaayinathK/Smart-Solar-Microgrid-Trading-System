package com.smartmicrogrid.M3

/** Shared M3 payload validation for display, camera scanning and verification. */
object TransactionQrPayload {
    data class Value(val transactionId: String, val transactionCode: String)
    private val pattern = Regex("^SMART-MICROGRID\\|TRANSACTION\\|([a-f0-9]{24})\\|([^|\\s]+)$")
    fun parse(payload: String): Value? = pattern.matchEntire(payload)?.let {
        Value(it.groupValues[1], it.groupValues[2])
    }
}
