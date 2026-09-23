package com.smartmicrogrid.M1.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.ViewModelProvider
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.databinding.FragmentSearchEnergyBinding

class VerifierMicrogridFragment : Fragment() {

    private var _binding: FragmentSearchEnergyBinding? = null
    private val binding get() = _binding!!

    private lateinit var viewModel: MicrogridViewModel
    private lateinit var adapter: MicrogridAdapter

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentSearchEnergyBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        
        viewModel = ViewModelProvider(this)[MicrogridViewModel::class.java]
        
        binding.tvTitle.text = "Infrastructure Verification"
        binding.tvSubtitle.text = "View microgrid capacities and battery levels for transaction validation"

        adapter = MicrogridAdapter(emptyList()) { microgrid ->
            // View slots or just details
            val intent = Intent(requireContext(), com.smartmicrogrid.M1.slots.EnergySlotActivity::class.java).apply {
                putExtra("MICROGRID_ID", microgrid.id)
            }
            startActivity(intent)
        }

        binding.recyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerView.adapter = adapter

        binding.swipeRefresh.setOnRefreshListener {
            viewModel.loadMicrogrids()
        }

        viewModel.microgrids.observe(viewLifecycleOwner) { list ->
            // Verifier needs to see everything
            adapter.updateData(list)
            binding.tvEmpty.visibility = if (list.isEmpty()) View.VISIBLE else View.GONE
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

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
