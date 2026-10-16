package com.smartmicrogrid.prosumer

import android.content.Context
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.local.ReservationEntity
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.*
import com.smartmicrogrid.operator.data
import com.smartmicrogrid.utils.SessionManager

class ProsumerRepository {
    val api get() = RetrofitClient.apiService
    fun ensureAccess() = check(SessionManager.isLoggedIn() && SessionManager.getUserRole() == "Prosumer") { "Sign in as a prosumer to continue." }
    suspend fun reservations(context: Context): List<Reservation> {
        ensureAccess()
        val rows = mutableListOf<Reservation>()
        var page = 1
        do {
            val batch = api.getReservations(page = page++, pageSize = 100).data()
            rows.addAll(batch)
        } while (batch.size == 100)
        val own = rows.distinctBy { it.id }.filter { ProsumerRules.owns(it.prosumerId, SessionManager.getUser()) }
        AppDatabase.getDatabase(context).reservationDao().replaceAll(own.map(ReservationEntity::fromDomain))
        return own
    }
    suspend fun cached(context: Context): List<Reservation> {
        ensureAccess()
        return AppDatabase.getDatabase(context).reservationDao().all().map { it.toDomain() }
            .filter { ProsumerRules.owns(it.prosumerId, SessionManager.getUser()) }
    }
    suspend fun transactions(): List<Transaction> {
        ensureAccess()
        return api.getTransactions().data().filter { ProsumerRules.owns(it.prosumerId, SessionManager.getUser()) }
    }
    suspend fun offers(): List<EnergySlot> {
        ensureAccess()
        return api.getEnergyAvailability().data().map { with(ProsumerRules) { it.asSlot() } }
    }
    suspend fun grids(): List<Microgrid> {
        ensureAccess()
        return api.getMicrogrids(status = "Active", isActive = true).data().filter { it.status == "Active" && it.isActive }
    }
    suspend fun resources(): List<Microgrid> { ensureAccess(); return api.getMicrogrids().data() }
}
