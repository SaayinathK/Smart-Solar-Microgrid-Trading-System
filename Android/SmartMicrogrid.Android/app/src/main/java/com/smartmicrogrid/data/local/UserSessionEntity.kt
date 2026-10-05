package com.smartmicrogrid.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

/**
 * ============================================================================
 * Room User Session Entity (SQLite Persistence)
 * Project: Smart Solar Microgrid Trading System - SE4040 EAD
 * Purpose: Persists login details in SQLite via Room abstraction layer.
 * ============================================================================
 */
@Entity(tableName = "room_user_session")
data class UserSessionEntity(
    @PrimaryKey val id: Int = 1,
    val token: String,
    val userId: String,
    val firstName: String,
    val lastName: String,
    val email: String,
    val role: String,
    val accountStatus: String,
    val isActive: Boolean,
    val userJson: String,
    val loginTimestamp: Long = System.currentTimeMillis()
)
