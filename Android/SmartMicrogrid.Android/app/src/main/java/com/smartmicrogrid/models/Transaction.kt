package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class Transaction(
    @SerializedName("id")
    val id: String = "",

    @SerializedName("reservationId")
    val reservationId: String = "",

    @SerializedName("prosumerId")
    val prosumerId: String = "",

    @SerializedName("microgridNodeId")
    val microgridNodeId: String = "",

    @SerializedName("energySlotId")
    val energySlotId: String = "",

    @SerializedName("energyAmount")
    val energyAmount: Double = 0.0,

    @SerializedName("transactionCode")
    val transactionCode: String = "",

    @SerializedName("qrCodeData")
    val qrCodeData: String = "",

    @SerializedName("verifiedBy")
    val verifiedBy: String? = null,

    @SerializedName("verificationTime")
    val verificationTime: String? = null,

    @SerializedName("energyTransferTime")
    val energyTransferTime: String? = null,

    @SerializedName("status")
    val status: String = "Pending",

    @SerializedName("createdAt")
    val createdAt: String = "",

    @SerializedName("updatedAt")
    val updatedAt: String = ""
)