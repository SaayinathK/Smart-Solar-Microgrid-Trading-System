package com.smartmicrogrid.data.repository

import com.smartmicrogrid.data.local.MicrogridDao
import com.smartmicrogrid.data.local.MicrogridEntity
import com.smartmicrogrid.data.remote.ApiService
import com.smartmicrogrid.models.Microgrid
import com.smartmicrogrid.models.UpdateBatteryRequest
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class MicrogridRepository(
    private val apiService: ApiService,
    private val microgridDao: MicrogridDao
) {
    suspend fun getMicrogrids(status: String? = null, search: String? = null): Result<List<Microgrid>> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.getMicrogrids(status = status, search = search)
                if (response.isSuccessful && response.body()?.success == true) {
                    val remoteList = response.body()?.data ?: emptyList()
                    // Cache to SQLite local DB
                    val entities = remoteList.map { MicrogridEntity.fromDomain(it) }
                    microgridDao.insertAll(entities)
                    Result.success(remoteList)
                } else {
                    // Fallback to SQLite cache if server returns error
                    val cachedList = microgridDao.getAllMicrogrids().map { it.toDomain() }
                    Result.success(cachedList)
                }
            } catch (e: Exception) {
                // Network failure -> Fallback to SQLite local database
                val cachedList = microgridDao.getAllMicrogrids().map { it.toDomain() }
                if (cachedList.isNotEmpty()) {
                    Result.success(cachedList)
                } else {
                    Result.failure(e)
                }
            }
        }
    }

    suspend fun getMicrogridById(id: String): Result<Microgrid> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.getMicrogridById(id)
                if (response.isSuccessful && response.body()?.success == true && response.body()?.data != null) {
                    val m = response.body()!!.data!!
                    microgridDao.insert(MicrogridEntity.fromDomain(m))
                    Result.success(m)
                } else {
                    val cached = microgridDao.getMicrogridById(id)?.toDomain()
                    if (cached != null) Result.success(cached)
                    else Result.failure(Exception("Microgrid not found."))
                }
            } catch (e: Exception) {
                val cached = microgridDao.getMicrogridById(id)?.toDomain()
                if (cached != null) Result.success(cached)
                else Result.failure(e)
            }
        }
    }

    suspend fun updateBatteryLevel(id: String, capacity: Double, currentLevel: Double): Result<Boolean> {
        return withContext(Dispatchers.IO) {
            try {
                val req = UpdateBatteryRequest(batteryCapacity = capacity, currentBatteryLevel = currentLevel)
                val response = apiService.updateBattery(id, req)
                if (response.isSuccessful && response.body()?.success == true) {
                    Result.success(true)
                } else {
                    Result.failure(Exception(response.body()?.message ?: "Update failed."))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
    }
}
