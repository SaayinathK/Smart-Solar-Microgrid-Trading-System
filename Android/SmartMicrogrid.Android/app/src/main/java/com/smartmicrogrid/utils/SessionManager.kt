package com.smartmicrogrid.utils

import android.content.Context
import android.content.SharedPreferences
import com.google.gson.Gson
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.User

/**
 * SharedPreferences-based session manager for JWT token and user data.
 * Follows the same pattern as the Web app's SessionManager (js/common/session.js).
 */
object SessionManager {

    private lateinit var prefs: SharedPreferences
    private val gson = Gson()

    fun init(context: Context) {
        prefs = context.getSharedPreferences(Constants.PREF_NAME, Context.MODE_PRIVATE)
        // Restore JWT token into RetrofitClient if a session already exists
        val token = getToken()
        if (!token.isNullOrEmpty()) {
            RetrofitClient.setJwtToken(token)
        }
    }

    fun setSession(token: String, user: User) {
        prefs.edit()
            .putString(Constants.KEY_JWT_TOKEN, token)
            .putString(Constants.KEY_USER_ROLE, user.role)
            .putString(Constants.KEY_USER_NAME, "${user.firstName} ${user.lastName}")
            .putString(Constants.KEY_USER_EMAIL, user.email)
            .putString(Constants.KEY_USER_JSON, gson.toJson(user))
            .apply()

        RetrofitClient.setJwtToken(token)
    }

    fun getToken(): String? = prefs.getString(Constants.KEY_JWT_TOKEN, null)

    fun getUser(): User? {
        val json = prefs.getString(Constants.KEY_USER_JSON, null) ?: return null
        return try {
            gson.fromJson(json, User::class.java)
        } catch (e: Exception) {
            null
        }
    }

    fun getUserName(): String = prefs.getString(Constants.KEY_USER_NAME, "User") ?: "User"

    fun getUserRole(): String = prefs.getString(Constants.KEY_USER_ROLE, "") ?: ""

    fun isLoggedIn(): Boolean = !getToken().isNullOrEmpty()

    fun updateUser(user: User) {
        prefs.edit()
            .putString(Constants.KEY_USER_ROLE, user.role)
            .putString(Constants.KEY_USER_NAME, "${user.firstName} ${user.lastName}")
            .putString(Constants.KEY_USER_EMAIL, user.email)
            .putString(Constants.KEY_USER_JSON, gson.toJson(user))
            .apply()
    }

    fun logout() {
        prefs.edit().clear().apply()
        RetrofitClient.setJwtToken(null)
    }
}
