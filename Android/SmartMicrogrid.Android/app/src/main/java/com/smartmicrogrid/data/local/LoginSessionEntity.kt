package com.smartmicrogrid.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

/** One signed-in account; passwords are never persisted. */
@Entity(tableName = "login_session")
data class LoginSessionEntity(
    @PrimaryKey val id: Int = 1,
    val token: String,
    val userId: String,
    val email: String,
    val role: String,
    val displayName: String,
    val userJson: String
)
