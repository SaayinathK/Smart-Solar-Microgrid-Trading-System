package com.smartmicrogrid.M1.availability

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.databinding.ActivityEnergyAvailabilityBinding
import com.smartmicrogrid.models.EnergyAvailabilitySlot
import java.util.Locale

class EnergyAvailabilityActivity : AppCompatActivity() {

    private lateinit var binding: ActivityEnergyAvailabilityBinding
    private val viewModel: EnergyAvailabilityViewModel by viewModels()
    private lateinit var adapter: EnergyAvailabilityAdapter
    private var sortByLowestPrice = true
    private var currentSlots: List<EnergyAvailabilitySlot> = emptyList()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityEnergyAvailabilityBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }
        adapter = EnergyAvailabilityAdapter(emptyList())
        binding.recyclerView.layoutManager = LinearLayoutManager(this)
        binding.recyclerView.adapter = adapter

        binding.btnSearch.setOnClickListener { loadWithFilters() }
        binding.btnSort.setOnClickListener {
            sortByLowestPrice = !sortByLowestPrice
            binding.btnSort.text = if (sortByLowestPrice) "Lowest price" else "Most energy"
            renderSlots(currentSlots)
        }
        binding.swipeRefresh.setOnRefreshListener { loadWithFilters() }

        viewModel.availableSlots.observe(this) { list ->
            currentSlots = list
            renderSlots(list)
        }
        viewModel.isLoading.observe(this) { loading ->
            binding.progressBar.visibility = if (loading && !binding.swipeRefresh.isRefreshing) View.VISIBLE else View.GONE
            if (!loading) binding.swipeRefresh.isRefreshing = false
        }
        viewModel.errorMessage.observe(this) { error ->
            error?.let { Toast.makeText(this, it, Toast.LENGTH_LONG).show() }
        }
        loadWithFilters()
    }

    private fun loadWithFilters() {
        val location = binding.etLocation.text?.toString()?.trim().orEmpty().ifBlank { null }
        val minimum = binding.etMinimumEnergy.text?.toString()?.toDoubleOrNull()
        viewModel.loadAvailability(location, minimum)
    }

    private fun renderSlots(slots: List<EnergyAvailabilitySlot>) {
        val sorted = if (sortByLowestPrice) slots.sortedBy { it.pricePerUnit } else slots.sortedByDescending { it.availableAmount }
        adapter.updateData(sorted)
        binding.tvEmpty.visibility = if (sorted.isEmpty()) View.VISIBLE else View.GONE
        binding.tvTotalOffers.text = sorted.size.toString()
        binding.tvTotalEnergy.text = "${format(sorted.sumOf { it.availableAmount })} kWh"
        binding.tvBestPrice.text = sorted.minOfOrNull { it.pricePerUnit }?.let { "$${format(it)}" } ?: "-"
    }

    private fun format(value: Double): String = String.format(Locale.getDefault(), "%.1f", value)
}
