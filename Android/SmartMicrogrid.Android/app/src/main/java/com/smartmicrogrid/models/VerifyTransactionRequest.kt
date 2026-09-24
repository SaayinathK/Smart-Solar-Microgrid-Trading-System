package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class VerifyTransactionRequest(
    @SerializedName("qrCodeData")
    val qrCodeData: String
)