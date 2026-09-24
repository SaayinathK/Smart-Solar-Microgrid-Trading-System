package com.smartmicrogrid.M3

import android.os.Bundle
import android.content.Intent
import android.view.View
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
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
        binding.btnConfirmTransfer.setOnClickListener {
            viewModel.confirmEnergyTransfer(transactionId)
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
            binding.btnConfirmTransfer.isEnabled = !completing
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
                    }
                )
                finish()
            }
        }

        viewModel.loadTransaction(transactionId)
    }

    private fun displayTransaction(transaction: Transaction) {
        binding.tvTransactionId.text = transaction.id.ifBlank { transactionId }
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

        val isVerified = transaction.status.equals("Verified", ignoreCase = true)
        binding.btnConfirmTransfer.visibility = if (isVerified) View.VISIBLE else View.GONE
        if (!isVerified) {
            binding.tvConfirmationMessage.text =
                getString(com.smartmicrogrid.R.string.transfer_confirmation_requires_verified_status)
            binding.tvConfirmationMessage.visibility = View.VISIBLE
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
