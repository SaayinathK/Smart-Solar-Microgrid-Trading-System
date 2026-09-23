package com.smartmicrogrid.M1.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import android.text.Editable
import android.text.TextWatcher
import androidx.fragment.app.Fragment
import androidx.lifecycle.ViewModelProvider
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.databinding.FragmentSearchEnergyBinding

class SearchEnergyFragment : Fragment() {

    private var _binding: FragmentSearchEnergyBinding? = null
    private val binding get() = _binding!!

    private lateinit var viewModel: MicrogridViewModel
    private lateinit var adapter: MicrogridAdapter
    private var sortByHighestCapacity = true

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentSearchEnergyBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        
        viewModel = ViewModelProvider(this)[MicrogridViewModel::class.java]

        adapter = MicrogridAdapter(emptyList()) { microgrid ->
            // Tapping a microgrid should open its slots
            val intent = Intent(requireContext(), com.smartmicrogrid.M1.slots.EnergySlotActivity::class.java).apply {
                putExtra("MICROGRID_ID", microgrid.id)
            }
            startActivity(intent)
        }

        binding.recyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerView.adapter = adapter

        binding.swipeRefresh.setOnRefreshListener {
            viewModel.loadMicrogrids() // We filter active status in viewmodel or adapter if needed
        }

        binding.etSearch.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {
                filterList(s.toString())
            }
            override fun afterTextChanged(s: Editable?) {}
        })

        binding.btnSortCapacity.setOnClickListener {
            sortByHighestCapacity = !sortByHighestCapacity
            binding.btnSortCapacity.text = if (sortByHighestCapacity) "Highest kWh" else "Lowest kWh"
            filterList(binding.etSearch.text.toString())
        }

        viewModel.microgrids.observe(viewLifecycleOwner) { list ->
            filterList(binding.etSearch.text.toString())
        }

        viewModel.isLoading.observe(viewLifecycleOwner) { loading ->
            binding.progressBar.visibility = if (loading && !binding.swipeRefresh.isRefreshing) View.VISIBLE else View.GONE
            if (!loading) binding.swipeRefresh.isRefreshing = false
        }

        viewModel.errorMessage.observe(viewLifecycleOwner) { err ->
            err?.let { Toast.makeText(requireContext(), it, Toast.LENGTH_LONG).show() }
        }

        viewModel.loadMicrogrids()
    }

    private fun filterList(query: String) {
        val currentList = viewModel.microgrids.value ?: emptyList()
        val activeList = currentList.filter { it.status == "Active" }
        
        val filtered = if (query.isBlank()) {
            activeList
        } else {
            activeList.filter {
                it.name.contains(query, ignoreCase = true) ||
                (it.location?.contains(query, ignoreCase = true) == true)
            }
        }
        
        val sorted = if (sortByHighestCapacity) filtered.sortedByDescending { it.availableCapacity } else filtered.sortedBy { it.availableCapacity }
        adapter.updateData(sorted)
        binding.tvEmpty.visibility = if (filtered.isEmpty()) View.VISIBLE else View.GONE
        binding.tvEmpty.text = if (filtered.isEmpty() && query.isNotBlank()) "No microgrids match your search." else "No active microgrids found in your area."
        binding.tvResultsSummary.text = if (sorted.isEmpty()) "No active local capacity available" else "${sorted.size} active microgrids - ${"%.1f".format(sorted.sumOf { it.availableCapacity })} kWh available"
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
