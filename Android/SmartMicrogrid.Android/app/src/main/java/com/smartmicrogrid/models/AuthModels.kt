package com.smartmicrogrid.models

import com.google.gson.annotations.SerializedName

// ── Auth Request/Response Models ──

data class LoginRequest(
    @SerializedName("email") val email: String,
    @SerializedName("password") val password: String
)

data class LoginResponse(
    @SerializedName("token") val token: String,
    @SerializedName("tokenType") val tokenType: String = "Bearer",
    @SerializedName("expiresAt") val expiresAt: String = "",
    @SerializedName("user") val user: User
)

data class RegisterRequest(
    @SerializedName("firstName") val firstName: String,
    @SerializedName("lastName") val lastName: String,
    @SerializedName("email") val email: String,
    @SerializedName("phoneNumber") val phoneNumber: String,
    @SerializedName("password") val password: String,
    @SerializedName("confirmPassword") val confirmPassword: String
)

// ── User Model ──

data class User(
    @SerializedName("id") val id: String = "",
    @SerializedName("firstName") val firstName: String = "",
    @SerializedName("lastName") val lastName: String = "",
    @SerializedName("email") val email: String = "",
    @SerializedName("phoneNumber") val phoneNumber: String = "",
    @SerializedName("role") val role: String = "",
    @SerializedName("isActive") val isActive: Boolean = true,
    @SerializedName("createdAt") val createdAt: String = "",
    @SerializedName("updatedAt") val updatedAt: String = ""
)

// ── User Update Models ──

data class UpdateProfileRequest(
    @SerializedName("firstName") val firstName: String,
    @SerializedName("lastName") val lastName: String,
    @SerializedName("phoneNumber") val phoneNumber: String
)

data class ChangePasswordRequest(
    @SerializedName("currentPassword") val currentPassword: String,
    @SerializedName("newPassword") val newPassword: String,
    @SerializedName("confirmNewPassword") val confirmNewPassword: String
)
