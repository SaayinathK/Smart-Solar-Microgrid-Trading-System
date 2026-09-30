package com.smartmicrogrid.data.repository

import com.smartmicrogrid.data.local.AdminActivityCache
import com.smartmicrogrid.data.local.AdminActivityDao
import com.smartmicrogrid.data.local.AdminConfigurationCache
import com.smartmicrogrid.data.local.AdminConfigurationDao
import com.smartmicrogrid.data.local.AdminDashboardCache
import com.smartmicrogrid.data.local.AdminDashboardDao
import com.smartmicrogrid.data.remote.ApiService
import com.smartmicrogrid.models.ActivityReport
import com.smartmicrogrid.models.AdminDashboard
import com.smartmicrogrid.models.MaintenanceModeRequest
import com.smartmicrogrid.models.RegistrationModeRequest
import com.smartmicrogrid.models.RoleDistribution
import com.smartmicrogrid.models.SystemActivity
import com.smartmicrogrid.models.SystemConfiguration
import com.smartmicrogrid.models.SystemHealth
import com.smartmicrogrid.models.UpdateConfigurationRequest
import com.smartmicrogrid.models.UserReport
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

/**
 * Data source for the M4 administration module.
 *
 * Read operations follow the same pattern as [MicrogridRepository]: try the API
 * first, fall back to the SQLite cache when the network is unavailable so the
 * module stays usable offline. Write operations never fall back, because silently
 * reporting a configuration change that never reached the server would be worse
 * than an explicit failure.
 */
class AdminRepository(
    private val apiService: ApiService,
    private val dashboardDao: AdminDashboardDao,
    private val configurationDao: AdminConfigurationDao,
    private val activityDao: AdminActivityDao
) {

    // ── Dashboard ──────────────────────────────────────────────────────────

    suspend fun getDashboard(): Result<AdminDashboard> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getAdminDashboard()
            val dashboard = response.body()?.takeIf { it.success }?.data

            if (dashboard != null) {
                dashboardDao.upsert(AdminDashboardCache.fromDomain(dashboard))
                Result.success(dashboard)
            } else {
                cachedDashboard(response.body()?.message)
            }
        } catch (e: Exception) {
            cachedDashboard(e.message)
        }
    }

    private suspend fun cachedDashboard(error: String?): Result<AdminDashboard> {
        val cached = dashboardDao.get()
        return if (cached != null) {
            Result.success(cached.toDomain())
        } else {
            Result.failure(Exception(error ?: "Administration data is unavailable."))
        }
    }

    // ── System health ──────────────────────────────────────────────────────

    suspend fun getSystemHealth(): Result<SystemHealth> = withContext(Dispatchers.IO) {
        runCatching {
            val response = apiService.getSystemHealth()
            val health = response.body()?.takeIf { it.success }?.data
                ?: throw Exception(response.body()?.message ?: "Failed to load system health.")
            health
        }
    }

    // ── Configuration ──────────────────────────────────────────────────────

    suspend fun getConfiguration(): Result<SystemConfiguration> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getSystemConfiguration()
            val config = response.body()?.takeIf { it.success }?.data

            if (config != null) {
                configurationDao.upsert(AdminConfigurationCache.fromDomain(config))
                Result.success(config)
            } else {
                cachedConfiguration(response.body()?.message)
            }
        } catch (e: Exception) {
            cachedConfiguration(e.message)
        }
    }

    private suspend fun cachedConfiguration(error: String?): Result<SystemConfiguration> {
        val cached = configurationDao.get()
        return if (cached != null) {
            Result.success(AdminConfigurationCache.toDomain(cached))
        } else {
            Result.failure(Exception(error ?: "System configuration is unavailable."))
        }
    }

    suspend fun updateConfiguration(request: UpdateConfigurationRequest): Result<SystemConfiguration> =
        withContext(Dispatchers.IO) {
            runCatching {
                val response = apiService.updateSystemConfiguration(request)
                val config = response.body()?.takeIf { it.success }?.data
                    ?: throw Exception(response.body()?.message ?: "Failed to save configuration.")
                configurationDao.upsert(AdminConfigurationCache.fromDomain(config))
                config
            }
        }

    suspend fun setMaintenanceMode(enabled: Boolean, message: String?): Result<SystemConfiguration> =
        withContext(Dispatchers.IO) {
            runCatching {
                val response = apiService.setMaintenanceMode(
                    MaintenanceModeRequest(enabled, message?.trim()?.ifEmpty { null })
                )
                val config = response.body()?.takeIf { it.success }?.data
                    ?: throw Exception(response.body()?.message ?: "Failed to update maintenance mode.")
                configurationDao.upsert(AdminConfigurationCache.fromDomain(config))
                config
            }
        }

    suspend fun setRegistrationMode(enabled: Boolean): Result<SystemConfiguration> =
        withContext(Dispatchers.IO) {
            runCatching {
                val response = apiService.setRegistrationMode(RegistrationModeRequest(enabled))
                val config = response.body()?.takeIf { it.success }?.data
                    ?: throw Exception(response.body()?.message ?: "Failed to update registration mode.")
                configurationDao.upsert(AdminConfigurationCache.fromDomain(config))
                config
            }
        }

    // ── Audit trail ────────────────────────────────────────────────────────

    /**
     * @param queryKey identifies the active filter combination so cached pages
     *   for different filters coexist instead of overwriting one another.
     */
    suspend fun getActivity(
        queryKey: String,
        page: Int = 1,
        pageSize: Int = 20,
        module: String? = null,
        action: String? = null,
        status: String? = null
    ): Result<List<SystemActivity>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getSystemActivity(
                page = page,
                pageSize = pageSize,
                module = module,
                action = action,
                status = status
            )
            val paged = response.body()?.takeIf { it.success }?.data

            if (paged != null) {
                // Only the first page replaces the cache, so paging back through
                // the history does not discard the pages already stored.
                if (page == 1) {
                    activityDao.replaceQuery(
                        queryKey,
                        paged.items.map { AdminActivityCache.fromDomain(queryKey, it) }
                    )
                }
                Result.success(paged.items)
            } else {
                cachedActivity(queryKey, response.body()?.message)
            }
        } catch (e: Exception) {
            cachedActivity(queryKey, e.message)
        }
    }

    private suspend fun cachedActivity(queryKey: String, error: String?): Result<List<SystemActivity>> {
        val cached = activityDao.getForQuery(queryKey).map { it.toDomain() }
        return if (cached.isNotEmpty()) {
            Result.success(cached)
        } else {
            Result.failure(Exception(error ?: "System activity is unavailable."))
        }
    }

    // ── Reports ────────────────────────────────────────────────────────────

    suspend fun getUserReport(
        from: String? = null,
        to: String? = null,
        role: String? = null,
        status: String? = null
    ): Result<UserReport> = withContext(Dispatchers.IO) {
        runCatching {
            val response = apiService.getUserReport(from, to, role, status)
            response.body()?.takeIf { it.success }?.data
                ?: throw Exception(response.body()?.message ?: "Failed to generate the user report.")
        }
    }

    suspend fun getRoleReport(): Result<List<RoleDistribution>> = withContext(Dispatchers.IO) {
        runCatching {
            val response = apiService.getRoleReport()
            response.body()?.takeIf { it.success }?.data
                ?: throw Exception(response.body()?.message ?: "Failed to generate the role report.")
        }
    }

    suspend fun getActivityReport(from: String? = null, to: String? = null): Result<ActivityReport> =
        withContext(Dispatchers.IO) {
            runCatching {
                val response = apiService.getActivityReport(from, to)
                response.body()?.takeIf { it.success }?.data
                    ?: throw Exception(response.body()?.message ?: "Failed to generate the activity report.")
            }
        }
}
