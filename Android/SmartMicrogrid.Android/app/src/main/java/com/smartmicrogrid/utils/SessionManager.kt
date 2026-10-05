package com.smartmicrogrid.utils

import android.content.Context
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.local.LoginSessionStore
import com.smartmicrogrid.models.User
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Deferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.async

/** Login details persist in Room/SQLite; SharedPreferences is read only for migration. */
object SessionManager {
    private lateinit var store: LoginSessionStore
    private lateinit var restoration: Deferred<Unit>
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    @Synchronized
    fun init(context: Context) {
        if (::store.isInitialized) return
        val app = context.applicationContext
        store = LoginSessionStore(
            AppDatabase.getDatabase(app).loginSessionDao(),
            readLegacy = {
                val prefs = app.getSharedPreferences(Constants.PREF_NAME, Context.MODE_PRIVATE)
                val token = prefs.getString(Constants.KEY_JWT_TOKEN, null)
                val user = prefs.getString(Constants.KEY_USER_JSON, null)?.let(LoginSessionStore::decodeUser)
                if (!token.isNullOrBlank() && user != null && !user.id.isNullOrBlank()) LoginSessionStore.fromUser(token, user) else null
            },
            clearLegacy = {
                val prefs = app.getSharedPreferences(Constants.PREF_NAME, Context.MODE_PRIVATE)
                check(prefs.edit().remove(Constants.KEY_JWT_TOKEN).remove(Constants.KEY_USER_JSON)
                    .remove(Constants.KEY_USER_ROLE).remove(Constants.KEY_USER_NAME)
                    .remove(Constants.KEY_USER_EMAIL).commit()) { "Unable to migrate saved login details" }
            }
        )
        restoration = scope.async { store.initialize() }
    }

    suspend fun awaitReady() = restoration.await()
    fun isReady(): Boolean = ::store.isInitialized && store.initialized
    fun getToken(): String? = if (isReady()) store.getToken() else null
    fun getUser(): User? = if (isReady()) store.getUser() else null
    fun getUserName(): String = if (isReady()) store.getUserName() else "User"
    fun getUserRole(): String = if (isReady()) store.getUserRole() else ""
    fun isLoggedIn(): Boolean = !getToken().isNullOrBlank()

    suspend fun setSession(token: String, user: User) { awaitReady(); store.setSession(token, user) }
    suspend fun updateUser(user: User, expectedToken: String?) { awaitReady(); store.updateUser(user, expectedToken) }
    suspend fun logout() { awaitReady(); store.logout() }
}
