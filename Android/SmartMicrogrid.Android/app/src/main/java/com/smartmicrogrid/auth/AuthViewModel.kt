package com.smartmicrogrid.auth

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.AuthRepository
import com.smartmicrogrid.models.*
import kotlinx.coroutines.launch
import kotlinx.coroutines.CancellationException

class AuthViewModel : ViewModel() {

    private val repository = AuthRepository(RetrofitClient.apiService)

    // ── Login State ──
    private val _loginResult = MutableLiveData<Result<LoginResponse>?>()
    val loginResult: LiveData<Result<LoginResponse>?> = _loginResult

    private val _isLoading = MutableLiveData(false)
    val isLoading: LiveData<Boolean> = _isLoading

    // ── Register State ──
    private val _registerResult = MutableLiveData<Result<User>?>()
    val registerResult: LiveData<Result<User>?> = _registerResult

    // ── Profile State ──
    private val _profileResult = MutableLiveData<Result<User>?>()
    val profileResult: LiveData<Result<User>?> = _profileResult

    private val _updateResult = MutableLiveData<Result<User>?>()
    val updateResult: LiveData<Result<User>?> = _updateResult

    private val _passwordResult = MutableLiveData<Result<Boolean>?>()
    val passwordResult: LiveData<Result<Boolean>?> = _passwordResult

    private val _logoutResult = MutableLiveData<Result<Unit>?>()
    val logoutResult: LiveData<Result<Unit>?> = _logoutResult

    fun login(email: String, password: String) {
        _isLoading.value = true
        viewModelScope.launch {
            _loginResult.value = repository.login(email, password)
            _isLoading.value = false
        }
    }

    fun register(request: RegisterRequest) {
        _isLoading.value = true
        viewModelScope.launch {
            _registerResult.value = repository.register(request)
            _isLoading.value = false
        }
    }

    fun loadProfile() {
        _isLoading.value = true
        viewModelScope.launch {
            _profileResult.value = repository.getProfile()
            _isLoading.value = false
        }
    }

    fun updateProfile(request: UpdateProfileRequest) {
        _isLoading.value = true
        viewModelScope.launch {
            _updateResult.value = repository.updateProfile(request)
            _isLoading.value = false
        }
    }

    fun changePassword(request: ChangePasswordRequest) {
        _isLoading.value = true
        viewModelScope.launch {
            _passwordResult.value = repository.changePassword(request)
            _isLoading.value = false
        }
    }

    fun logout() {
        viewModelScope.launch {
            try {
                repository.logout()
                _logoutResult.value = Result.success(Unit)
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                _logoutResult.value = Result.failure(error)
            }
        }
    }
}
