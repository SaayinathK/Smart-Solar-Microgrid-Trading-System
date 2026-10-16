package com.smartmicrogrid.data.repository

import com.smartmicrogrid.data.remote.ApiService
import com.smartmicrogrid.models.ApiResponse
import com.smartmicrogrid.models.RegisterRequest
import kotlinx.coroutines.runBlocking
import org.junit.Assert.assertEquals
import org.junit.Test
import retrofit2.Response
import java.lang.reflect.Proxy

class AuthRepositoryTest {
    private fun server(message: String): ApiService = Proxy.newProxyInstance(
        ApiService::class.java.classLoader,
        arrayOf(ApiService::class.java)
    ) { _, _, _ -> Response.success(ApiResponse<Any>(success = false, message = message)) } as ApiService

    @Test
    fun loginUsesTheNewConnectionWithoutRecreatingTheRepository() = runBlocking {
        var activeServer = server("old connection")
        val repository = AuthRepository { activeServer }
        assertEquals("old connection", repository.login("user@example.com", "password").exceptionOrNull()?.message)
        activeServer = server("new connection")
        assertEquals("new connection", repository.login("user@example.com", "password").exceptionOrNull()?.message)
    }

    @Test
    fun registrationUsesTheNewConnectionWithoutRecreatingTheRepository() = runBlocking {
        var activeServer = server("old connection")
        val repository = AuthRepository { activeServer }
        val request = RegisterRequest("First", "Last", "user@example.com", "", password = "password", confirmPassword = "password")
        assertEquals("old connection", repository.register(request).exceptionOrNull()?.message)
        activeServer = server("new connection")
        assertEquals("new connection", repository.register(request).exceptionOrNull()?.message)
    }
}
