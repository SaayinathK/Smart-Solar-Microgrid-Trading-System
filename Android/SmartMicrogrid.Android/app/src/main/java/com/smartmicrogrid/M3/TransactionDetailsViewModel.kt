package com.smartmicrogrid.M3

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.TransactionRepository
import com.smartmicrogrid.models.GenerateQrResponse
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

    private val _isGeneratingQr = MutableLiveData(false)
    val isGeneratingQr: LiveData<Boolean> = _isGeneratingQr

    private val _qrGenerationResult = MutableLiveData<GenerateQrResponse?>()
    val qrGenerationResult: LiveData<GenerateQrResponse?> = _qrGenerationResult

    private val _qrGenerationError = MutableLiveData<String?>()
    val qrGenerationError: LiveData<String?> = _qrGenerationError

    private val _isVerifying = MutableLiveData<Boolean>()
    val isVerifying: LiveData<Boolean> = _isVerifying

    private val _verificationResult = MutableLiveData<Transaction?>()
    val verificationResult: LiveData<Transaction?> = _verificationResult

    private val _verificationError = MutableLiveData<String?>()
    val verificationError: LiveData<String?> = _verificationError

    fun generateTransactionQr(transactionId: String) {
        if (_isGeneratingQr.value == true) return

        val currentTransaction = _transaction.value
        if (currentTransaction == null || !currentTransaction.status.equals("Pending", ignoreCase = true)) {
            _qrGenerationError.value = "QR generation is not valid for the current transaction state."
            return
        }

        viewModelScope.launch {
            _isGeneratingQr.value = true
            _qrGenerationError.value = null
            _qrGenerationResult.value = null

            try {
                val response = repository.generateTransactionQr(transactionId)
                when {
                    response.code() == 401 -> {
                        _qrGenerationError.value = "Your session has expired. Please sign in again."
                    }
                    response.code() == 403 -> {
                        _qrGenerationError.value = "You are not authorized to generate this QR code."
                    }
                    response.code() == 404 -> {
                        _qrGenerationError.value = "Transaction not found."
                    }
                    response.code() == 409 -> {
                        _qrGenerationError.value = "QR generation is not valid for the current transaction state."
                    }
                    response.isSuccessful -> {
                        val body = response.body()
                        val qr = body?.data
                        if (body?.success == true && qr != null &&
                            qr.transactionId.isNotBlank() && qr.qrCodeData.isNotBlank()
                        ) {
                            _transaction.value = currentTransaction.copy(
                                id = qr.transactionId,
                                transactionCode = qr.transactionCode,
                                qrCodeData = qr.qrCodeData,
                                status = qr.status
                            )
                            _qrGenerationResult.value = qr
                        } else {
                            _qrGenerationError.value = "Unable to generate the QR code. Please try again."
                        }
                    }
                    else -> {
                        _qrGenerationError.value = "Unable to generate the QR code. Please try again."
                    }
                }
            } catch (_: IOException) {
                _qrGenerationError.value = "Unable to generate the QR code. Please try again."
            } catch (_: Exception) {
                _qrGenerationError.value = "Unable to generate the QR code. Please try again."
            } finally {
                _isGeneratingQr.value = false
            }
        }
    }

    fun clearQrGenerationResult() {
        _qrGenerationResult.value = null
    }

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

    fun verifyTransaction(transactionId: String, qrCodeData: String) {
        if (_isVerifying.value == true) return

        val currentTransaction = _transaction.value
        if (currentTransaction == null || currentTransaction.id != transactionId) {
            _verificationError.value = "Transaction not found."
            return
        }

        if (currentTransaction.status.equals("Verified", ignoreCase = true)) {
            _verificationError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.transaction_already_verified)
            return
        }

        if (!currentTransaction.status.equals("QRGenerated", ignoreCase = true) &&
            !currentTransaction.status.equals("VerificationPending", ignoreCase = true)
        ) {
            _verificationError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.transaction_cannot_be_verified)
            return
        }

        val qrParts = qrCodeData.split('|')
        if (qrParts.size != 4 || qrParts[0] != "SMART-MICROGRID" ||
            qrParts[1] != "TRANSACTION" || qrParts[2].isBlank() || qrParts[3].isBlank()
        ) {
            _verificationError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.invalid_transaction_qr)
            return
        }

        if (qrParts[2] != currentTransaction.id) {
            _verificationError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.different_transaction_qr)
            return
        }

        if (qrParts[3] != currentTransaction.transactionCode) {
            _verificationError.value = getApplication<Application>()
                .getString(com.smartmicrogrid.R.string.invalid_transaction_qr)
            return
        }

        viewModelScope.launch {
            _isVerifying.value = true
            _verificationError.value = null
            _verificationResult.value = null

            try {
                val response = repository.verifyTransaction(transactionId, qrCodeData)
                when {
                    response.code() == 401 -> {
                        _verificationError.value = getApplication<Application>()
                            .getString(com.smartmicrogrid.R.string.session_expired_sign_in)
                    }
                    response.code() == 403 -> {
                        _verificationError.value = "You are not authorized to verify this transaction."
                    }
                    response.code() == 404 -> {
                        _verificationError.value = "Transaction not found."
                    }
                    response.code() == 409 -> {
                        _verificationError.value = getApplication<Application>()
                            .getString(com.smartmicrogrid.R.string.transaction_cannot_be_verified)
                    }
                    response.code() == 502 -> {
                        _verificationError.value = getApplication<Application>()
                            .getString(com.smartmicrogrid.R.string.verification_reservation_unavailable)
                    }
                    response.isSuccessful -> {
                        val body = response.body()
                        if (body?.success == true && body.data != null) {
                            _transaction.value = body.data
                            _verificationResult.value = body.data
                        } else {
                            _verificationError.value = getApplication<Application>()
                                .getString(com.smartmicrogrid.R.string.verification_failed_retry)
                        }
                    }
                    else -> {
                        _verificationError.value = getApplication<Application>()
                            .getString(com.smartmicrogrid.R.string.verification_failed_retry)
                    }
                }
            } catch (_: IOException) {
                _verificationError.value = getApplication<Application>()
                    .getString(com.smartmicrogrid.R.string.verification_failed_retry)
            } catch (_: Exception) {
                _verificationError.value = getApplication<Application>()
                    .getString(com.smartmicrogrid.R.string.verification_failed_retry)
            } finally {
                _isVerifying.value = false
            }
        }
    }
}
