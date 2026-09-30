package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

// ── M4 Platform Administration models ──
// These mirror the ApiResponse payloads exposed by the /api/admin endpoints.
// Property names match the camelCase JSON emitted by the ASP.NET backend.

data class DashboardSummary(
    @SerializedName("totalUsers") val totalUsers: Long = 0,
    @SerializedName("activeUsers") val activeUsers: Long = 0,
    @SerializedName("inactiveUsers") val inactiveUsers: Long = 0,
    @SerializedName("suspendedUsers") val suspendedUsers: Long = 0,
    @SerializedName("pendingUsers") val pendingUsers: Long = 0
)

data class RoleDistribution(
    @SerializedName("role") val role: String = "",
    @SerializedName("description") val description: String = "",
    @SerializedName("userCount") val userCount: Long = 0,
    @SerializedName("activeUserCount") val activeUserCount: Long = 0
)

/**
 * A statistic contributed by M1/M2/M3. [status] is "Live" when the owning
 * component answered and "Unavailable" when it did not, so a broken component
 * degrades a single tile instead of failing the whole screen.
 */
data class ComponentStat(
    @SerializedName("count") val count: Long = 0,
    @SerializedName("total") val total: Double = 0.0,
    @SerializedName("available") val available: Double = 0.0,
    @SerializedName("unit") val unit: String = "",
    @SerializedName("status") val status: String = "Unavailable",
    @SerializedName("note") val note: String = ""
) {
    val isLive: Boolean get() = status == "Live"
}

data class PlatformOverview(
    @SerializedName("microgridCount") val microgridCount: Long = 0,
    @SerializedName("activeMicrogridCount") val activeMicrogridCount: Long = 0,
    @SerializedName("totalCapacity") val totalCapacity: Double = 0.0,
    @SerializedName("totalAvailableCapacity") val totalAvailableCapacity: Double = 0.0,
    @SerializedName("reservationCount") val reservationCount: Long = 0,
    @SerializedName("pendingReservationCount") val pendingReservationCount: Long = 0,
    @SerializedName("transactionCount") val transactionCount: Long = 0,
    @SerializedName("completedTransactionCount") val completedTransactionCount: Long = 0,
    @SerializedName("microgrids") val microgrids: ComponentStat = ComponentStat(),
    @SerializedName("reservations") val reservations: ComponentStat = ComponentStat(),
    @SerializedName("transactions") val transactions: ComponentStat = ComponentStat()
)

data class SystemActivity(
    @SerializedName("id") val id: String = "",
    @SerializedName("userId") val userId: String? = null,
    @SerializedName("userName") val userName: String = "",
    @SerializedName("role") val role: String? = null,
    @SerializedName("action") val action: String = "",
    @SerializedName("module") val module: String = "",
    @SerializedName("description") val description: String = "",
    @SerializedName("entityType") val entityType: String? = null,
    @SerializedName("entityId") val entityId: String? = null,
    @SerializedName("ipAddress") val ipAddress: String? = null,
    @SerializedName("timestamp") val timestamp: String = "",
    @SerializedName("status") val status: String = ""
)

data class ActivitySummary(
    @SerializedName("recentActivity") val recentActivity: List<SystemActivity> = emptyList(),
    @SerializedName("totalEvents") val totalEvents: Long = 0
)

data class SystemHealth(
    @SerializedName("api") val api: String = "Unknown",
    @SerializedName("database") val database: String = "Unknown",
    @SerializedName("databaseDetail") val databaseDetail: String = "",
    @SerializedName("authentication") val authentication: String = "Unknown",
    @SerializedName("server") val server: String = "Unknown",
    @SerializedName("environment") val environment: String = "",
    @SerializedName("version") val version: String = "",
    @SerializedName("serverTime") val serverTime: String = "",
    @SerializedName("databaseLatencyMs") val databaseLatencyMs: Double = 0.0,
    @SerializedName("processMemoryBytes") val processMemoryBytes: Long = 0,
    // ASP.NET serialises TimeSpan as "hh:mm:ss"; the app only needs the day part.
    @SerializedName("uptime") val uptime: String = "",
    @SerializedName("jwtIssuer") val jwtIssuer: String = ""
) {
    val isDatabaseHealthy: Boolean get() = database.equals("Healthy", ignoreCase = true)
}

data class AdminDashboard(
    @SerializedName("platformName") val platformName: String = "",
    @SerializedName("generatedAt") val generatedAt: String = "",
    @SerializedName("maintenanceMode") val maintenanceMode: Boolean = false,
    @SerializedName("users") val users: DashboardSummary = DashboardSummary(),
    @SerializedName("roleDistribution") val roleDistribution: List<RoleDistribution> = emptyList(),
    @SerializedName("platform") val platform: PlatformOverview = PlatformOverview(),
    @SerializedName("activity") val activity: ActivitySummary = ActivitySummary(),
    @SerializedName("health") val health: SystemHealth = SystemHealth()
)

data class SystemConfiguration(
    @SerializedName("platformName") val platformName: String = "",
    @SerializedName("platformDescription") val platformDescription: String = "",
    @SerializedName("maintenanceMode") val maintenanceMode: Boolean = false,
    @SerializedName("maintenanceMessage") val maintenanceMessage: String? = null,
    @SerializedName("allowRegistration") val allowRegistration: Boolean = true,
    @SerializedName("sessionTimeoutMinutes") val sessionTimeoutMinutes: Int = 480,
    @SerializedName("maxLoginAttempts") val maxLoginAttempts: Int = 5,
    @SerializedName("defaultPageSize") val defaultPageSize: Int = 20,
    @SerializedName("createdAt") val createdAt: String = "",
    @SerializedName("updatedAt") val updatedAt: String = "",
    @SerializedName("updatedBy") val updatedBy: String? = null
)

/** Shared pagination envelope for the activity endpoint. */
data class PagedResult<T>(
    @SerializedName("items") val items: List<T> = emptyList(),
    @SerializedName("page") val page: Int = 1,
    @SerializedName("pageSize") val pageSize: Int = 20,
    @SerializedName("totalItems") val totalItems: Long = 0,
    @SerializedName("totalPages") val totalPages: Int = 0
)

data class ActivityReport(
    @SerializedName("totalEvents") val totalEvents: Long = 0,
    @SerializedName("successEvents") val successEvents: Long = 0,
    @SerializedName("failureEvents") val failureEvents: Long = 0,
    @SerializedName("byAction") val byAction: Map<String, Long> = emptyMap(),
    @SerializedName("byModule") val byModule: Map<String, Long> = emptyMap(),
    @SerializedName("recent") val recent: List<SystemActivity> = emptyList()
)

data class UserSummaryRow(
    @SerializedName("role") val role: String = "",
    @SerializedName("status") val status: String = "",
    @SerializedName("count") val count: Long = 0
)

data class UserReport(
    @SerializedName("generatedAt") val generatedAt: String = "",
    @SerializedName("from") val from: String? = null,
    @SerializedName("to") val to: String? = null,
    @SerializedName("totalUsers") val totalUsers: Long = 0,
    @SerializedName("activeUsers") val activeUsers: Long = 0,
    @SerializedName("inactiveUsers") val inactiveUsers: Long = 0,
    @SerializedName("suspendedUsers") val suspendedUsers: Long = 0,
    @SerializedName("pendingUsers") val pendingUsers: Long = 0,
    @SerializedName("newUsersInPeriod") val newUsersInPeriod: Long = 0,
    @SerializedName("byStatus") val byStatus: List<UserSummaryRow> = emptyList(),
    @SerializedName("byRole") val byRole: List<RoleDistribution> = emptyList()
)

data class PlatformReport(
    @SerializedName("generatedAt") val generatedAt: String = "",
    @SerializedName("users") val users: DashboardSummary = DashboardSummary(),
    @SerializedName("roleDistribution") val roleDistribution: List<RoleDistribution> = emptyList(),
    @SerializedName("platform") val platform: PlatformOverview = PlatformOverview(),
    @SerializedName("activity") val activity: ActivityReport = ActivityReport()
)

// ── Write requests ──

data class UpdateConfigurationRequest(
    @SerializedName("platformName") val platformName: String,
    @SerializedName("platformDescription") val platformDescription: String,
    @SerializedName("maintenanceMessage") val maintenanceMessage: String?,
    @SerializedName("sessionTimeoutMinutes") val sessionTimeoutMinutes: Int,
    @SerializedName("maxLoginAttempts") val maxLoginAttempts: Int,
    @SerializedName("defaultPageSize") val defaultPageSize: Int
)

data class MaintenanceModeRequest(
    @SerializedName("maintenanceMode") val maintenanceMode: Boolean,
    @SerializedName("maintenanceMessage") val maintenanceMessage: String?
)

data class RegistrationModeRequest(
    @SerializedName("allowRegistration") val allowRegistration: Boolean
)
