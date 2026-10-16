package com.smartmicrogrid.models

data class OperatorMicrogridRequest(
    val name: String,
    val location: String,
    val description: String,
    val latitude: Double,
    val longitude: Double,
    val capacity: Double,
    val batteryCapacity: Double,
    val batteryStorageSlots: Int,
    val currentBatteryLevel: Double,
    val status: String,
    val isActive: Boolean
)

data class OperatorLocation(val lat: Double, val lng: Double, val formattedAddress: String, val isApproximate: Boolean)

data class OperatorBatteryReading(
    val microgridId: String,
    val batteryCapacity: Double,
    val currentBatteryLevel: Double,
    val batteryPercentage: Double,
    val status: String
)

data class ReservationStatusEvent(
    val from: String? = null,
    val to: String = "",
    val changedBy: String = "",
    val changedAt: String = "",
    val reason: String? = null
)
