package com.smartmicrogrid.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey
import com.smartmicrogrid.models.EnergySlot

@Entity(tableName = "energy_slots")
data class EnergySlotEntity(
    @PrimaryKey val id: String,
    val microgridNodeId: String,
    val microgridName: String,
    val location: String,
    val energyAmount: Double,
    val availableAmount: Double,
    val startTime: String,
    val endTime: String,
    val pricePerUnit: Double,
    val status: String,
    val createdBy: String,
    val lastCachedAt: Long = System.currentTimeMillis()
) {
    fun toDomain(): EnergySlot {
        return EnergySlot(
            id = id,
            microgridNodeId = microgridNodeId,
            microgridName = microgridName,
            location = location,
            energyAmount = energyAmount,
            availableAmount = availableAmount,
            startTime = startTime,
            endTime = endTime,
            pricePerUnit = pricePerUnit,
            status = status,
            createdBy = createdBy
        )
    }

    companion object {
        fun fromDomain(s: EnergySlot): EnergySlotEntity {
            return EnergySlotEntity(
                id = s.id,
                microgridNodeId = s.microgridNodeId,
                microgridName = s.microgridName,
                location = s.location,
                energyAmount = s.energyAmount,
                availableAmount = s.availableAmount,
                startTime = s.startTime,
                endTime = s.endTime,
                pricePerUnit = s.pricePerUnit,
                status = s.status,
                createdBy = s.createdBy
            )
        }
    }
}
