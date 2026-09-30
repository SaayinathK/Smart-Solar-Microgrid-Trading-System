package com.smartmicrogrid.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey
import com.smartmicrogrid.models.AdminDashboard
import com.smartmicrogrid.models.SystemActivity
import com.smartmicrogrid.models.SystemConfiguration

/**
 * Cached administration dashboard. The snapshot is flattened into typed columns
 * rather than a JSON blob so it stays readable in a database dump and does not
 * need a TypeConverter. Only the last successful snapshot is kept, so an
 * operator can still read platform totals when the network is unavailable.
 */
@Entity(tableName = "admin_dashboard_cache")
data class AdminDashboardCache(
    @PrimaryKey val cacheKey: String = SINGLETON_KEY,
    val platformName: String,
    val maintenanceMode: Boolean,
    val totalUsers: Long,
    val activeUsers: Long,
    val inactiveUsers: Long,
    val suspendedUsers: Long,
    val pendingUsers: Long,
    val microgridCount: Long,
    val activeMicrogridCount: Long,
    val totalCapacity: Double,
    val totalAvailableCapacity: Double,
    val reservationCount: Long,
    val pendingReservationCount: Long,
    val transactionCount: Long,
    val completedTransactionCount: Long,
    val microgridsLive: Boolean,
    val reservationsLive: Boolean,
    val transactionsLive: Boolean,
    val databaseStatus: String,
    val generatedAt: String,
    val cachedAt: Long = System.currentTimeMillis()
) {
    fun toDomain() = AdminDashboard(
        platformName = platformName,
        generatedAt = generatedAt,
        maintenanceMode = maintenanceMode,
        users = com.smartmicrogrid.models.DashboardSummary(
            totalUsers = totalUsers,
            activeUsers = activeUsers,
            inactiveUsers = inactiveUsers,
            suspendedUsers = suspendedUsers,
            pendingUsers = pendingUsers
        ),
        platform = com.smartmicrogrid.models.PlatformOverview(
            microgridCount = microgridCount,
            activeMicrogridCount = activeMicrogridCount,
            totalCapacity = totalCapacity,
            totalAvailableCapacity = totalAvailableCapacity,
            reservationCount = reservationCount,
            pendingReservationCount = pendingReservationCount,
            transactionCount = transactionCount,
            completedTransactionCount = completedTransactionCount
        ),
        health = com.smartmicrogrid.models.SystemHealth(database = databaseStatus)
    )

    companion object {
        const val SINGLETON_KEY = "admin-dashboard"

        fun fromDomain(dashboard: AdminDashboard) = AdminDashboardCache(
            platformName = dashboard.platformName,
            maintenanceMode = dashboard.maintenanceMode,
            totalUsers = dashboard.users.totalUsers,
            activeUsers = dashboard.users.activeUsers,
            inactiveUsers = dashboard.users.inactiveUsers,
            suspendedUsers = dashboard.users.suspendedUsers,
            pendingUsers = dashboard.users.pendingUsers,
            microgridCount = dashboard.platform.microgridCount,
            activeMicrogridCount = dashboard.platform.activeMicrogridCount,
            totalCapacity = dashboard.platform.totalCapacity,
            totalAvailableCapacity = dashboard.platform.totalAvailableCapacity,
            reservationCount = dashboard.platform.reservationCount,
            pendingReservationCount = dashboard.platform.pendingReservationCount,
            transactionCount = dashboard.platform.transactionCount,
            completedTransactionCount = dashboard.platform.completedTransactionCount,
            microgridsLive = dashboard.platform.microgrids.isLive,
            reservationsLive = dashboard.platform.reservations.isLive,
            transactionsLive = dashboard.platform.transactions.isLive,
            databaseStatus = dashboard.health.database,
            generatedAt = dashboard.generatedAt
        )
    }
}

/** Cached singleton system configuration, including the two feature toggles. */
@Entity(tableName = "admin_configuration_cache")
data class AdminConfigurationCache(
    @PrimaryKey val cacheKey: String = SINGLETON_KEY,
    val platformName: String,
    val platformDescription: String,
    val maintenanceMode: Boolean,
    val maintenanceMessage: String?,
    val allowRegistration: Boolean,
    val sessionTimeoutMinutes: Int,
    val maxLoginAttempts: Int,
    val defaultPageSize: Int,
    val updatedAt: String,
    val cachedAt: Long = System.currentTimeMillis()
) {
    companion object {
        const val SINGLETON_KEY = "system-configuration"

        fun fromDomain(config: SystemConfiguration) = AdminConfigurationCache(
            platformName = config.platformName,
            platformDescription = config.platformDescription,
            maintenanceMode = config.maintenanceMode,
            maintenanceMessage = config.maintenanceMessage,
            allowRegistration = config.allowRegistration,
            sessionTimeoutMinutes = config.sessionTimeoutMinutes,
            maxLoginAttempts = config.maxLoginAttempts,
            defaultPageSize = config.defaultPageSize,
            updatedAt = config.updatedAt
        )

        fun toDomain(entity: AdminConfigurationCache) = SystemConfiguration(
            platformName = entity.platformName,
            platformDescription = entity.platformDescription,
            maintenanceMode = entity.maintenanceMode,
            maintenanceMessage = entity.maintenanceMessage,
            allowRegistration = entity.allowRegistration,
            sessionTimeoutMinutes = entity.sessionTimeoutMinutes,
            maxLoginAttempts = entity.maxLoginAttempts,
            defaultPageSize = entity.defaultPageSize,
            createdAt = "",
            updatedAt = entity.updatedAt,
            updatedBy = null
        )
    }
}

/**
 * Cached audit trail. Keyed by the filter signature so different filter
 * combinations do not overwrite each other.
 */
@Entity(tableName = "admin_activity_cache", primaryKeys = ["queryKey", "id"])
data class AdminActivityCache(
    val queryKey: String,
    val id: String,
    val userId: String?,
    val userName: String,
    val role: String?,
    val action: String,
    val module: String,
    val description: String,
    val entityType: String?,
    val entityId: String?,
    val ipAddress: String?,
    val timestamp: String,
    val status: String,
    val cachedAt: Long = System.currentTimeMillis()
) {
    fun toDomain() = SystemActivity(
        id = id,
        userId = userId,
        userName = userName,
        role = role,
        action = action,
        module = module,
        description = description,
        entityType = entityType,
        entityId = entityId,
        ipAddress = ipAddress,
        timestamp = timestamp,
        status = status
    )

    companion object {
        fun fromDomain(queryKey: String, activity: SystemActivity) = AdminActivityCache(
            queryKey = queryKey,
            id = activity.id,
            userId = activity.userId,
            userName = activity.userName,
            role = activity.role,
            action = activity.action,
            module = activity.module,
            description = activity.description,
            entityType = activity.entityType,
            entityId = activity.entityId,
            ipAddress = activity.ipAddress,
            timestamp = activity.timestamp,
            status = activity.status
        )
    }
}
