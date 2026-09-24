package com.smartmicrogrid.M1.slots

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.EnergySlotRepository
import com.smartmicrogrid.models.EnergySlot
import kotlinx.coroutines.launch

class EnergySlotViewModel(application: Application) : AndroidViewModel(application) {

    private val repository: EnergySlotRepository

    private val _slots = MutableLiveData<List<EnergySlot>>()
    val slots: LiveData<List<EnergySlot>> = _slots

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String?>()
    val errorMessage: LiveData<String?> = _errorMessage

    init {
        val db = AppDatabase.getDatabase(application)
        repository = EnergySlotRepository(RetrofitClient.apiService, db.energySlotDao())
    }

    fun loadEnergySlots(microgridId: String? = null) {
        viewModelScope.launch {
            _isLoading.value = true
            val result = repository.getEnergySlots(microgridId)
            _isLoading.value = false
            result.onSuccess {
                _slots.value = it
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to load energy slots."
            }
        }
    }
}
