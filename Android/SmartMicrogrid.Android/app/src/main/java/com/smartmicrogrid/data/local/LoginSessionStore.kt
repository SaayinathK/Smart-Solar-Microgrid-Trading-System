package com.smartmicrogrid.data.local

import com.google.gson.Gson
import com.smartmicrogrid.models.User
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext

/** SQLite is authoritative. The snapshot only serves synchronous UI reads. */
class LoginSessionStore(
    private val dao: LoginSessionDao,
    private val readLegacy: () -> LoginSessionEntity?,
    private val clearLegacy: () -> Unit
) {
    private val mutex = Mutex()
    @Volatile private var session: LoginSessionEntity? = null
    @Volatile var initialized = false
        private set

    suspend fun initialize() = withContext(Dispatchers.IO) {
        mutex.withLock {
            if (initialized) return@withLock
            val saved = dao.getSession()
            val restored = saved ?: readLegacy()
            val valid = restored?.takeIf { it.token.isNotBlank() && decodeUser(it.userJson)?.id == it.userId && it.userId.isNotBlank() }
            if (valid != null && saved == null) dao.saveSession(valid)
            if (saved != null && valid == null) dao.clearSession()
            // Never remove the old session until its SQLite write has succeeded.
            clearLegacy()
            session = valid
            initialized = true
        }
    }

    fun getToken(): String? = session?.token
    fun getUser(): User? = session?.let { decodeUser(it.userJson) }
    fun getUserRole(): String = session?.role.orEmpty()
    fun getUserName(): String = session?.displayName ?: "User"

    suspend fun setSession(token: String, user: User) = withContext(Dispatchers.IO + NonCancellable) {
        mutex.withLock {
            check(initialized)
            require(token.isNotBlank() && user.id.isNotBlank()) { "Invalid login session" }
            val next = fromUser(token, user)
            dao.saveSession(next)
            session = next
        }
    }

    suspend fun updateUser(user: User, expectedToken: String?) = withContext(Dispatchers.IO + NonCancellable) {
        mutex.withLock {
            val current = session ?: return@withLock
            // An old response must not recreate a logged-out session or replace another login.
            if (current.token != expectedToken || current.userId != user.id) return@withLock
            val next = fromUser(current.token, user)
            dao.saveSession(next)
            session = next
        }
    }

    suspend fun logout() = withContext(Dispatchers.IO + NonCancellable) {
        mutex.withLock {
            dao.clearSession()
            session = null
        }
    }

    companion object {
        private val gson = Gson()
        fun decodeUser(json: String): User? = try { gson.fromJson(json, User::class.java) } catch (_: Exception) { null }
        fun fromUser(token: String, user: User) = LoginSessionEntity(
            token = token, userId = user.id, email = user.email, role = user.role,
            displayName = "${user.firstName} ${user.lastName}", userJson = gson.toJson(user)
        )
    }
}
