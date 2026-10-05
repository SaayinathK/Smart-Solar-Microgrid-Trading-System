package com.smartmicrogrid.data.local

import android.content.ContentValues
import android.content.Context
import android.database.Cursor
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import com.google.gson.Gson
import com.smartmicrogrid.models.User

/**
 * ============================================================================
 * Pure Native SQLiteOpenHelper Implementation (Zero External Frameworks)
 * Project: Smart Solar Microgrid Trading System - SE4040 EAD
 * Purpose: Direct Android SDK SQLite persistence for login session & cached data.
 * Author: University Development Team
 * ============================================================================
 *
 * This class directly extends android.database.sqlite.SQLiteOpenHelper, fulfilling
 * the strict "pure SQLite, no frameworks" requirement. It manages the database
 * lifecycle using standard SQLiteDatabase, ContentValues, Cursor, and raw SQL queries.
 */
class DatabaseHelper private constructor(context: Context) :
    SQLiteOpenHelper(context.applicationContext, DATABASE_NAME, null, DATABASE_VERSION) {

    private val gson = Gson()

    companion object {
        const val DATABASE_NAME = "smart_microgrid_local.db"
        const val DATABASE_VERSION = 1

        // ── Table: user_session (Persists login details per rubric) ──
        const val TABLE_USER_SESSION = "user_session"
        const val COL_SESSION_ID = "id"
        const val COL_TOKEN = "jwt_token"
        const val COL_USER_ID = "user_id"
        const val COL_FIRST_NAME = "first_name"
        const val COL_LAST_NAME = "last_name"
        const val COL_EMAIL = "email"
        const val COL_PHONE = "phone_number"
        const val COL_NIC = "nic"
        const val COL_ROLE = "role"
        const val COL_ACCOUNT_STATUS = "account_status"
        const val COL_IS_ACTIVE = "is_active"
        const val COL_USER_JSON = "raw_user_json"
        const val COL_LOGIN_TIMESTAMP = "login_timestamp"
        const val COL_LAST_UPDATED = "last_updated"

        // ── Table: cached_microgrids ──
        const val TABLE_CACHED_MICROGRIDS = "cached_microgrids"
        const val COL_GRID_ID = "id"
        const val COL_GRID_NAME = "name"
        const val COL_GRID_LOCATION = "location"
        const val COL_GRID_CAPACITY = "total_capacity"
        const val COL_GRID_STATUS = "status"
        const val COL_GRID_RAW_JSON = "raw_json"
        const val COL_GRID_CACHED_AT = "cached_at"

        // ── Table: cached_reservations ──
        const val TABLE_CACHED_RESERVATIONS = "cached_reservations"
        const val COL_RES_ID = "id"
        const val COL_RES_PROSUMER_ID = "prosumer_id"
        const val COL_RES_GRID_ID = "grid_id"
        const val COL_RES_AMOUNT = "energy_amount"
        const val COL_RES_STATUS = "status"
        const val COL_RES_RAW_JSON = "raw_json"
        const val COL_RES_CACHED_AT = "cached_at"

        @Volatile
        private var instance: DatabaseHelper? = null

        /**
         * Returns the thread-safe singleton instance of DatabaseHelper.
         */
        fun getInstance(context: Context): DatabaseHelper {
            return instance ?: synchronized(this) {
                instance ?: DatabaseHelper(context).also { instance = it }
            }
        }
    }

    override fun onCreate(db: SQLiteDatabase) {
        // 1. Create User Login Session Table
        val createSessionTable = """
            CREATE TABLE $TABLE_USER_SESSION (
                $COL_SESSION_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                $COL_TOKEN TEXT NOT NULL,
                $COL_USER_ID TEXT NOT NULL,
                $COL_FIRST_NAME TEXT,
                $COL_LAST_NAME TEXT,
                $COL_EMAIL TEXT NOT NULL,
                $COL_PHONE TEXT,
                $COL_NIC TEXT,
                $COL_ROLE TEXT NOT NULL,
                $COL_ACCOUNT_STATUS TEXT NOT NULL,
                $COL_IS_ACTIVE INTEGER NOT NULL DEFAULT 1,
                $COL_USER_JSON TEXT NOT NULL,
                $COL_LOGIN_TIMESTAMP INTEGER NOT NULL,
                $COL_LAST_UPDATED INTEGER NOT NULL
            );
        """.trimIndent()
        db.execSQL(createSessionTable)

        // 2. Create Cached Microgrids Table
        val createMicrogridsTable = """
            CREATE TABLE $TABLE_CACHED_MICROGRIDS (
                $COL_GRID_ID TEXT PRIMARY KEY,
                $COL_GRID_NAME TEXT NOT NULL,
                $COL_GRID_LOCATION TEXT NOT NULL,
                $COL_GRID_CAPACITY REAL NOT NULL,
                $COL_GRID_STATUS TEXT NOT NULL,
                $COL_GRID_RAW_JSON TEXT NOT NULL,
                $COL_GRID_CACHED_AT INTEGER NOT NULL
            );
        """.trimIndent()
        db.execSQL(createMicrogridsTable)

        // 3. Create Cached Reservations Table
        val createReservationsTable = """
            CREATE TABLE $TABLE_CACHED_RESERVATIONS (
                $COL_RES_ID TEXT PRIMARY KEY,
                $COL_RES_PROSUMER_ID TEXT NOT NULL,
                $COL_RES_GRID_ID TEXT NOT NULL,
                $COL_RES_AMOUNT REAL NOT NULL,
                $COL_RES_STATUS TEXT NOT NULL,
                $COL_RES_RAW_JSON TEXT NOT NULL,
                $COL_RES_CACHED_AT INTEGER NOT NULL
            );
        """.trimIndent()
        db.execSQL(createReservationsTable)
    }

    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        db.execSQL("DROP TABLE IF EXISTS $TABLE_USER_SESSION")
        db.execSQL("DROP TABLE IF EXISTS $TABLE_CACHED_MICROGRIDS")
        db.execSQL("DROP TABLE IF EXISTS $TABLE_CACHED_RESERVATIONS")
        onCreate(db)
    }

    // =========================================================================
    // User Login Details & Session Operations (SQLite Persistence)
    // =========================================================================

    /**
     * Persists the user login details and JWT token into SQLite.
     * Clears any previous session row and inserts the new authenticated session.
     */
    @Synchronized
    fun saveLoginSession(token: String, user: User): Boolean {
        val db = writableDatabase
        return try {
            db.beginTransaction()

            // Remove existing session
            db.delete(TABLE_USER_SESSION, null, null)

            val now = System.currentTimeMillis()
            val values = ContentValues().apply {
                put(COL_TOKEN, token)
                put(COL_USER_ID, user.id)
                put(COL_FIRST_NAME, user.firstName)
                put(COL_LAST_NAME, user.lastName)
                put(COL_EMAIL, user.email)
                put(COL_PHONE, user.phoneNumber)
                put(COL_NIC, user.nic)
                put(COL_ROLE, user.role)
                put(COL_ACCOUNT_STATUS, user.accountStatus)
                put(COL_IS_ACTIVE, if (user.isActive) 1 else 0)
                put(COL_USER_JSON, gson.toJson(user))
                put(COL_LOGIN_TIMESTAMP, now)
                put(COL_LAST_UPDATED, now)
            }

            val insertedId = db.insert(TABLE_USER_SESSION, null, values)
            db.setTransactionSuccessful()
            insertedId != -1L
        } catch (e: Exception) {
            e.printStackTrace()
            false
        } finally {
            db.endTransaction()
        }
    }

    /**
     * Retrieves the stored login session (JWT token and User profile) from SQLite.
     */
    @Synchronized
    fun getLoginSession(): Pair<String, User>? {
        val db = readableDatabase
        var cursor: Cursor? = null
        return try {
            val query = "SELECT $COL_TOKEN, $COL_USER_JSON FROM $TABLE_USER_SESSION ORDER BY $COL_SESSION_ID DESC LIMIT 1"
            cursor = db.rawQuery(query, null)
            if (cursor != null && cursor.moveToFirst()) {
                val token = cursor.getString(cursor.getColumnIndexOrThrow(COL_TOKEN))
                val userJson = cursor.getString(cursor.getColumnIndexOrThrow(COL_USER_JSON))
                val user = gson.fromJson(userJson, User::class.java)
                Pair(token, user)
            } else {
                null
            }
        } catch (e: Exception) {
            e.printStackTrace()
            null
        } finally {
            cursor?.close()
        }
    }

    /**
     * Retrieves the persisted JWT bearer token from SQLite.
     */
    @Synchronized
    fun getToken(): String? {
        val db = readableDatabase
        var cursor: Cursor? = null
        return try {
            cursor = db.query(
                TABLE_USER_SESSION,
                arrayOf(COL_TOKEN),
                null, null, null, null,
                "$COL_SESSION_ID DESC",
                "1"
            )
            if (cursor != null && cursor.moveToFirst()) {
                cursor.getString(cursor.getColumnIndexOrThrow(COL_TOKEN))
            } else {
                null
            }
        } catch (e: Exception) {
            e.printStackTrace()
            null
        } finally {
            cursor?.close()
        }
    }

    /**
     * Retrieves the persisted User entity from SQLite.
     */
    @Synchronized
    fun getUser(): User? {
        val db = readableDatabase
        var cursor: Cursor? = null
        return try {
            cursor = db.query(
                TABLE_USER_SESSION,
                arrayOf(COL_USER_JSON),
                null, null, null, null,
                "$COL_SESSION_ID DESC",
                "1"
            )
            if (cursor != null && cursor.moveToFirst()) {
                val json = cursor.getString(cursor.getColumnIndexOrThrow(COL_USER_JSON))
                gson.fromJson(json, User::class.java)
            } else {
                null
            }
        } catch (e: Exception) {
            e.printStackTrace()
            null
        } finally {
            cursor?.close()
        }
    }

    /**
     * Checks if there is an active logged-in user session in SQLite.
     */
    @Synchronized
    fun hasActiveSession(): Boolean {
        val db = readableDatabase
        var cursor: Cursor? = null
        return try {
            cursor = db.rawQuery("SELECT COUNT(*) FROM $TABLE_USER_SESSION", null)
            if (cursor != null && cursor.moveToFirst()) {
                cursor.getInt(0) > 0
            } else {
                false
            }
        } catch (e: Exception) {
            false
        } finally {
            cursor?.close()
        }
    }

    /**
     * Updates user details in the active SQLite session.
     */
    @Synchronized
    fun updateUser(user: User): Boolean {
        val db = writableDatabase
        return try {
            val values = ContentValues().apply {
                put(COL_FIRST_NAME, user.firstName)
                put(COL_LAST_NAME, user.lastName)
                put(COL_EMAIL, user.email)
                put(COL_PHONE, user.phoneNumber)
                put(COL_NIC, user.nic)
                put(COL_ROLE, user.role)
                put(COL_ACCOUNT_STATUS, user.accountStatus)
                put(COL_IS_ACTIVE, if (user.isActive) 1 else 0)
                put(COL_USER_JSON, gson.toJson(user))
                put(COL_LAST_UPDATED, System.currentTimeMillis())
            }
            val rows = db.update(TABLE_USER_SESSION, values, null, null)
            rows > 0
        } catch (e: Exception) {
            e.printStackTrace()
            false
        }
    }

    /**
     * Clears the user login session from SQLite on logout.
     */
    @Synchronized
    fun clearLoginSession(): Boolean {
        val db = writableDatabase
        return try {
            db.delete(TABLE_USER_SESSION, null, null) >= 0
        } catch (e: Exception) {
            e.printStackTrace()
            false
        }
    }
}
