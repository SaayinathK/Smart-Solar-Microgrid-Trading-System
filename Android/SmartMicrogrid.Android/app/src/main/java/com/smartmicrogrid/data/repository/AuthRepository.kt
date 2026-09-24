package com.smartmicrogrid.data.repository

import com.smartmicrogrid.data.remote.ApiService
import com.smartmicrogrid.models.*
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class AuthRepository(private val apiService: ApiService) {

    suspend fun login(email: String, password: String): Result<LoginResponse> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.login(LoginRequest(email, password))
                if (response.isSuccessful && response.body()?.success == true && response.body()?.data != null) {
                    val loginData = response.body()!!.data!!
                    // Save session (JWT + User)
                    SessionManager.setSession(loginData.token, loginData.user)
                    Result.success(loginData)
                } else {
                    val msg = response.body()?.message ?: "Login failed. Check your credentials."
                    Result.failure(Exception(msg))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Unable to connect to the server. Check network."))
            }
        }
    }

    suspend fun register(request: RegisterRequest): Result<User> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.register(request)
                if (response.isSuccessful && response.body()?.success == true && response.body()?.data != null) {
                    Result.success(response.body()!!.data!!)
                } else {
                    val msg = response.body()?.message ?: "Registration failed."
                    Result.failure(Exception(msg))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Unable to connect to the server. Check network."))
            }
        }
    }

    suspend fun getProfile(): Result<User> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.getCurrentUser()
                if (response.isSuccessful && response.body()?.success == true && response.body()?.data != null) {
                    val user = response.body()!!.data!!
                    SessionManager.updateUser(user)
                    Result.success(user)
                } else {
                    Result.failure(Exception(response.body()?.message ?: "Failed to load profile."))
                }
            } catch (e: Exception) {
                // Fallback to cached user
                val cached = SessionManager.getUser()
                if (cached != null) Result.success(cached)
                else Result.failure(Exception("Unable to load profile."))
            }
        }
    }

    suspend fun updateProfile(request: UpdateProfileRequest): Result<User> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.updateCurrentUser(request)
                if (response.isSuccessful && response.body()?.success == true && response.body()?.data != null) {
                    val user = response.body()!!.data!!
                    SessionManager.updateUser(user)
                    Result.success(user)
                } else {
                    Result.failure(Exception(response.body()?.message ?: "Update failed."))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Unable to connect to the server."))
            }
        }
    }

    suspend fun changePassword(request: ChangePasswordRequest): Result<Boolean> {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.changePassword(request)
                if (response.isSuccessful && response.body()?.success == true) {
                    Result.success(true)
                } else {
                    Result.failure(Exception(response.body()?.message ?: "Password change failed."))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Unable to connect to the server."))
            }
        }
    }

    fun logout() {
        SessionManager.logout()
    }
}
