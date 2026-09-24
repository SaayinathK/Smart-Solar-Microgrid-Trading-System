package com.smartmicrogrid.M3

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.databinding.ActivityTransactionDetailsBinding

class TransactionDetailsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTransactionDetailsBinding
    private val viewModel: TransactionDetailsViewModel by viewModels()
    private val scannerLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == RESULT_OK) {
            val data = result.data
            val transactionId = data?.getStringExtra(QRScannerActivity.EXTRA_TRANSACTION_ID)
            val transactionCode = data?.getStringExtra(QRScannerActivity.EXTRA_TRANSACTION_CODE)
            val qrData = data?.getStringExtra(QRScannerActivity.EXTRA_QR_DATA)

            binding.tvScanResult.text = "Scanned transaction: $transactionId\nCode: $transactionCode\nPayload: $qrData"
            binding.tvScanResult.visibility = View.VISIBLE
        } else {
            val message = result.data?.getStringExtra(QRScannerActivity.EXTRA_ERROR)
                ?: "QR scan cancelled."
            Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityTransactionDetailsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = "Transaction Details"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        val transactionId = intent.getStringExtra("TRANSACTION_ID") ?: ""

        binding.btnScanQr.setOnClickListener {
            scannerLauncher.launch(Intent(this, QRScannerActivity::class.java))
        }

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
        binding.btnScanQr.visibility = View.VISIBLE
    }

    private fun valueOrFallback(value: String?, fallback: String = "Not available"): String {
        return if (value.isNullOrBlank()) fallback else value
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}