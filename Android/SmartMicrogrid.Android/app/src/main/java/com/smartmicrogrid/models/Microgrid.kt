package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class Microgrid(
    @SerializedName("id") val id: String = "",
    @SerializedName("name") val name: String = "",
    @SerializedName("location") val location: String = "",
    @SerializedName("description") val description: String? = null,
    @SerializedName("latitude") val latitude: Double = 0.0,
    @SerializedName("longitude") val longitude: Double = 0.0,
    @SerializedName("capacity") val capacity: Double = 0.0,
    @SerializedName("availableCapacity") val availableCapacity: Double = 0.0,
    @SerializedName("reservedCapacity") val reservedCapacity: Double = 0.0,
    @SerializedName("usedCapacity") val usedCapacity: Double = 0.0,
    @SerializedName("batteryCapacity") val batteryCapacity: Double = 0.0,
    @SerializedName("currentBatteryLevel") val currentBatteryLevel: Double = 0.0,
    @SerializedName("batteryPercentage") val batteryPercentage: Double = 0.0,
    @SerializedName("status") val status: String = "Active",
    @SerializedName("isActive") val isActive: Boolean = true,
    @SerializedName("operatorId") val operatorId: String = "",
    @SerializedName("createdAt") val createdAt: String = "",
    @SerializedName("updatedAt") val updatedAt: String = ""
)

data class UpdateBatteryRequest(
    @SerializedName("batteryCapacity") val batteryCapacity: Double,
    @SerializedName("currentBatteryLevel") val currentBatteryLevel: Double
)

data class UpdateCapacityRequest(
    @SerializedName("totalCapacity") val totalCapacity: Double,
    @SerializedName("availableCapacity") val availableCapacity: Double,
    @SerializedName("reservedCapacity") val reservedCapacity: Double,
    @SerializedName("usedCapacity") val usedCapacity: Double
)
