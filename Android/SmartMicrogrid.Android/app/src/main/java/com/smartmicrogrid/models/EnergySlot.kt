package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class EnergySlot(
    @SerializedName("id") val id: String = "",
    @SerializedName("microgridNodeId") val microgridNodeId: String = "",
    @SerializedName("microgridName") val microgridName: String = "",
    @SerializedName("location") val location: String = "",
    @SerializedName("energyAmount") val energyAmount: Double = 0.0,
    @SerializedName("availableAmount") val availableAmount: Double = 0.0,
    @SerializedName("startTime") val startTime: String = "",
    @SerializedName("endTime") val endTime: String = "",
    @SerializedName("pricePerUnit") val pricePerUnit: Double = 0.0,
    @SerializedName("status") val status: String = "Available",
    @SerializedName("createdBy") val createdBy: String = "",
    @SerializedName("createdAt") val createdAt: String = "",
    @SerializedName("updatedAt") val updatedAt: String = ""
)

data class CreateEnergySlotRequest(
    @SerializedName("microgridNodeId") val microgridNodeId: String,
    @SerializedName("energyAmount") val energyAmount: Double,
    @SerializedName("availableAmount") val availableAmount: Double,
    @SerializedName("startTime") val startTime: String,
    @SerializedName("endTime") val endTime: String,
    @SerializedName("pricePerUnit") val pricePerUnit: Double,
    @SerializedName("status") val status: String = "Available"
)

data class EnergyAvailabilitySlot(
    @SerializedName("energySlotId") val energySlotId: String = "",
    @SerializedName("microgridNodeId") val microgridNodeId: String = "",
    @SerializedName("microgridName") val microgridName: String = "",
    @SerializedName("location") val location: String = "",
    @SerializedName("energyAmount") val energyAmount: Double = 0.0,
    @SerializedName("availableAmount") val availableAmount: Double = 0.0,
    @SerializedName("startTime") val startTime: String = "",
    @SerializedName("endTime") val endTime: String = "",
    @SerializedName("pricePerUnit") val pricePerUnit: Double = 0.0,
    @SerializedName("status") val status: String = "Available"
)
