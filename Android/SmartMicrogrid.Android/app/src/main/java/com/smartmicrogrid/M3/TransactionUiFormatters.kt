package com.smartmicrogrid.M3

import java.text.SimpleDateFormat
import java.util.Locale
import java.util.TimeZone

/** Formatting helpers for M3 presentation only; API values remain unchanged. */
internal object TransactionUiFormatters {
    private val fractionalSeconds = Regex("\\.(\\d{3})\\d+")
    private val hasZone = Regex("(Z|[+-]\\d{2}:?\\d{2})$", RegexOption.IGNORE_CASE)

    fun dateTime(value: String?): String {
        if (value.isNullOrBlank()) return "Not available"
        val trimmed = value.trim()
        if (trimmed.matches(Regex("\\d{4}-\\d{2}-\\d{2}"))) {
            return parse(trimmed, listOf("yyyy-MM-dd"))?.let {
                SimpleDateFormat("d MMM yyyy", Locale.getDefault()).apply {
                    timeZone = TimeZone.getTimeZone("UTC")
                }.format(it)
            } ?: value
        }
        if (trimmed.matches(Regex("\\d{1,2}:\\d{2}(:\\d{2})?"))) {
            val pattern = if (trimmed.count { it == ':' } == 2) "H:mm:ss" else "H:mm"
            return parse(trimmed, listOf(pattern))?.let {
                SimpleDateFormat("h:mm a", Locale.getDefault()).apply {
                    timeZone = TimeZone.getTimeZone("UTC")
                }.format(it)
            } ?: value
        }
        val normalized = fractionalSeconds.replace(value.trim()) { ".${it.groupValues[1]}" }
            .replace(Regex("([+-]\\d{2})(\\d{2})$"), "\$1:\$2")
        val input = if (hasZone.containsMatchIn(normalized)) normalized else "${normalized}Z"
        val patterns = listOf(
            "yyyy-MM-dd'T'HH:mm:ss.SSSXXX",
            "yyyy-MM-dd'T'HH:mm:ssXXX",
            "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'",
            "yyyy-MM-dd'T'HH:mm:ss'Z'"
        )
        val parsed = parse(input, patterns) ?: return value

        return SimpleDateFormat("d MMM yyyy, h:mm a", Locale.getDefault()).format(parsed)
    }

    private fun parse(value: String, patterns: List<String>) = patterns.firstNotNullOfOrNull { pattern ->
        runCatching {
            SimpleDateFormat(pattern, Locale.US).apply {
                isLenient = false
                timeZone = TimeZone.getTimeZone("UTC")
            }.parse(value)
        }.getOrNull()
    }

    fun statusLabel(status: String?): String = when (status?.lowercase(Locale.ROOT)) {
        "pending" -> "Pending"
        "qrgenerated" -> "QR Ready"
        "verificationpending" -> "Awaiting Verification"
        "verified" -> "Verified"
        "energytransferinprogress" -> "Energy Transfer"
        "completed" -> "Completed"
        "rejected" -> "Rejected"
        "cancelled" -> "Cancelled"
        else -> status?.takeIf(String::isNotBlank) ?: "Not available"
    }
}
