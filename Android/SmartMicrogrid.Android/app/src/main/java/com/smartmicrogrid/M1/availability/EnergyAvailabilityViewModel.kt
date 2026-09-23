package com.smartmicrogrid.M1.availability

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.EnergySlotRepository
import com.smartmicrogrid.models.EnergyAvailabilitySlot
import kotlinx.coroutines.launch

class EnergyAvailabilityViewModel(application: Application) : AndroidViewModel(application) {

    private val repository: EnergySlotRepository

    private val _availableSlots = MutableLiveData<List<EnergyAvailabilitySlot>>()
    val availableSlots: LiveData<List<EnergyAvailabilitySlot>> = _availableSlots

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String?>()
    val errorMessage: LiveData<String?> = _errorMessage

    init {
        val db = AppDatabase.getDatabase(application)
        repository = EnergySlotRepository(RetrofitClient.apiService, db.energySlotDao())
    }

    fun loadAvailability(location: String? = null) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getEnergyAvailability(location)
            _isLoading.value = false
            result.onSuccess {
                _availableSlots.value = it
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to load energy availability."
            }
        }
    }
}
