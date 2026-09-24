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

class TransactionViewModel(application: Application) : AndroidViewModel(application) {

    private val repository = TransactionRepository(RetrofitClient.apiService)

    private val _transactions = MutableLiveData<List<Transaction>>()
    val transactions: LiveData<List<Transaction>> = _transactions

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String?>()
    val errorMessage: LiveData<String?> = _errorMessage

    fun loadPendingTransactions() {
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
                            _transactions.value = body.data.orEmpty().filter(::isActionable)
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
}