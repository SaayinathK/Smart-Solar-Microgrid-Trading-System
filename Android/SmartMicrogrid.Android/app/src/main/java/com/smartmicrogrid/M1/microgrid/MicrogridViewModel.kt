package com.smartmicrogrid.M1.microgrid

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.MicrogridRepository
import com.smartmicrogrid.models.Microgrid
import kotlinx.coroutines.launch

class MicrogridViewModel(application: Application) : AndroidViewModel(application) {

    private val repository: MicrogridRepository

    private val _microgrids = MutableLiveData<List<Microgrid>>()
    val microgrids: LiveData<List<Microgrid>> = _microgrids

    private val _selectedMicrogrid = MutableLiveData<Microgrid?>()
    val selectedMicrogrid: LiveData<Microgrid?> = _selectedMicrogrid

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String?>()
    val errorMessage: LiveData<String?> = _errorMessage

    init {
        val db = AppDatabase.getDatabase(application)
        repository = MicrogridRepository(RetrofitClient.apiService, db.microgridDao())
    }

    fun loadMicrogrids(status: String? = null, search: String? = null) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getMicrogrids(status, search)
            _isLoading.value = false
            result.onSuccess {
                _microgrids.value = it
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to load microgrids."
            }
        }
    }

    fun loadMicrogridById(id: String) {
        viewModelScope.launch {
            _isLoading.value = true
            val result = repository.getMicrogridById(id)
            _isLoading.value = false
            result.onSuccess {
                _selectedMicrogrid.value = it
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to load microgrid details."
            }
        }
    }
}
