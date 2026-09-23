package com.smartmicrogrid.data.local

import androidx.room.*

@Dao
interface MicrogridDao {
    @Query("SELECT * FROM microgrids ORDER BY name ASC")
    suspend fun getAllMicrogrids(): List<MicrogridEntity>

    @Query("SELECT * FROM microgrids WHERE id = :id")
    suspend fun getMicrogridById(id: String): MicrogridEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(microgrids: List<MicrogridEntity>)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(microgrid: MicrogridEntity)

    @Query("DELETE FROM microgrids")
    suspend fun clearAll()
}

@Dao
interface EnergySlotDao {
    @Query("SELECT * FROM energy_slots ORDER BY startTime ASC")
    suspend fun getAllSlots(): List<EnergySlotEntity>

    @Query("SELECT * FROM energy_slots WHERE status = 'Available' ORDER BY startTime ASC")
    suspend fun getAvailableSlots(): List<EnergySlotEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(slots: List<EnergySlotEntity>)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(slot: EnergySlotEntity)

    @Query("DELETE FROM energy_slots")
    suspend fun clearAll()
}
