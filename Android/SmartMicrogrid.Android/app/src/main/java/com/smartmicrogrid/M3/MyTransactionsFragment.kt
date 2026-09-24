package com.smartmicrogrid.M3

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.lifecycle.ViewModelProvider
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.databinding.FragmentMyTransactionsBinding

class MyTransactionsFragment : Fragment() {

    private var _binding: FragmentMyTransactionsBinding? = null
    private val binding get() = _binding!!
    private lateinit var viewModel: TransactionViewModel
    private lateinit var adapter: TransactionAdapter

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentMyTransactionsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        viewModel = ViewModelProvider(this)[TransactionViewModel::class.java]
        adapter = TransactionAdapter(emptyList()) { transaction ->
            startActivity(
                Intent(requireContext(), ProsumerTransactionDetailsActivity::class.java).apply {
                    putExtra(
                        ProsumerTransactionDetailsActivity.EXTRA_TRANSACTION_ID,
                        transaction.id
                    )
                }
            )
        }
        binding.recyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerView.adapter = adapter
        binding.swipeRefresh.setOnRefreshListener { viewModel.loadTransactionHistory() }
        binding.btnRetry.setOnClickListener { viewModel.loadTransactionHistory() }

        viewModel.transactions.observe(viewLifecycleOwner) { transactions ->
            adapter.updateData(transactions)
            val hasError = viewModel.errorMessage.value != null
            binding.tvEmpty.visibility = if (transactions.isEmpty() && !hasError) View.VISIBLE else View.GONE
            binding.recyclerView.visibility = if (transactions.isEmpty()) View.GONE else View.VISIBLE
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
            binding.errorContent.visibility = if (error.isNullOrBlank()) View.GONE else View.VISIBLE
            binding.tvError.text = error.orEmpty()
            if (error.isNullOrBlank()) {
                binding.tvEmpty.visibility = if (adapter.itemCount == 0) View.VISIBLE else View.GONE
                binding.recyclerView.visibility = if (adapter.itemCount == 0) View.GONE else View.VISIBLE
            } else {
                binding.tvEmpty.visibility = View.GONE
                binding.recyclerView.visibility = View.GONE
            }
        }

        viewModel.loadTransactionHistory()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
