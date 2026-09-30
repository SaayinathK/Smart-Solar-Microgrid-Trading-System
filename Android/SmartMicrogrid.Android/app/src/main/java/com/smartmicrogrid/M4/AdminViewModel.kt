package com.smartmicrogrid.M4

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.data.repository.AdminRepository
import com.smartmicrogrid.models.ActivityReport
import com.smartmicrogrid.models.AdminDashboard
import com.smartmicrogrid.models.RoleDistribution
import com.smartmicrogrid.models.SystemActivity
import com.smartmicrogrid.models.SystemConfiguration
import com.smartmicrogrid.models.SystemHealth
import com.smartmicrogrid.models.UpdateConfigurationRequest
import com.smartmicrogrid.models.UserReport
import kotlinx.coroutines.launch

/**
 * State holder for the M4 administration screens. Reads fall back to the SQLite
 * cache, so [isStale] is surfaced to the UI to warn the operator that what they
 * are looking at did not come from the server just now.
 */
class AdminViewModel(application: Application) : AndroidViewModel(application) {

    private val repository: AdminRepository

    private val _dashboard = MutableLiveData<AdminDashboard?>()
    val dashboard: LiveData<AdminDashboard?> = _dashboard

    private val _health = MutableLiveData<SystemHealth?>()
    val health: LiveData<SystemHealth?> = _health

    private val _configuration = MutableLiveData<SystemConfiguration?>()
    val configuration: LiveData<SystemConfiguration?> = _configuration

    private val _activity = MutableLiveData<List<SystemActivity>>()
    val activity: LiveData<List<SystemActivity>> = _activity

    private val _userReport = MutableLiveData<UserReport?>()
    val userReport: LiveData<UserReport?> = _userReport

    private val _roleReport = MutableLiveData<List<RoleDistribution>>()
    val roleReport: LiveData<List<RoleDistribution>> = _roleReport

    private val _activityReport = MutableLiveData<ActivityReport?>()
    val activityReport: LiveData<ActivityReport?> = _activityReport

    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading

    private val _errorMessage = MutableLiveData<String?>()
    val errorMessage: LiveData<String?> = _errorMessage

    private val _isStale = MutableLiveData<Boolean>()
    val isStale: LiveData<Boolean> = _isStale

    private val _savedSuccessfully = MutableLiveData<Boolean>()
    val savedSuccessfully: LiveData<Boolean> = _savedSuccessfully

    init {
        val db = AppDatabase.getDatabase(application)
        repository = AdminRepository(
            RetrofitClient.apiService,
            db.adminDashboardDao(),
            db.adminConfigurationDao(),
            db.adminActivityDao()
        )
    }

    // ── Dashboard ──────────────────────────────────────────────────────────

    fun loadDashboard() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getDashboard()
            _isLoading.value = false
            result.onSuccess {
                _dashboard.value = it
                _isStale.value = false
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to load the administration dashboard."
            }
        }
    }

    fun loadSystemHealth() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getSystemHealth()
            _isLoading.value = false
            result.onSuccess { _health.value = it }
                .onFailure { _errorMessage.value = it.message ?: "Failed to load system health." }
        }
    }

    // ── Configuration ──────────────────────────────────────────────────────

    fun loadConfiguration() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getConfiguration()
            _isLoading.value = false
            result.onSuccess {
                _configuration.value = it
                _isStale.value = false
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to load the system configuration."
            }
        }
    }

    fun saveConfiguration(request: UpdateConfigurationRequest) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.updateConfiguration(request)
            _isLoading.value = false
            result.onSuccess {
                _configuration.value = it
                _savedSuccessfully.value = true
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to save the configuration."
            }
        }
    }

    fun setMaintenanceMode(enabled: Boolean, message: String?) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.setMaintenanceMode(enabled, message)
            _isLoading.value = false
            result.onSuccess {
                _configuration.value = it
                _savedSuccessfully.value = true
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to update maintenance mode."
            }
        }
    }

    fun setRegistrationMode(enabled: Boolean) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.setRegistrationMode(enabled)
            _isLoading.value = false
            result.onSuccess {
                _configuration.value = it
                _savedSuccessfully.value = true
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to update registration mode."
            }
        }
    }

    // ── Audit trail ────────────────────────────────────────────────────────

    fun loadActivity(
        page: Int = 1,
        pageSize: Int = 20,
        module: String? = null,
        action: String? = null,
        status: String? = null
    ) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val queryKey = activityQueryKey(module, action, status)
            val result = repository.getActivity(queryKey, page, pageSize, module, action, status)
            _isLoading.value = false
            result.onSuccess {
                _activity.value = it
                _isStale.value = false
            }.onFailure {
                _errorMessage.value = it.message ?: "Failed to load system activity."
            }
        }
    }

    // ── Reports ────────────────────────────────────────────────────────────

    fun loadUserReport(role: String? = null, status: String? = null) {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getUserReport(role = role, status = status)
            _isLoading.value = false
            result.onSuccess { _userReport.value = it }
                .onFailure { _errorMessage.value = it.message ?: "Failed to generate the user report." }
        }
    }

    fun loadRoleReport() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getRoleReport()
            _isLoading.value = false
            result.onSuccess { _roleReport.value = it }
                .onFailure { _errorMessage.value = it.message ?: "Failed to generate the role report." }
        }
    }

    fun loadActivityReport() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            val result = repository.getActivityReport()
            _isLoading.value = false
            result.onSuccess { _activityReport.value = it }
                .onFailure { _errorMessage.value = it.message ?: "Failed to generate the activity report." }
        }
    }

    fun clearMessages() {
        _errorMessage.value = null
        _savedSuccessfully.value = false
    }

    companion object {
        /**
         * Stable cache key for a filter combination. The cached page is keyed by
         * filters so switching filters does not discard the previous result.
         */
        fun activityQueryKey(module: String?, action: String?, status: String?): String =
            "module=${module.orEmpty()}|action=${action.orEmpty()}|status=${status.orEmpty()}"
    }
}
