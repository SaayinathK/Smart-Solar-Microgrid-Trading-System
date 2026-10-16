package com.smartmicrogrid.operator

import com.smartmicrogrid.data.remote.ApiService
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.*
import com.smartmicrogrid.utils.SessionManager
import retrofit2.Response

fun <T> Response<ApiResponse<T>>.checked(): ApiResponse<T> {
    if (isSuccessful && body()?.success == true) return body()!!
    val message = when (code()) {
        401 -> "Your session has expired. Sign in again."
        403 -> "This action is not permitted for your account."
        else -> runCatching {
            org.json.JSONObject(errorBody()?.string().orEmpty()).optString("message").takeIf { it.isNotBlank() }
        }.getOrNull() ?: body()?.message?.takeIf { it.isNotBlank() } ?: "The request failed (${code()}). Please retry."
    }
    throw IllegalStateException(message)
}

fun <T> Response<ApiResponse<T>>.data(): T = checked().data ?: throw IllegalStateException("The server returned no data. Refresh and retry.")

class OperatorRepository {
    val api: ApiService get() = RetrofitClient.apiService
    fun ensureOperator() {
        check(OperatorRules.canManage(SessionManager.getUserRole())) { "Sign in as a microgrid operator to use this workspace." }
    }
    suspend fun grids(): List<Microgrid> {
        ensureOperator()
        val userId = SessionManager.getUser()?.id?.takeIf { it.isNotBlank() } ?: error("Sign in again to load your assigned microgrids.")
        return api.getMicrogrids().data().filter { it.operatorId == userId }
    }
    suspend fun slots(grids: List<Microgrid>): List<EnergySlot> {
        ensureOperator()
        val ids = grids.map { it.id }.toSet()
        return api.getEnergySlots().data().filter { it.microgridNodeId in ids }
    }
    suspend fun available(grids: List<Microgrid>): List<EnergySlot> {
        ensureOperator()
        val ids = grids.map { it.id }.toSet()
        return api.getEnergyAvailability(operatorId = SessionManager.getUser()?.id).data()
            .filter { it.microgridNodeId in ids }
            .map { EnergySlot(id = it.energySlotId, microgridNodeId = it.microgridNodeId,
                microgridName = it.microgridName, location = it.location, energyAmount = it.energyAmount,
                availableAmount = it.availableAmount, startTime = it.startTime, endTime = it.endTime,
                pricePerUnit = it.pricePerUnit, status = it.status) }
    }
    suspend fun reservations(): List<Reservation> {
        ensureOperator()
        val rows = mutableListOf<Reservation>()
        var page = 1
        do {
            val batch = api.getReservations(page = page++, pageSize = 100).data()
            rows.addAll(batch)
        } while (batch.size == 100)
        return rows.distinctBy { it.id }
    }
    suspend fun transactions(): List<Transaction> { ensureOperator(); return api.getTransactions().data() }
}
