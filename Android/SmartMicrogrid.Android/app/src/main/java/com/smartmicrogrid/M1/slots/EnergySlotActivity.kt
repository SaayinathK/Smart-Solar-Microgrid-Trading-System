package com.smartmicrogrid.M1.slots

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.M1.availability.EnergyAvailabilityAdapter
import com.smartmicrogrid.databinding.ActivityEnergySlotsBinding
import com.smartmicrogrid.models.EnergyAvailabilitySlot

class EnergySlotActivity : AppCompatActivity() {

    private lateinit var binding: ActivityEnergySlotsBinding
    private val viewModel: EnergySlotViewModel by viewModels()
    private lateinit var adapter: EnergyAvailabilityAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityEnergySlotsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }

        adapter = EnergyAvailabilityAdapter(emptyList())
        binding.recyclerView.layoutManager = LinearLayoutManager(this)
        binding.recyclerView.adapter = adapter

        viewModel.slots.observe(this) { list ->
            val adapted = list.map {
                EnergyAvailabilitySlot(
                    energySlotId = it.id,
                    microgridNodeId = it.microgridNodeId,
                    microgridName = it.microgridName,
                    location = it.location,
                    energyAmount = it.energyAmount,
                    availableAmount = it.availableAmount,
                    startTime = it.startTime,
                    endTime = it.endTime,
                    pricePerUnit = it.pricePerUnit,
                    status = it.status
                )
            }
            adapter.updateData(adapted)
            binding.tvEmpty.visibility = if (list.isEmpty()) View.VISIBLE else View.GONE
        }

        viewModel.isLoading.observe(this) { loading ->
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        }

        viewModel.errorMessage.observe(this) { err ->
            err?.let { Toast.makeText(this, it, Toast.LENGTH_LONG).show() }
        }

        viewModel.loadEnergySlots()
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
