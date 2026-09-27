package com.smartmicrogrid.M3

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.TransactionRepository
import com.smartmicrogrid.models.Reservation
import com.smartmicrogrid.models.Transaction
import kotlinx.coroutines.launch
import java.io.IOException

class EnergyTransferConfirmationViewModel(application: Application) : AndroidViewModel(application) {

    private val repository = TransactionRepository(RetrofitClient.apiService)

    private val _transaction = MutableLiveData<Transaction?>()
    val transaction: LiveData<Transaction?> = _transaction

    private val _scheduledTransfer = MutableLiveData<String>()
    val scheduledTransfer: LiveData<String> = _scheduledTransfer

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _loadError = MutableLiveData<String?>()
    val loadError: LiveData<String?> = _loadError

    private val _isCompleting = MutableLiveData<Boolean>()
    val isCompleting: LiveData<Boolean> = _isCompleting

    private val _completionResult = MutableLiveData<Transaction?>()
    val completionResult: LiveData<Transaction?> = _completionResult

    private val _completionError = MutableLiveData<String?>()
    val completionError: LiveData<String?> = _completionError

    fun loadTransaction(transactionId: String) {
        if (transactionId.isBlank()) {
            _loadError.value = "Transaction ID is required."
            return
        }

        viewModelScope.launch {
            _isLoading.value = true
            _loadError.value = null

            try {
                val response = repository.getTransactionById(transactionId)
                when {
                    response.code() == 401 -> _loadError.value = getApplication<Application>()
                        .getString(com.smartmicrogrid.R.string.session_expired_sign_in)
                    response.code() == 403 -> _loadError.value = "You are not authorized to view this transaction."
                    response.code() == 404 -> _loadError.value = "Transaction not found."
                    response.isSuccessful -> {
                        val body = response.body()
                        if (body?.success == true && body.data != null) {
                            _transaction.value = body.data
                            _scheduledTransfer.value = getApplication<Application>()
                                .getString(com.smartmicrogrid.R.string.not_available)
                            if (body.data.reservationId.isNotBlank()) {
                                _scheduledTransfer.value = loadScheduledTransfer(body.data.reservationId)
                            }
                        } else {
                            _loadError.value = "Unable to load transaction. Please try again."
                        }
                    }
                    else -> _loadError.value = "Unable to load transaction. Please try again."
                }
            } catch (_: IOException) {
                _loadError.value = "Unable to connect to the transaction service."
            } catch (_: Exception) {
                _loadError.value = "Unable to load transaction. Please try again."
            } finally {
                _isLoading.value = false
            }
        }
    }

    private suspend fun loadScheduledTransfer(reservationId: String): String {
        return try {
            val response = repository.getReservationById(reservationId)
            val reservation = response.body()?.takeIf { response.isSuccessful && it.success }?.data
            reservation?.let(::formatSchedule) ?: getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.not_available)
        } catch (_: Exception) {
            getApplication<Application>().getString(com.smartmicrogrid.R.string.not_available)
        }
    }

    private fun formatSchedule(reservation: Reservation): String {
        val date = reservation.reservationDate.trim().let(TransactionUiFormatters::dateTime)
        val start = reservation.startTime.trim().let(TransactionUiFormatters::dateTime)
        val end = reservation.endTime.trim().let(TransactionUiFormatters::dateTime)
        val timeRange = when {
            reservation.startTime.isNotBlank() && reservation.endTime.isNotBlank() -> "$start - $end"
            reservation.startTime.isNotBlank() -> start
            reservation.endTime.isNotBlank() -> end
            else -> ""
        }
        return listOfNotNull(
            date.takeUnless { it == "Not available" },
            timeRange.takeIf { it.isNotBlank() }
        ).joinToString(" · ").ifBlank {
            getApplication<Application>().getString(com.smartmicrogrid.R.string.not_available)
        }
    }

    fun confirmEnergyTransfer(transactionId: String, isConfirmed: Boolean) {
        if (_isCompleting.value == true) return
        if (!isConfirmed) {
            _completionError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.transfer_confirmation_required)
            return
        }

        val status = _transaction.value?.status.orEmpty()
        if (status.equals("Completed", ignoreCase = true)) {
            _completionError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.transaction_already_completed)
            return
        }
        if (!status.equals("Verified", ignoreCase = true) &&
            !status.equals("EnergyTransferInProgress", ignoreCase = true)
        ) {
            _completionError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.transaction_cannot_be_completed)
            return
        }

        if (transactionId.isBlank()) {
            _completionError.value = "Transaction ID is required."
            return
        }

        viewModelScope.launch {
            _isCompleting.value = true
            _completionError.value = null
            _completionResult.value = null

            try {
                val response = repository.completeTransaction(transactionId)
                when {
                    response.code() == 401 -> _completionError.value =
                        getApplication<Application>().getString(com.smartmicrogrid.R.string.session_expired_sign_in)
                    response.code() == 403 -> _completionError.value =
                        "You are not authorized to complete this transaction."
                    response.code() == 404 -> _completionError.value =
                        "Transaction not found."
                    response.code() == 409 -> _completionError.value =
                        getApplication<Application>().getString(com.smartmicrogrid.R.string.transaction_cannot_be_completed)
                    response.code() == 502 -> _completionError.value =
                        getApplication<Application>().getString(com.smartmicrogrid.R.string.reservation_verification_unavailable)
                    response.isSuccessful -> {
                        val body = response.body()
                        if (body?.success == true && body.data != null) {
                            _transaction.value = body.data
                            if (body.data.status.equals("Completed", ignoreCase = true)) {
                                _completionResult.value = body.data
                            } else {
                                _completionError.value = getApplication<Application>()
                                    .getString(com.smartmicrogrid.R.string.completion_failed_retry)
                            }
                        } else {
                            _completionError.value = getApplication<Application>()
                                .getString(com.smartmicrogrid.R.string.completion_failed_retry)
                        }
                    }
                    else -> _completionError.value = getApplication<Application>()
                        .getString(com.smartmicrogrid.R.string.completion_failed_retry)
                }
            } catch (_: IOException) {
                _completionError.value = getApplication<Application>()
                    .getString(com.smartmicrogrid.R.string.completion_failed_retry)
            } catch (_: Exception) {
                _completionError.value = getApplication<Application>()
                    .getString(com.smartmicrogrid.R.string.completion_failed_retry)
            } finally {
                _isCompleting.value = false
            }
        }
    }

    fun clearCompletionResult() {
        _completionResult.value = null
    }
}
