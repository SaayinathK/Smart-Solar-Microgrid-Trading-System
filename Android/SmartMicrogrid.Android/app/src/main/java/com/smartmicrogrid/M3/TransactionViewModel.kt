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

class TransactionViewModel(application: Application) : AndroidViewModel(application) {

    private val repository = TransactionRepository(RetrofitClient.apiService)

    private val _transactions = MutableLiveData<List<Transaction>>()
    val transactions: LiveData<List<Transaction>> = _transactions

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String?>()
    val errorMessage: LiveData<String?> = _errorMessage

    private val _approvedReservations = MutableLiveData<List<Reservation>>()
    val approvedReservations: LiveData<List<Reservation>> = _approvedReservations

    private val _isLoadingApprovedReservations = MutableLiveData<Boolean>()
    val isLoadingApprovedReservations: LiveData<Boolean> = _isLoadingApprovedReservations

    private val _approvedReservationsError = MutableLiveData<String?>()
    val approvedReservationsError: LiveData<String?> = _approvedReservationsError

    private val _isCreatingTransaction = MutableLiveData<Boolean>()
    val isCreatingTransaction: LiveData<Boolean> = _isCreatingTransaction

    private val _transactionCreationError = MutableLiveData<String?>()
    val transactionCreationError: LiveData<String?> = _transactionCreationError

    private val _createdTransaction = MutableLiveData<Transaction?>()
    val createdTransaction: LiveData<Transaction?> = _createdTransaction

    fun loadPendingTransactions() {
        loadTransactions(includeAllStatuses = false)
    }

    fun loadTransactionHistory() {
        loadTransactions(includeAllStatuses = true)
    }

    fun loadApprovedReservations() {
        viewModelScope.launch {
            _isLoadingApprovedReservations.value = true
            _approvedReservationsError.value = null

            try {
                val approved = mutableListOf<Reservation>()
                var page = 1
                var hasMore = true

                while (hasMore) {
                    val response = repository.getReservations(
                        status = "Approved",
                        page = page,
                        pageSize = RESERVATION_PAGE_SIZE
                    )

                    if (!response.isSuccessful) {
                        _approvedReservationsError.value = reservationLoadError(response.code())
                        return@launch
                    }

                    val body = response.body()
                    if (body?.success != true) {
                        _approvedReservationsError.value =
                            "Unable to load approved reservations. Please try again."
                        return@launch
                    }

                    val rows = body.data.orEmpty()
                    approved.addAll(rows.filter {
                        it.status.equals("Approved", ignoreCase = true)
                    })
                    hasMore = rows.size == RESERVATION_PAGE_SIZE
                    page++
                }

                _approvedReservations.value = approved
            } catch (_: IOException) {
                _approvedReservationsError.value =
                    "Unable to load approved reservations. Check your connection and retry."
            } catch (_: Exception) {
                _approvedReservationsError.value =
                    "Unable to load approved reservations. Please try again."
            } finally {
                _isLoadingApprovedReservations.value = false
            }
        }
    }

    fun createTransaction(reservationId: String) {
        if (_isCreatingTransaction.value == true || reservationId.isBlank()) return

        viewModelScope.launch {
            _isCreatingTransaction.value = true
            _transactionCreationError.value = null
            _createdTransaction.value = null

            try {
                val response = repository.createTransaction(reservationId)
                if (response.isSuccessful) {
                    val body = response.body()
                    if (body?.success == true && body.data != null) {
                        _createdTransaction.value = body.data
                    } else {
                        _transactionCreationError.value =
                            "Unable to create the transaction. Please try again."
                    }
                } else {
                    _transactionCreationError.value = transactionCreateError(response.code())
                }
            } catch (_: IOException) {
                _transactionCreationError.value =
                    "Unable to create the transaction. Please check your connection and try again."
            } catch (_: Exception) {
                _transactionCreationError.value =
                    "Unable to create the transaction. Please try again."
            } finally {
                _isCreatingTransaction.value = false
            }
        }
    }

    fun clearCreatedTransaction() {
        _createdTransaction.value = null
    }

    private fun loadTransactions(includeAllStatuses: Boolean) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null

            try {
                val response = repository.getTransactions()
                when {
                    response.code() == 401 -> {
                        _errorMessage.value = "Your session has expired. Please log in again."
                    }
                    response.code() == 403 -> {
                        _errorMessage.value = "You are not authorized to view transactions."
                    }
                    response.isSuccessful -> {
                        val body = response.body()
                        if (body?.success == true) {
                            val transactions = body.data.orEmpty()
                            _transactions.value = if (includeAllStatuses) {
                                transactions
                            } else {
                                transactions.filter(::isActionable)
                            }
                        } else {
                            _errorMessage.value = body?.message ?: "Unable to load transactions."
                        }
                    }
                    else -> {
                        _errorMessage.value = "Unable to load transactions (${response.code()})."
                    }
                }
            } catch (_: IOException) {
                _errorMessage.value = "Unable to connect to the transaction service."
            } catch (exception: Exception) {
                _errorMessage.value = exception.message ?: "Unable to load transactions."
            } finally {
                _isLoading.value = false
            }
        }
    }

    private fun isActionable(transaction: Transaction): Boolean {
        return transaction.status.equals("Pending", ignoreCase = true) ||
            transaction.status.equals("QRGenerated", ignoreCase = true) ||
            transaction.status.equals("VerificationPending", ignoreCase = true)
    }

    private fun reservationLoadError(statusCode: Int): String = when (statusCode) {
        401 -> "Your session has expired. Please sign in again."
        403 -> "You are not authorized to view approved reservations."
        else -> "Unable to load approved reservations. Please try again."
    }

    private fun transactionCreateError(statusCode: Int): String = when (statusCode) {
        401 -> "Your session has expired. Please sign in again."
        403 -> "You are not authorized to create transactions."
        404 -> "Reservation not found."
        409 -> "This reservation already has a transaction."
        else -> "Unable to create the transaction. Please try again."
    }

    companion object {
        private const val RESERVATION_PAGE_SIZE = 100
    }
}
