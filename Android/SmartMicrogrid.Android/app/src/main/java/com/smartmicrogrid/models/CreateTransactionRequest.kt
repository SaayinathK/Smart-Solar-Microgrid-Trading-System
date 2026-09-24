package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class CreateTransactionRequest(
    @SerializedName("reservationId")
    val reservationId: String
)