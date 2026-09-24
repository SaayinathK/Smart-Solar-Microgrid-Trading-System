package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class Reservation(
    @SerializedName("id") val id: String = "",
    @SerializedName("prosumerId") val prosumerId: String = "",
    @SerializedName("microgridNodeId") val microgridNodeId: String = "",
    @SerializedName("energySlotId") val energySlotId: String = "",
    @SerializedName("energyAmount") val energyAmount: Double = 0.0,
    @SerializedName("reservationDate") val reservationDate: String = "",
    @SerializedName("startTime") val startTime: String = "",
    @SerializedName("endTime") val endTime: String = "",
    @SerializedName("status") val status: String = "Pending",
    @SerializedName("createdAt") val createdAt: String = "",
    @SerializedName("updatedAt") val updatedAt: String = ""
)

data class CreateReservationRequest(
    @SerializedName("energySlotId") val energySlotId: String,
    @SerializedName("energyAmount") val energyAmount: Double
)

data class ReservationSummary(
    @SerializedName("pendingCount") val pendingCount: Int = 0,
    @SerializedName("approvedFutureCount") val approvedFutureCount: Int = 0,
    @SerializedName("completedThisMonthCount") val completedThisMonthCount: Int = 0,
    @SerializedName("totalEnergyReserved") val totalEnergyReserved: Double = 0.0
)
