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
    suspend fun getMicrogridById(@Path("id") id: String): Response<ApiResponse<Microgrid>>

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
    suspend fun createEnergySlot(@Body request: CreateEnergySlotRequest): Response<ApiResponse<EnergySlot>>

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
    suspend fun login(@Body request: LoginRequest): Response<ApiResponse<LoginResponse>>

    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<ApiResponse<User>>

    @POST("auth/change-password")
    suspend fun changePassword(@Body request: ChangePasswordRequest): Response<ApiResponse<Unit>>

    // ── User Profile Endpoints ──

    @GET("users/me")
    suspend fun getCurrentUser(): Response<ApiResponse<User>>

    @PUT("users/me")
    suspend fun updateCurrentUser(@Body request: UpdateProfileRequest): Response<ApiResponse<User>>
}
