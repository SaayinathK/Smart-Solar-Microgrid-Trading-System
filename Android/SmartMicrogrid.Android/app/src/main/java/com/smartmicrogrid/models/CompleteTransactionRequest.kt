package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

data class CompleteTransactionRequest(
    @SerializedName("confirmation")
    val confirmation: String
)