package com.smartmicrogrid.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import androidx.room.migration.Migration
import androidx.sqlite.db.SupportSQLiteDatabase

@Database(
    entities = [
        MicrogridEntity::class,
        EnergySlotEntity::class,
        ReservationEntity::class,
        AdminDashboardCache::class,
        AdminConfigurationCache::class,
        AdminActivityCache::class
    ],
    version = 3,
    exportSchema = false
)
abstract class AppDatabase : RoomDatabase() {

    abstract fun microgridDao(): MicrogridDao
    abstract fun energySlotDao(): EnergySlotDao
    abstract fun reservationDao(): ReservationDao
    abstract fun adminDashboardDao(): AdminDashboardDao
    abstract fun adminConfigurationDao(): AdminConfigurationDao
    abstract fun adminActivityDao(): AdminActivityDao

    companion object {
        private val MIGRATION_1_2 = object : Migration(1, 2) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL("CREATE TABLE IF NOT EXISTS reservations (id TEXT NOT NULL PRIMARY KEY, prosumerId TEXT NOT NULL, microgridNodeId TEXT NOT NULL, energySlotId TEXT NOT NULL, energyAmount REAL NOT NULL, reservationDate TEXT NOT NULL, startTime TEXT NOT NULL, endTime TEXT NOT NULL, status TEXT NOT NULL, createdAt TEXT NOT NULL, updatedAt TEXT NOT NULL, cachedAt INTEGER NOT NULL)")
            }
        }

        /**
         * Adds the M4 administration cache. The columns are declared non-null
         * without a default, which is why the tables are created empty first and
         * the cache is only ever written through a complete entity.
         */
        private val MIGRATION_2_3 = object : Migration(2, 3) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    "CREATE TABLE IF NOT EXISTS admin_dashboard_cache (" +
                        "cacheKey TEXT NOT NULL PRIMARY KEY, " +
                        "platformName TEXT NOT NULL, " +
                        "maintenanceMode INTEGER NOT NULL, " +
                        "totalUsers INTEGER NOT NULL, " +
                        "activeUsers INTEGER NOT NULL, " +
                        "inactiveUsers INTEGER NOT NULL, " +
                        "suspendedUsers INTEGER NOT NULL, " +
                        "pendingUsers INTEGER NOT NULL, " +
                        "microgridCount INTEGER NOT NULL, " +
                        "activeMicrogridCount INTEGER NOT NULL, " +
                        "totalCapacity REAL NOT NULL, " +
                        "totalAvailableCapacity REAL NOT NULL, " +
                        "reservationCount INTEGER NOT NULL, " +
                        "pendingReservationCount INTEGER NOT NULL, " +
                        "transactionCount INTEGER NOT NULL, " +
                        "completedTransactionCount INTEGER NOT NULL, " +
                        "microgridsLive INTEGER NOT NULL, " +
                        "reservationsLive INTEGER NOT NULL, " +
                        "transactionsLive INTEGER NOT NULL, " +
                        "databaseStatus TEXT NOT NULL, " +
                        "generatedAt TEXT NOT NULL, " +
                        "cachedAt INTEGER NOT NULL)"
                )
                db.execSQL(
                    "CREATE TABLE IF NOT EXISTS admin_configuration_cache (" +
                        "cacheKey TEXT NOT NULL PRIMARY KEY, " +
                        "platformName TEXT NOT NULL, " +
                        "platformDescription TEXT NOT NULL, " +
                        "maintenanceMode INTEGER NOT NULL, " +
                        "maintenanceMessage TEXT, " +
                        "allowRegistration INTEGER NOT NULL, " +
                        "sessionTimeoutMinutes INTEGER NOT NULL, " +
                        "maxLoginAttempts INTEGER NOT NULL, " +
                        "defaultPageSize INTEGER NOT NULL, " +
                        "updatedAt TEXT NOT NULL, " +
                        "cachedAt INTEGER NOT NULL)"
                )
                db.execSQL(
                    "CREATE TABLE IF NOT EXISTS admin_activity_cache (" +
                        "queryKey TEXT NOT NULL, " +
                        "id TEXT NOT NULL, " +
                        "userId TEXT, " +
                        "userName TEXT NOT NULL, " +
                        "role TEXT, " +
                        "action TEXT NOT NULL, " +
                        "module TEXT NOT NULL, " +
                        "description TEXT NOT NULL, " +
                        "entityType TEXT, " +
                        "entityId TEXT, " +
                        "ipAddress TEXT, " +
                        "timestamp TEXT NOT NULL, " +
                        "status TEXT NOT NULL, " +
                        "cachedAt INTEGER NOT NULL, " +
                        "PRIMARY KEY (queryKey, id))"
                )
            }
        }

        @Volatile
        private var INSTANCE: AppDatabase? = null

        fun getDatabase(context: Context): AppDatabase {
            return INSTANCE ?: synchronized(this) {
                val instance = Room.databaseBuilder(
                    context.applicationContext,
                    AppDatabase::class.java,
                    "smart_microgrid_db"
                )
                    .addMigrations(MIGRATION_1_2, MIGRATION_2_3)
                    .fallbackToDestructiveMigration()
                    .build()
                INSTANCE = instance
                instance
            }
        }
    }
}
