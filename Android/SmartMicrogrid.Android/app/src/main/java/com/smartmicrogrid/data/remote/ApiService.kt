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
    ): Response<ApiResponse<Transaction>>

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
}