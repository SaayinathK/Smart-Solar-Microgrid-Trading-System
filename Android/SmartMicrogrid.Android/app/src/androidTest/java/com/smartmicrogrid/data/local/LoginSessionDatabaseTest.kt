package com.smartmicrogrid.data.local

import android.content.Context
import android.database.sqlite.SQLiteDatabase
import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.smartmicrogrid.models.User
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.*
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class LoginSessionDatabaseTest {
    private val context = ApplicationProvider.getApplicationContext<Context>()
    private val name = "login-session-test.db"
    private val user = User(id = "test-user", email = "test@example.test", role = "Prosumer")
    private fun open() = Room.databaseBuilder(context, AppDatabase::class.java, name)
        .addMigrations(AppDatabase.MIGRATION_2_3).build()

    private suspend fun withDatabase(block: suspend (AppDatabase) -> Unit) {
        val db = open()
        try { block(db) } finally { db.close() }
    }

    @After fun cleanup() { context.deleteDatabase(name) }

    @Test fun sqlitePersistsAcrossReopenAndLogoutRemovesTheRow() = runBlocking {
        withDatabase { db -> db.loginSessionDao().saveSession(LoginSessionStore.fromUser("test-token", user)) }
        withDatabase { db ->
            assertEquals("test-token", db.loginSessionDao().getSession()?.token)
            db.loginSessionDao().clearSession()
        }
        withDatabase { db -> assertNull(db.loginSessionDao().getSession()) }
    }

    @Test fun migrationFromVersion2PreservesCachedReservationsAndValidatesRoomSchema() = runBlocking {
        // Create the current cache schema, then reproduce the version 2 layout.
        withDatabase { db -> db.loginSessionDao().getSession() }
        SQLiteDatabase.openDatabase(context.getDatabasePath(name).path, null, SQLiteDatabase.OPEN_READWRITE).use { db ->
            db.execSQL("DROP TABLE login_session")
            db.execSQL("INSERT INTO reservations VALUES ('reservation-1', 'user-1', 'grid-1', 'slot-1', 5.0, 'date', 'start', 'end', 'Pending', 'created', 'updated', 1)")
            db.version = 2
        }
        withDatabase { db ->
            assertNull(db.loginSessionDao().getSession())
            assertEquals("reservation-1", db.reservationDao().all().single().id)
            db.loginSessionDao().saveSession(LoginSessionStore.fromUser("migrated-token", user))
            assertEquals("migrated-token", db.loginSessionDao().getSession()?.token)
        }
    }
}
