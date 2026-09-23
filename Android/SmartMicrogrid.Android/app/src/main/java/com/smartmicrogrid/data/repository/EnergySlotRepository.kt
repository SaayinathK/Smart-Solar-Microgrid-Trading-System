package com.smartmicrogrid.data.repository

import com.smartmicrogrid.data.local.EnergySlotDao
import com.smartmicrogrid.data.local.EnergySlotEntity
import com.smartmicrogrid.data.remote.ApiService
import com.smartmicrogrid.models.CreateEnergySlotRequest
import com.smartmicrogrid.models.EnergyAvailabilitySlot
import com.smartmicrogrid.models.EnergySlot
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class EnergySlotRepository(
    private val apiService: ApiService,
    private val energySlotDao: EnergySlotDao
) {
    suspend fun getEnergySlots(microgridId: String? = null): Result<List<EnergySlot>> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.getEnergySlots(microgridId = microgridId)
                if (response.isSuccessful && response.body()?.success == true) {
                    val list = response.body()?.data ?: emptyList()
                    val entities = list.map { EnergySlotEntity.fromDomain(it) }
                    energySlotDao.insertAll(entities)
                    Result.success(list)
                } else {
                    val cached = energySlotDao.getAllSlots().map { it.toDomain() }
                    Result.success(cached)
                }
            } catch (e: Exception) {
                val cached = energySlotDao.getAllSlots().map { it.toDomain() }
                if (cached.isNotEmpty()) Result.success(cached)
                else Result.failure(e)
            }
        }
    }

    suspend fun getEnergyAvailability(location: String? = null): Result<List<EnergyAvailabilitySlot>> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.getEnergyAvailability(location = location)
                if (response.isSuccessful && response.body()?.success == true) {
                    Result.success(response.body()?.data ?: emptyList())
                } else {
                    Result.failure(Exception(response.body()?.message ?: "Failed to fetch energy availability"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
    }

    suspend fun createEnergySlot(req: CreateEnergySlotRequest): Result<EnergySlot> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.createEnergySlot(req)
                if (response.isSuccessful && response.body()?.success == true && response.body()?.data != null) {
                    val created = response.body()!!.data!!
                    energySlotDao.insert(EnergySlotEntity.fromDomain(created))
                    Result.success(created)
                } else {
                    Result.failure(Exception(response.body()?.message ?: "Failed to publish energy slot"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
    }
}
