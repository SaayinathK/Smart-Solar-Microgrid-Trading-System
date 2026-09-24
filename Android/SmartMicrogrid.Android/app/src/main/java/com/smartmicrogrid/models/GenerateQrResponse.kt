package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class GenerateQrResponse(
    @SerializedName("transactionId")
    val transactionId: String = "",

    @SerializedName("reservationId")
    val reservationId: String = "",

    @SerializedName("transactionCode")
    val transactionCode: String = "",

    @SerializedName("qrCodeData")
    val qrCodeData: String = "",

    @SerializedName("status")
    val status: String = ""
)