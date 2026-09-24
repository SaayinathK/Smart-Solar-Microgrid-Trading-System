package com.smartmicrogrid.M3

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.ViewModelProvider
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.databinding.FragmentTransactionListBinding

class PendingTransactionsFragment : Fragment() {

    private var _binding: FragmentTransactionListBinding? = null
    private val binding get() = _binding!!
    private lateinit var viewModel: TransactionViewModel
    private lateinit var adapter: TransactionAdapter

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentTransactionListBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        viewModel = ViewModelProvider(this)[TransactionViewModel::class.java]

        binding.tvTitle.text = "Pending Transactions"
        binding.tvSubtitle.text = "Transactions ready for verifier action"

        adapter = TransactionAdapter(emptyList()) { transaction ->
            Toast.makeText(
                requireContext(),
                "Selected ${transaction.transactionCode.ifBlank { transaction.id } }",
                Toast.LENGTH_SHORT
            ).show()
        }

        binding.recyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerView.adapter = adapter
        binding.swipeRefresh.setOnRefreshListener { viewModel.loadPendingTransactions() }

        viewModel.transactions.observe(viewLifecycleOwner) { transactions ->
            adapter.updateData(transactions)
            binding.tvEmpty.visibility = if (transactions.isEmpty()) View.VISIBLE else View.GONE
        }

        viewModel.isLoading.observe(viewLifecycleOwner) { loading ->
            binding.progressBar.visibility = if (loading && !binding.swipeRefresh.isRefreshing) {
                View.VISIBLE
            } else {
                View.GONE
            }
            if (!loading) binding.swipeRefresh.isRefreshing = false
        }

        viewModel.errorMessage.observe(viewLifecycleOwner) { error ->
            error?.let { Toast.makeText(requireContext(), it, Toast.LENGTH_LONG).show() }
        }

        viewModel.loadPendingTransactions()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}