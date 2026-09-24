package com.smartmicrogrid.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey
import com.smartmicrogrid.models.Microgrid

@Entity(tableName = "microgrids")
data class MicrogridEntity(
    @PrimaryKey val id: String,
    val name: String,
    val location: String,
    val description: String?,
    val latitude: Double,
    val longitude: Double,
    val capacity: Double,
    val availableCapacity: Double,
    val reservedCapacity: Double,
    val usedCapacity: Double,
    val batteryCapacity: Double,
    val currentBatteryLevel: Double,
    val batteryPercentage: Double,
    val status: String,
    val isActive: Boolean,
    val operatorId: String,
    val lastCachedAt: Long = System.currentTimeMillis()
) {
    fun toDomain(): Microgrid {
        return Microgrid(
            id = id,
            name = name,
            location = location,
            description = description,
            latitude = latitude,
            longitude = longitude,
            capacity = capacity,
            availableCapacity = availableCapacity,
            reservedCapacity = reservedCapacity,
            usedCapacity = usedCapacity,
            batteryCapacity = batteryCapacity,
            currentBatteryLevel = currentBatteryLevel,
            batteryPercentage = batteryPercentage,
            status = status,
            isActive = isActive,
            operatorId = operatorId
        )
    }

    companion object {
        fun fromDomain(m: Microgrid): MicrogridEntity {
            return MicrogridEntity(
                id = m.id,
                name = m.name,
                location = m.location,
                description = m.description,
                latitude = m.latitude,
                longitude = m.longitude,
                capacity = m.capacity,
                availableCapacity = m.availableCapacity,
                reservedCapacity = m.reservedCapacity,
                usedCapacity = m.usedCapacity,
                batteryCapacity = m.batteryCapacity,
                currentBatteryLevel = m.currentBatteryLevel,
                batteryPercentage = m.batteryPercentage,
                status = m.status,
                isActive = m.isActive,
                operatorId = m.operatorId
            )
        }
    }
}
