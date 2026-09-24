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

class TransactionDetailsViewModel(application: Application) : AndroidViewModel(application) {

    private val repository = TransactionRepository(RetrofitClient.apiService)

    private val _transaction = MutableLiveData<Transaction?>()
    val transaction: LiveData<Transaction?> = _transaction

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String?>()
    val errorMessage: LiveData<String?> = _errorMessage

    fun loadTransaction(transactionId: String) {
        if (transactionId.isBlank()) {
            _errorMessage.value = "Transaction ID is required."
            return
        }

        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null

            try {
                val response = repository.getTransactionById(transactionId)
                when {
                    response.code() == 401 -> {
                        _errorMessage.value = "Your session has expired. Please log in again."
                    }
                    response.code() == 403 -> {
                        _errorMessage.value = "You are not authorized to view this transaction."
                    }
                    response.code() == 404 -> {
                        _errorMessage.value = "Transaction not found."
                    }
                    response.isSuccessful -> {
                        val body = response.body()
                        if (body?.success == true && body.data != null) {
                            _transaction.value = body.data
                        } else {
                            _errorMessage.value = body?.message ?: "Transaction not found."
                        }
                    }
                    else -> {
                        _errorMessage.value = "Unable to load transaction (${response.code()})."
                    }
                }
            } catch (_: IOException) {
                _errorMessage.value = "Unable to connect to the transaction service."
            } catch (exception: Exception) {
                _errorMessage.value = exception.message ?: "Unable to load transaction."
            } finally {
                _isLoading.value = false
            }
        }
    }
}