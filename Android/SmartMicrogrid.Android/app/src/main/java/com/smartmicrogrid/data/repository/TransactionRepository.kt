package com.smartmicrogrid.data.repository

import com.smartmicrogrid.data.remote.ApiService
import com.smartmicrogrid.models.CompleteTransactionRequest
import com.smartmicrogrid.models.CreateTransactionRequest
import com.smartmicrogrid.models.GenerateQrResponse
import com.smartmicrogrid.models.Transaction
import com.smartmicrogrid.models.VerifyTransactionRequest
import retrofit2.Response

class TransactionRepository(
    private val apiService: ApiService
) {

    suspend fun getTransactions(): Response<com.smartmicrogrid.models.ApiResponse<List<Transaction>>> {
        return apiService.getTransactions()
    }

    suspend fun getTransactionById(
        id: String
    ): Response<com.smartmicrogrid.models.ApiResponse<Transaction>> {
        return apiService.getTransactionById(id)
    }

    suspend fun createTransaction(
        reservationId: String
    ): Response<com.smartmicrogrid.models.ApiResponse<Transaction>> {
        val request = CreateTransactionRequest(
            reservationId = reservationId
        )

        return apiService.createTransaction(request)
    }

    suspend fun generateTransactionQr(
        transactionId: String
    ): Response<com.smartmicrogrid.models.ApiResponse<GenerateQrResponse>> {
        return apiService.generateTransactionQr(transactionId)
    }

    suspend fun verifyTransaction(
        transactionId: String,
        qrCodeData: String
    ): Response<com.smartmicrogrid.models.ApiResponse<Transaction>> {
        val request = VerifyTransactionRequest(
            qrCodeData = qrCodeData
        )

        return apiService.verifyTransaction(
            transactionId,
            request
        )
    }

    suspend fun completeTransaction(
        transactionId: String
    ): Response<com.smartmicrogrid.models.ApiResponse<Transaction>> {
        val request = CompleteTransactionRequest(
            confirmation = "CONFIRMED"
        )

        return apiService.completeTransaction(
            transactionId,
            request
        )
    }

    suspend fun updateTransactionStatus(
        transactionId: String,
        status: String
    ): Response<com.smartmicrogrid.models.ApiResponse<Transaction>> {
        return apiService.updateTransactionStatus(
            transactionId,
            status
        )
    }
}