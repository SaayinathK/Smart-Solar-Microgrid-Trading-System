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

@Dao
interface ReservationDao {
    @Query("SELECT * FROM reservations ORDER BY startTime ASC") suspend fun all(): List<ReservationEntity>
    @Insert(onConflict = OnConflictStrategy.REPLACE) suspend fun insertAll(rows: List<ReservationEntity>)
    @Query("DELETE FROM reservations") suspend fun clear()
    @Transaction suspend fun replaceAll(rows: List<ReservationEntity>) { clear(); insertAll(rows) }
}

@Dao
interface AdminDashboardDao {
    @Query("SELECT * FROM admin_dashboard_cache WHERE cacheKey = :key LIMIT 1")
    suspend fun get(key: String = AdminDashboardCache.SINGLETON_KEY): AdminDashboardCache?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(cache: AdminDashboardCache)

    @Query("DELETE FROM admin_dashboard_cache")
    suspend fun clear()
}

@Dao
interface AdminConfigurationDao {
    @Query("SELECT * FROM admin_configuration_cache WHERE cacheKey = :key LIMIT 1")
    suspend fun get(key: String = AdminConfigurationCache.SINGLETON_KEY): AdminConfigurationCache?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(cache: AdminConfigurationCache)

    @Query("DELETE FROM admin_configuration_cache")
    suspend fun clear()
}

@Dao
interface AdminActivityDao {
    @Query(
        "SELECT * FROM admin_activity_cache WHERE queryKey = :queryKey " +
            "ORDER BY timestamp DESC LIMIT :limit"
    )
    suspend fun getForQuery(queryKey: String, limit: Int = 50): List<AdminActivityCache>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(rows: List<AdminActivityCache>)

    @Query("DELETE FROM admin_activity_cache WHERE queryKey = :queryKey")
    suspend fun clearQuery(queryKey: String)

    @Transaction
    suspend fun replaceQuery(queryKey: String, rows: List<AdminActivityCache>) {
        clearQuery(queryKey)
        insertAll(rows)
    }
}
