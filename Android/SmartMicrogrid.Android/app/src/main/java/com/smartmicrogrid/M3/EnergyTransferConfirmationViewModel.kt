package com.smartmicrogrid.M3

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.TransactionRepository
import com.smartmicrogrid.models.Transaction
import kotlinx.coroutines.launch
import java.io.IOException

class EnergyTransferConfirmationViewModel(application: Application) : AndroidViewModel(application) {

    private val repository = TransactionRepository(RetrofitClient.apiService)

    private val _transaction = MutableLiveData<Transaction?>()
    val transaction: LiveData<Transaction?> = _transaction

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
                    response.code() == 401 -> _loadError.value = "Your session has expired. Please log in again."
                    response.code() == 403 -> _loadError.value = "You are not authorized to view this transaction."
                    response.code() == 404 -> _loadError.value = "Transaction was not found."
                    response.isSuccessful -> {
                        val body = response.body()
                        if (body?.success == true && body.data != null) {
                            _transaction.value = body.data
                        } else {
                            _loadError.value = body?.message ?: "Unable to load transaction."
                        }
                    }
                    response.code() >= 500 -> _loadError.value =
                        "The transaction service is unavailable (${response.code()})."
                    else -> _loadError.value = "Unable to load transaction (${response.code()})."
                }
            } catch (_: IOException) {
                _loadError.value = "Unable to connect to the transaction service."
            } catch (exception: Exception) {
                _loadError.value = exception.message ?: "Unable to load transaction."
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun confirmEnergyTransfer(transactionId: String) {
        if (_transaction.value?.status.equals("Verified", ignoreCase = true).not()) {
            _completionError.value = "Only Verified transactions can be completed."
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
                        "Your session has expired. Please log in again."
                    response.code() == 403 -> _completionError.value =
                        "You are not authorized to complete this transaction."
                    response.code() == 404 -> _completionError.value =
                        "Transaction was not found."
                    response.code() == 409 -> _completionError.value =
                        "This transaction cannot be completed in its current state. Refresh and try again."
                    response.isSuccessful -> {
                        val body = response.body()
                        if (body?.success == true && body.data != null) {
                            _transaction.value = body.data
                            if (body.data.status.equals("Completed", ignoreCase = true)) {
                                _completionResult.value = body.data
                            } else {
                                _completionError.value =
                                    "The backend returned status ${body.data.status}; the transaction is not Completed."
                            }
                        } else {
                            _completionError.value = body?.message
                                ?: "The transaction could not be completed."
                        }
                    }
                    response.code() >= 500 -> _completionError.value =
                        "The transaction service failed (${response.code()}). Please try again."
                    else -> _completionError.value = "Unable to complete transaction (${response.code()})."
                }
            } catch (_: IOException) {
                _completionError.value = "Unable to connect to the transaction service. Please try again."
            } catch (exception: Exception) {
                _completionError.value = exception.message ?: "Unable to complete transaction."
            } finally {
                _isCompleting.value = false
            }
        }
    }
}
