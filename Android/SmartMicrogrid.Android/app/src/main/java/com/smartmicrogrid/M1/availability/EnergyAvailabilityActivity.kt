package com.smartmicrogrid.M1.availability

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.databinding.ActivityEnergyAvailabilityBinding

class EnergyAvailabilityActivity : AppCompatActivity() {

    private lateinit var binding: ActivityEnergyAvailabilityBinding
    private val viewModel: EnergyAvailabilityViewModel by viewModels()
    private lateinit var adapter: EnergyAvailabilityAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityEnergyAvailabilityBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = "Energy Availability"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        adapter = EnergyAvailabilityAdapter(emptyList())
        binding.recyclerView.layoutManager = LinearLayoutManager(this)
        binding.recyclerView.adapter = adapter

        binding.btnSearch.setOnClickListener {
            val loc = binding.etLocation.text.toString().trim()
            viewModel.loadAvailability(if (loc.isNotEmpty()) loc else null)
        }

        binding.swipeRefresh.setOnRefreshListener {
            viewModel.loadAvailability()
        }

        viewModel.availableSlots.observe(this) { list ->
            adapter.updateData(list)
            binding.tvEmpty.visibility = if (list.isEmpty()) View.VISIBLE else View.GONE
        }

        viewModel.isLoading.observe(this) { loading ->
            binding.progressBar.visibility = if (loading && !binding.swipeRefresh.isRefreshing) View.VISIBLE else View.GONE
            if (!loading) binding.swipeRefresh.isRefreshing = false
        }

        viewModel.errorMessage.observe(this) { err ->
            err?.let { Toast.makeText(this, it, Toast.LENGTH_LONG).show() }
        }

        viewModel.loadAvailability()
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
