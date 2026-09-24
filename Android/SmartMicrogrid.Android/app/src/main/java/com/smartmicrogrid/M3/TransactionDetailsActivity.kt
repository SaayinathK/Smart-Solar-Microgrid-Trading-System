package com.smartmicrogrid.M3

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.databinding.ActivityTransactionDetailsBinding

class TransactionDetailsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTransactionDetailsBinding
    private val viewModel: TransactionDetailsViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityTransactionDetailsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = "Transaction Details"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        val transactionId = intent.getStringExtra("TRANSACTION_ID") ?: ""

        binding.btnRetry.setOnClickListener {
            viewModel.loadTransaction(transactionId)
        }

        viewModel.transaction.observe(this) { transaction ->
            transaction?.let { displayTransaction(it) }
        }

        viewModel.isLoading.observe(this) { loading ->
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
            if (loading) {
                binding.detailsContent.visibility = View.GONE
                binding.errorContent.visibility = View.GONE
            }
        }

        viewModel.errorMessage.observe(this) { error ->
            if (error == null) return@observe
            binding.progressBar.visibility = View.GONE
            binding.detailsContent.visibility = View.GONE
            binding.errorContent.visibility = View.VISIBLE
            binding.tvError.text = error
            Toast.makeText(this, error, Toast.LENGTH_LONG).show()
        }

        viewModel.loadTransaction(transactionId)
    }

    private fun displayTransaction(transaction: com.smartmicrogrid.models.Transaction) {
        binding.progressBar.visibility = View.GONE
        binding.errorContent.visibility = View.GONE
        binding.detailsContent.visibility = View.VISIBLE

        binding.tvTransactionCode.text = valueOrFallback(transaction.transactionCode, transaction.id)
        binding.tvReservationId.text = valueOrFallback(transaction.reservationId)
        binding.tvProsumerId.text = valueOrFallback(transaction.prosumerId)
        binding.tvMicrogridNodeId.text = valueOrFallback(transaction.microgridNodeId)
        binding.tvEnergySlotId.text = valueOrFallback(transaction.energySlotId)
        binding.tvEnergyAmount.text = "${transaction.energyAmount} kWh"
        binding.tvStatus.text = valueOrFallback(transaction.status)
        binding.tvCreatedAt.text = valueOrFallback(transaction.createdAt)
        binding.tvUpdatedAt.text = valueOrFallback(transaction.updatedAt)
        binding.tvVerificationTime.text = valueOrFallback(transaction.verificationTime)
        binding.tvEnergyTransferTime.text = valueOrFallback(transaction.energyTransferTime)
    }

    private fun valueOrFallback(value: String?, fallback: String = "Not available"): String {
        return if (value.isNullOrBlank()) fallback else value
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}