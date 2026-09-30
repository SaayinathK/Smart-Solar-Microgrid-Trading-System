package com.smartmicrogrid.data.remote

import com.smartmicrogrid.models.*
import retrofit2.Response
import retrofit2.http.*

interface ApiService {

    // Microgrid Endpoints
    @GET("microgrids")
    suspend fun getMicrogrids(
        @Query("status") status: String? = null,
        @Query("isActive") isActive: Boolean? = null,
        @Query("location") location: String? = null,
        @Query("search") search: String? = null
    ): Response<ApiResponse<List<Microgrid>>>

    @GET("microgrids/{id}")
    suspend fun getMicrogridById(
        @Path("id") id: String
    ): Response<ApiResponse<Microgrid>>

    @PATCH("microgrids/{id}/status")
    suspend fun updateMicrogridStatus(
        @Path("id") id: String,
        @Body body: Map<String, String>
    ): Response<ApiResponse<Unit>>

    @PUT("microgrids/{id}/capacity")
    suspend fun updateCapacity(
        @Path("id") id: String,
        @Body request: UpdateCapacityRequest
    ): Response<ApiResponse<Unit>>

    @PUT("microgrids/{id}/battery")
    suspend fun updateBattery(
        @Path("id") id: String,
        @Body request: UpdateBatteryRequest
    ): Response<ApiResponse<Unit>>

    // Energy Slot Endpoints
    @GET("energy-slots")
    suspend fun getEnergySlots(
        @Query("microgridId") microgridId: String? = null,
        @Query("status") status: String? = null
    ): Response<ApiResponse<List<EnergySlot>>>

    @POST("energy-slots")
    suspend fun createEnergySlot(
        @Body request: CreateEnergySlotRequest
    ): Response<ApiResponse<EnergySlot>>

    @PATCH("energy-slots/{id}/status")
    suspend fun updateSlotStatus(
        @Path("id") id: String,
        @Body body: Map<String, String>
    ): Response<ApiResponse<Unit>>

    // Energy Availability Endpoint (Public/Prosumer)
    @GET("energy-availability")
    suspend fun getEnergyAvailability(
        @Query("location") location: String? = null,
        @Query("minimumEnergy") minimumEnergy: Double? = null
    ): Response<ApiResponse<List<EnergyAvailabilitySlot>>>

    // ── M3 Transaction Endpoints ──

    @GET("transactions")
    suspend fun getTransactions(): Response<ApiResponse<List<Transaction>>>

    @GET("transactions/{id}")
    suspend fun getTransactionById(
        @Path("id") id: String
    ): Response<ApiResponse<Transaction>>

    @POST("transactions")
    suspend fun createTransaction(
        @Body request: CreateTransactionRequest
    ): Response<ApiResponse<Transaction>>

    @POST("transactions/{id}/generate-qr")
    suspend fun generateTransactionQr(
        @Path("id") id: String
    ): Response<ApiResponse<GenerateQrResponse>>

    @POST("transactions/{id}/verify")
    suspend fun verifyTransaction(
        @Path("id") id: String,
        @Body request: VerifyTransactionRequest
    ): Response<ApiResponse<Transaction>>

    @POST("transactions/{id}/complete")
    suspend fun completeTransaction(
        @Path("id") id: String,
        @Body request: CompleteTransactionRequest
    ): Response<ApiResponse<Transaction>>

    @PATCH("transactions/{id}/status")
    suspend fun updateTransactionStatus(
        @Path("id") id: String,
        @Query("status") status: String
    ): Response<ApiResponse<Transaction>>
    // ── Component 2 (M2) Reservation Endpoints ──
    @GET("reservations")
    suspend fun getReservations(
        @Query("status") status: String? = null,
        @Query("nodeId") nodeId: String? = null,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50
    ): Response<ApiResponse<List<Reservation>>>

    @GET("reservations/summary")
    suspend fun getReservationSummary(
        @Query("nic") nic: String? = null
    ): Response<ApiResponse<ReservationSummary>>

    @GET("reservations/{id}")
    suspend fun getReservationById(
        @Path("id") id: String
    ): Response<ApiResponse<Reservation>>

    @POST("reservations")
    suspend fun createReservation(
        @Body request: CreateReservationRequest
    ): Response<ApiResponse<Reservation>>

    @PUT("reservations/{id}")
    suspend fun updateReservation(
        @Path("id") id: String,
        @Body request: CreateReservationRequest
    ): Response<ApiResponse<Reservation>>

    @PATCH("reservations/{id}/cancel")
    suspend fun cancelReservation(
        @Path("id") id: String
    ): Response<ApiResponse<Reservation>>

    @PATCH("reservations/{id}/approve")
    suspend fun approveReservation(
        @Path("id") id: String
    ): Response<ApiResponse<Reservation>>

    @PATCH("reservations/{id}/reject")
    suspend fun rejectReservation(
        @Path("id") id: String,
        @Body body: Map<String, String>
    ): Response<ApiResponse<Reservation>>

    @PATCH("reservations/{id}/complete")
    suspend fun completeReservation(
        @Path("id") id: String
    ): Response<ApiResponse<Reservation>>

    // ── Auth Endpoints ──

    @POST("auth/login")
    suspend fun login(
        @Body request: LoginRequest
    ): Response<ApiResponse<LoginResponse>>

    @POST("auth/register")
    suspend fun register(
        @Body request: RegisterRequest
    ): Response<ApiResponse<User>>

    @POST("auth/change-password")
    suspend fun changePassword(
        @Body request: ChangePasswordRequest
    ): Response<ApiResponse<Unit>>

    // ── User Profile Endpoints ──

    @GET("users/me")
    suspend fun getCurrentUser(): Response<ApiResponse<User>>

    @PUT("users/me")
    suspend fun updateCurrentUser(
        @Body request: UpdateProfileRequest
    ): Response<ApiResponse<User>>

    // ── M4 Platform Administration Endpoints (Admin only) ──

    @GET("admin/dashboard")
    suspend fun getAdminDashboard(): Response<ApiResponse<AdminDashboard>>

    @GET("admin/system/health")
    suspend fun getSystemHealth(): Response<ApiResponse<SystemHealth>>

    @GET("admin/system/configuration")
    suspend fun getSystemConfiguration(): Response<ApiResponse<SystemConfiguration>>

    @PUT("admin/system/configuration")
    suspend fun updateSystemConfiguration(
        @Body request: UpdateConfigurationRequest
    ): Response<ApiResponse<SystemConfiguration>>

    @PATCH("admin/system/configuration/maintenance")
    suspend fun setMaintenanceMode(
        @Body request: MaintenanceModeRequest
    ): Response<ApiResponse<SystemConfiguration>>

    @PATCH("admin/system/configuration/registration")
    suspend fun setRegistrationMode(
        @Body request: RegistrationModeRequest
    ): Response<ApiResponse<SystemConfiguration>>

    @GET("admin/activity")
    suspend fun getSystemActivity(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 20,
        @Query("userId") userId: String? = null,
        @Query("module") module: String? = null,
        @Query("action") action: String? = null,
        @Query("status") status: String? = null,
        @Query("from") from: String? = null,
        @Query("to") to: String? = null
    ): Response<ApiResponse<PagedResult<SystemActivity>>>

    @GET("admin/reports/users")
    suspend fun getUserReport(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
        @Query("role") role: String? = null,
        @Query("status") status: String? = null
    ): Response<ApiResponse<UserReport>>

    @GET("admin/reports/roles")
    suspend fun getRoleReport(): Response<ApiResponse<List<RoleDistribution>>>

    @GET("admin/reports/activity")
    suspend fun getActivityReport(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null
    ): Response<ApiResponse<ActivityReport>>

    @GET("admin/reports/platform")
    suspend fun getPlatformReport(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null
    ): Response<ApiResponse<PlatformReport>>

    // ── M4 User Lifecycle Endpoints (Admin only) ──

    @GET("users")
    suspend fun getAllUsers(
        @Query("search") search: String? = null,
        @Query("role") role: String? = null,
        @Query("accountStatus") accountStatus: String? = null
    ): Response<ApiResponse<List<User>>>

    @PATCH("users/{id}/account-status")
    suspend fun updateUserAccountStatus(
        @Path("id") id: String,
        @Body body: Map<String, String>
    ): Response<ApiResponse<User>>
}