package com.smartmicrogrid.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey
import com.smartmicrogrid.models.Reservation

@Entity(tableName = "reservations")
data class ReservationEntity(
    @PrimaryKey val id: String,
    val prosumerId: String,
    val microgridNodeId: String,
    val energySlotId: String,
    val energyAmount: Double,
    val reservationDate: String,
    val startTime: String,
    val endTime: String,
    val status: String,
    val createdAt: String,
    val updatedAt: String,
    val cachedAt: Long = System.currentTimeMillis()
) {
    fun toDomain() = Reservation(id, prosumerId, microgridNodeId, energySlotId, energyAmount, reservationDate, startTime, endTime, status, createdAt, updatedAt)
    companion object {
        fun fromDomain(r: Reservation) = ReservationEntity(r.id, r.prosumerId, r.microgridNodeId, r.energySlotId, r.energyAmount, r.reservationDate, r.startTime, r.endTime, r.status, r.createdAt, r.updatedAt)
    }
}
