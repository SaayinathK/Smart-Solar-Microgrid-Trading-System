package com.smartmicrogrid.M1.microgrid

import android.os.Bundle
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.databinding.ActivityMicrogridDetailsBinding

class MicrogridDetailsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMicrogridDetailsBinding
    private val viewModel: MicrogridViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMicrogridDetailsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = "Microgrid Details"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        val microgridId = intent.getStringExtra("MICROGRID_ID") ?: ""

        viewModel.selectedMicrogrid.observe(this) { m ->
            m?.let {
                binding.tvName.text = it.name
                binding.tvLocation.text = "Location: ${it.location}"
                binding.tvDescription.text = it.description ?: "Solar generation node."
                binding.tvCapacity.text = "Total Capacity: ${it.capacity} kWh"
                binding.tvAvailableCapacity.text = "Available: ${it.availableCapacity} kWh"
                binding.tvReservedCapacity.text = "Reserved: ${it.reservedCapacity} kWh"
                binding.tvUsedCapacity.text = "Used: ${it.usedCapacity} kWh"
                binding.tvBattery.text = "Battery: ${it.currentBatteryLevel} / ${it.batteryCapacity} kWh (${it.batteryPercentage.toInt()}%)"
                binding.tvStatus.text = "Status: ${it.status}"
                binding.tvGps.text = "GPS: Lat ${it.latitude}, Lng ${it.longitude}"
            }
        }

        viewModel.errorMessage.observe(this) { err ->
            err?.let { Toast.makeText(this, it, Toast.LENGTH_LONG).show() }
        }

        if (microgridId.isNotEmpty()) {
            viewModel.loadMicrogridById(microgridId)
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
