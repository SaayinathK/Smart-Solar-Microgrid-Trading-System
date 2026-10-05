package com.smartmicrogrid.data.local

import com.smartmicrogrid.models.User
import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Test

class LoginSessionStoreTest {
    private val user = User(id = "user-1", firstName = "Solar", lastName = "User", email = "user@example.test", role = "Prosumer")

    private class FakeDao : LoginSessionDao {
        var row: LoginSessionEntity? = null
        var failWrites = false
        override suspend fun getSession() = row
        override suspend fun saveSession(session: LoginSessionEntity) {
            check(!failWrites) { "Disk write failed" }
            row = session
        }
        override suspend fun clearSession() {
            check(!failWrites) { "Disk write failed" }
            row = null
        }
    }

    private fun store(dao: FakeDao) = LoginSessionStore(dao, { null }, {})

    @Test fun loginRestoresInANewStoreAndProfileEditsPersist() = runBlocking {
        val dao = FakeDao()
        val first = store(dao)
        first.initialize()
        first.setSession("token-1", user)
        val restored = store(dao)
        restored.initialize()
        assertEquals("token-1", restored.getToken())
        assertEquals(user, restored.getUser())
        val updated = user.copy(firstName = "Updated")
        restored.updateUser(updated, "token-1")
        val reopened = store(dao)
        reopened.initialize()
        assertEquals("Updated User", reopened.getUserName())
        assertEquals(updated, reopened.getUser())
        assertEquals("Prosumer", reopened.getUserRole())
        assertEquals(user.email, dao.row?.email)
    }

    @Test fun legacySessionIsWrittenBeforePreferencesAreRemoved() = runBlocking {
        val dao = FakeDao()
        var legacy: LoginSessionEntity? = LoginSessionStore.fromUser("legacy-token", user)
        val session = LoginSessionStore(dao, { legacy }, {
            assertEquals(legacy, dao.row)
            legacy = null
        })
        session.initialize()
        assertNull(legacy)
        assertEquals("legacy-token", session.getToken())
    }

    @Test fun failedMigrationKeepsLegacySessionForRetry() = runBlocking {
        val dao = FakeDao().apply { failWrites = true }
        var cleared = false
        val session = LoginSessionStore(dao, { LoginSessionStore.fromUser("legacy", user) }, { cleared = true })
        assertTrue(runCatching { session.initialize() }.isFailure)
        assertFalse(cleared)
        assertFalse(session.initialized)
        dao.failWrites = false
        session.initialize()
        assertTrue(cleared)
        assertEquals("legacy", session.getToken())
    }

    @Test fun sqliteSessionWinsOverLeftoverPreferences() = runBlocking {
        val dao = FakeDao().apply { row = LoginSessionStore.fromUser("new", user) }
        var cleared = false
        val session = LoginSessionStore(dao, { error("Must not read old session") }, { cleared = true })
        session.initialize()
        assertEquals("new", session.getToken())
        assertTrue(cleared)
    }

    @Test fun logoutSurvivesRestartAndIgnoresLateProfileResponse() = runBlocking {
        val dao = FakeDao()
        val session = store(dao)
        session.initialize()
        session.setSession("token", user)
        session.logout()
        session.updateUser(user.copy(firstName = "Late"), "token")
        assertNull(session.getUser())
        assertNull(dao.row)
        val reopened = store(dao)
        reopened.initialize()
        assertNull(reopened.getToken())
    }

    @Test fun oldProfileResponseCannotOverwriteANewerLogin() = runBlocking {
        val dao = FakeDao()
        val session = store(dao)
        session.initialize()
        session.setSession("old-token", user)
        session.setSession("new-token", user.copy(firstName = "New login"))
        session.updateUser(user.copy(firstName = "Old response"), "old-token")
        assertEquals("New login", session.getUser()?.firstName)
        assertEquals("new-token", dao.row?.token)
    }

    @Test fun diskFailuresDoNotPublishUnpersistedLoginOrLogout() = runBlocking {
        val dao = FakeDao()
        val session = store(dao)
        session.initialize()
        dao.failWrites = true
        assertTrue(runCatching { session.setSession("token", user) }.isFailure)
        assertNull(session.getToken())
        dao.failWrites = false
        session.setSession("token", user)
        dao.failWrites = true
        assertTrue(runCatching { session.logout() }.isFailure)
        assertEquals("token", session.getToken())
        assertEquals("token", dao.row?.token)
    }

    @Test fun corruptStoredProfileIsDiscardedWithoutRestoringLegacyCredentials() = runBlocking {
        val dao = FakeDao().apply { row = LoginSessionStore.fromUser("token", user).copy(userJson = "broken json") }
        val session = LoginSessionStore(dao, { error("Must not restore old credentials") }, {})
        session.initialize()
        assertNull(session.getToken())
        assertNull(dao.row)
    }
}
