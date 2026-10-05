package com.smartmicrogrid.M3

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.Fragment
import androidx.lifecycle.ViewModelProvider
import androidx.recyclerview.widget.LinearLayoutManager
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.snackbar.Snackbar
import com.smartmicrogrid.R
import com.smartmicrogrid.databinding.FragmentApprovedReservationsBinding
import com.smartmicrogrid.models.Reservation
import com.smartmicrogrid.models.Transaction

class ApprovedReservationsFragment : Fragment() {

    private var _binding: FragmentApprovedReservationsBinding? = null
    private val binding get() = _binding!!
    private lateinit var viewModel: TransactionViewModel
    private lateinit var adapter: ApprovedReservationAdapter
    private var createDialog: AlertDialog? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentApprovedReservationsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        viewModel = ViewModelProvider(this)[TransactionViewModel::class.java]
        adapter = ApprovedReservationAdapter(::confirmCreateTransaction)
        binding.recyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerView.adapter = adapter

        binding.btnActionRequired.setOnClickListener { showFragment(PendingTransactionsFragment()) }
        binding.btnTransactionHistory.setOnClickListener { showFragment(TransactionHistoryFragment()) }
        binding.btnRetry.setOnClickListener { viewModel.loadApprovedReservations() }
        binding.swipeRefresh.setOnRefreshListener { viewModel.loadApprovedReservations() }

        viewModel.approvedReservations.observe(viewLifecycleOwner) { reservations ->
            adapter.submit(reservations)
            renderApprovedReservations(reservations.isEmpty())
        }
        viewModel.isLoadingApprovedReservations.observe(viewLifecycleOwner) { loading ->
            binding.progressBar.visibility = if (loading && !binding.swipeRefresh.isRefreshing) {
                View.VISIBLE
            } else {
                View.GONE
            }
            if (!loading) binding.swipeRefresh.isRefreshing = false
            if (loading) {
                binding.emptyState.visibility = View.GONE
                binding.errorState.visibility = View.GONE
            }
        }
        viewModel.approvedReservationsError.observe(viewLifecycleOwner) { error ->
            val hasError = !error.isNullOrBlank()
            binding.errorState.visibility = if (hasError) View.VISIBLE else View.GONE
            binding.tvError.text = error.orEmpty()
            if (hasError) {
                binding.recyclerView.visibility = View.GONE
                binding.emptyState.visibility = View.GONE
            } else if (viewModel.isLoadingApprovedReservations.value != true) {
                renderApprovedReservations(viewModel.approvedReservations.value.isNullOrEmpty())
            }
        }
        viewModel.isCreatingTransaction.observe(viewLifecycleOwner) { creating ->
            adapter.setCreateEnabled(!creating)
            createDialog?.let { dialog ->
                dialog.getButton(AlertDialog.BUTTON_POSITIVE)?.apply {
                    isEnabled = !creating
                    text = if (creating) "Creating…" else getString(R.string.create_transaction)
                }
                dialog.getButton(AlertDialog.BUTTON_NEGATIVE)?.isEnabled = !creating
            }
        }
        viewModel.transactionCreationError.observe(viewLifecycleOwner) { error ->
            if (!error.isNullOrBlank()) {
                val duplicate = error.contains("already has a transaction", ignoreCase = true)
                val message = if (duplicate) {
                    "Transaction already exists. This approved reservation has already been converted into a transaction."
                } else error
                Snackbar.make(binding.root, message, Snackbar.LENGTH_LONG).show()
                createDialog?.setCancelable(true)
            }
        }
        viewModel.createdTransaction.observe(viewLifecycleOwner) { transaction ->
            transaction?.let(::showCreatedTransaction)
        }

        viewModel.loadApprovedReservations()
    }

    private fun renderApprovedReservations(isEmpty: Boolean) {
        val hasError = !viewModel.approvedReservationsError.value.isNullOrBlank()
        binding.recyclerView.visibility = if (!isEmpty && !hasError) View.VISIBLE else View.GONE
        binding.emptyState.visibility = if (isEmpty && !hasError) View.VISIBLE else View.GONE
    }

    private fun confirmCreateTransaction(reservation: Reservation) {
        if (!reservation.status.equals("Approved", ignoreCase = true)) return

        val prosumer = reservation.prosumerName?.takeIf(String::isNotBlank)
            ?: reservation.prosumerId
        val microgrid = reservation.microgridName?.takeIf(String::isNotBlank)
            ?: reservation.microgridNodeId
        val context = requireContext()
        val details = LinearLayout(context).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(24, 4, 24, 4)
            addDialogRow("Reservation", reservation.id)
            addDialogRow("Prosumer", prosumer)
            addDialogRow("Energy", "${reservation.energyAmount} kWh")
            addDialogRow("Microgrid", microgrid)
            val date = TransactionUiFormatters.dateTime(reservation.reservationDate)
            addDialogRow(
                "Scheduled",
                "${if (date == "Not available") "" else "$date · "}" +
                    "${TransactionUiFormatters.dateTime(reservation.startTime)} to ${TransactionUiFormatters.dateTime(reservation.endTime)}"
            )
            addDialogRow("Status", reservation.status)
        }
        val scrollableDetails = ScrollView(context).apply {
            isFillViewport = false
            addView(details)
        }

        createDialog = MaterialAlertDialogBuilder(context)
            .setTitle("Create Energy Transaction?")
            .setView(scrollableDetails)
            .setNegativeButton("Cancel", null)
            .setPositiveButton(R.string.create_transaction, null)
            .create()
            .also { dialog ->
                dialog.setOnShowListener {
                    dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                        dialog.setCancelable(false)
                        viewModel.createTransaction(reservation.id)
                    }
                }
                dialog.show()
            }
    }

    private fun LinearLayout.addDialogRow(label: String, value: String) {
        val context = requireContext()
        addView(TextView(context).apply {
            text = label
            textSize = 12f
            setTextColor(context.getColor(R.color.m3_text_secondary))
            setPadding(0, 10, 0, 0)
        })
        addView(TextView(context).apply {
            text = value
            textSize = 14f
            setTextColor(context.getColor(R.color.m3_text_primary))
            setTextIsSelectable(true)
            setPadding(0, 2, 0, 0)
        })
    }

    private fun showCreatedTransaction(transaction: Transaction) {
        viewModel.clearCreatedTransaction()
        createDialog?.dismiss()
        createDialog = null

        val message = "Transaction ID: ${transaction.id}\n" +
            "Reservation ID: ${transaction.reservationId}\n" +
            "Status: ${transaction.status}"
        AlertDialog.Builder(requireContext())
            .setTitle("Transaction Created")
            .setMessage(message)
            .setNegativeButton("Back to Approved Reservations", null)
            .setPositiveButton("View Transaction") { _, _ ->
                startActivity(Intent(requireContext(), TransactionDetailsActivity::class.java).apply {
                    putExtra(TransactionDetailsActivity.EXTRA_TRANSACTION_ID, transaction.id)
                })
            }
            .show()
    }

    private fun showFragment(fragment: Fragment) {
        parentFragmentManager.beginTransaction()
            .replace(R.id.fragment_container, fragment)
            .commit()
    }

    override fun onDestroyView() {
        createDialog?.dismiss()
        createDialog = null
        super.onDestroyView()
        _binding = null
    }
}
