package com.smartmicrogrid.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface LoginSessionDao {
    @Query("SELECT * FROM login_session WHERE id = 1")
    suspend fun getSession(): LoginSessionEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun saveSession(session: LoginSessionEntity)

    @Query("DELETE FROM login_session")
    suspend fun clearSession()
}
