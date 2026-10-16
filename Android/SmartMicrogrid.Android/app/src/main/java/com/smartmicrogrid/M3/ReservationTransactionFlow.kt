package com.smartmicrogrid.M3

import android.content.Context
import android.content.Intent
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.CreateTransactionRequest
import com.smartmicrogrid.models.Reservation
import com.smartmicrogrid.utils.SessionManager

/** Reservation screens reuse M3 transactions and their server-issued QR payloads. */
object ReservationTransactionFlow {
    suspend fun open(context: Context, reservation: Reservation, preferQr: Boolean = false, createIfMissing: Boolean = false) {
        val api = RetrofitClient.apiService
        suspend fun find() = api.getTransactions().let { response ->
            if (!response.isSuccessful || response.body()?.success != true)
                throw IllegalStateException("Unable to load the transaction. Check your connection and retry.")
            response.body()?.data.orEmpty().firstOrNull { it.reservationId == reservation.id }
        }
        var transaction = find()
        val operator = SessionManager.getUserRole() == "MicrogridOperator"
        if (transaction == null && createIfMissing && operator && reservation.status == "Approved") {
            val response = api.createTransaction(CreateTransactionRequest(reservation.id))
            transaction = if (response.code() == 409) find() else response.body()?.data.takeIf { response.isSuccessful && response.body()?.success == true }
            if (transaction == null) throw IllegalStateException("Unable to start the M3 transaction. Refresh and retry.")
        }
        val current = transaction ?: throw IllegalStateException("The assigned operator has not started a transaction for this reservation yet.")
        val intent = if (preferQr && current.qrCodeData.isNotBlank() &&
            current.status in listOf("QRGenerated", "VerificationPending", "Verified", "EnergyTransferInProgress", "Completed")) {
            Intent(context, TransactionQrActivity::class.java).apply {
                putExtra(TransactionQrActivity.EXTRA_QR_CODE_DATA, current.qrCodeData)
                putExtra(TransactionQrActivity.EXTRA_TRANSACTION_ID, current.id)
                putExtra(TransactionQrActivity.EXTRA_TRANSACTION_CODE, current.transactionCode)
                putExtra(TransactionQrActivity.EXTRA_ENERGY_AMOUNT, current.energyAmount)
                putExtra(TransactionQrActivity.EXTRA_STATUS, current.status)
                putExtra(TransactionQrActivity.EXTRA_MICROGRID_NODE_ID, current.microgridNodeId)
                putExtra(TransactionQrActivity.EXTRA_ENERGY_SLOT_ID, current.energySlotId)
                putExtra(TransactionQrActivity.EXTRA_SCHEDULE_LABEL, "${reservation.startTime} to ${reservation.endTime}")
            }
        } else {
            Intent(context, if (SessionManager.getUserRole() == "Prosumer") ProsumerTransactionDetailsActivity::class.java else TransactionDetailsActivity::class.java)
                .putExtra("TRANSACTION_ID", current.id)
        }
        context.startActivity(intent)
    }
}
