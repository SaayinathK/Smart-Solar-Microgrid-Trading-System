package com.smartmicrogrid.M3

import android.os.Bundle
import android.content.Intent
import android.view.View
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.google.gson.Gson
import com.smartmicrogrid.databinding.ActivityEnergyTransferConfirmationBinding
import com.smartmicrogrid.models.Transaction

class EnergyTransferConfirmationActivity : AppCompatActivity() {

    private lateinit var binding: ActivityEnergyTransferConfirmationBinding
    private val viewModel: EnergyTransferConfirmationViewModel by viewModels()
    private var transactionId = ""

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityEnergyTransferConfirmationBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = getString(com.smartmicrogrid.R.string.energy_transfer_confirmation_title)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        transactionId = intent.getStringExtra(TransactionDetailsActivity.EXTRA_TRANSACTION_ID).orEmpty()
        binding.btnRetry.setOnClickListener { viewModel.loadTransaction(transactionId) }
        binding.checkboxTransferComplete.setOnCheckedChangeListener { _, _ -> updateCompletionButton() }
        binding.btnConfirmTransfer.setOnClickListener {
            viewModel.confirmEnergyTransfer(
                transactionId,
                binding.checkboxTransferComplete.isChecked
            )
        }
        binding.btnTransactionHistory.setOnClickListener {
            openTransactionHistory()
        }

        viewModel.transaction.observe(this) { transaction ->
            transaction?.let(::displayTransaction)
        }

        viewModel.isLoading.observe(this) { loading ->
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
            if (loading) {
                binding.confirmationContent.visibility = View.GONE
                binding.errorContent.visibility = View.GONE
            }
        }

        viewModel.loadError.observe(this) { error ->
            if (error == null) return@observe
            binding.progressBar.visibility = View.GONE
            binding.confirmationContent.visibility = View.GONE
            binding.errorContent.visibility = View.VISIBLE
            binding.tvError.text = error
        }

        viewModel.isCompleting.observe(this) { completing ->
            binding.completionProgress.visibility = if (completing) View.VISIBLE else View.GONE
            binding.btnConfirmTransfer.setText(
                if (completing) com.smartmicrogrid.R.string.completing_transaction
                else com.smartmicrogrid.R.string.complete_transaction
            )
            binding.checkboxTransferComplete.isEnabled = !completing
            updateCompletionButton()
        }

        viewModel.completionError.observe(this) { error ->
            binding.tvConfirmationMessage.text = error.orEmpty()
            binding.tvConfirmationMessage.visibility = if (error.isNullOrBlank()) View.GONE else View.VISIBLE
        }

        viewModel.completionResult.observe(this) { transaction ->
            transaction?.let {
                startActivity(
                    Intent(this, TransactionCompletionActivity::class.java).apply {
                        putExtra(TransactionDetailsActivity.EXTRA_TRANSACTION_ID, it.id.ifBlank { transactionId })
                        putExtra(TransactionCompletionActivity.EXTRA_COMPLETED_TRANSACTION, Gson().toJson(it))
                    }
                )
                viewModel.clearCompletionResult()
                finish()
            }
        }

        viewModel.scheduledTransfer.observe(this) { schedule ->
            binding.tvScheduledTransfer.text = schedule
        }

        viewModel.loadTransaction(transactionId)
    }

    private fun displayTransaction(transaction: Transaction) {
        binding.tvTransactionId.text = transaction.id.ifBlank { transactionId }
        binding.tvReservationId.text = valueOrFallback(transaction.reservationId)
        binding.tvProsumer.text = valueOrFallback(transaction.prosumerId)
        binding.tvEnergyAmount.text = getString(
            com.smartmicrogrid.R.string.energy_amount_kwh,
            transaction.energyAmount.toString()
        )
        binding.tvMicrogrid.text = transaction.microgridNodeId.ifBlank {
            getString(com.smartmicrogrid.R.string.not_available)
        }
        binding.tvStatus.text = transaction.status.ifBlank {
            getString(com.smartmicrogrid.R.string.not_available)
        }
        binding.confirmationContent.visibility = View.VISIBLE

        val status = transaction.status
        val isCompleted = status.equals("Completed", ignoreCase = true)
        val canComplete = status.equals("Verified", ignoreCase = true) ||
            status.equals("EnergyTransferInProgress", ignoreCase = true)
        binding.checkboxTransferComplete.visibility = if (canComplete) View.VISIBLE else View.GONE
        binding.btnConfirmTransfer.visibility = if (canComplete) View.VISIBLE else View.GONE
        binding.btnTransactionHistory.visibility = if (isCompleted) View.VISIBLE else View.GONE
        binding.tvConfirmationMessage.text = when {
            isCompleted -> getString(com.smartmicrogrid.R.string.transaction_already_completed)
            !canComplete -> getString(com.smartmicrogrid.R.string.transaction_cannot_be_completed)
            else -> ""
        }
        binding.tvConfirmationMessage.visibility = if (isCompleted || !canComplete) View.VISIBLE else View.GONE
        updateCompletionButton()
    }

    private fun updateCompletionButton() {
        val canComplete = viewModel.transaction.value?.status.equals("Verified", ignoreCase = true) ||
            viewModel.transaction.value?.status.equals("EnergyTransferInProgress", ignoreCase = true)
        binding.btnConfirmTransfer.isEnabled = canComplete &&
            binding.checkboxTransferComplete.isChecked && viewModel.isCompleting.value != true
    }

    private fun valueOrFallback(value: String?): String = value?.takeIf { it.isNotBlank() }
        ?: getString(com.smartmicrogrid.R.string.not_available)

    private fun openTransactionHistory() {
        startActivity(
            Intent(this, MicrogridOperatorMainActivity::class.java)
                .putExtra(MicrogridOperatorMainActivity.EXTRA_OPEN_HISTORY, true)
        )
        finish()
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
