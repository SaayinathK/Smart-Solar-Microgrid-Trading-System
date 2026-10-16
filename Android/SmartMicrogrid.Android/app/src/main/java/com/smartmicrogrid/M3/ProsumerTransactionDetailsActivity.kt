package com.smartmicrogrid.M3

import android.os.Bundle
import android.view.View
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.databinding.ActivityProsumerTransactionDetailsBinding
import com.smartmicrogrid.models.Transaction

class ProsumerTransactionDetailsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProsumerTransactionDetailsBinding
    private val viewModel: TransactionDetailsViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProsumerTransactionDetailsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = getString(R.string.prosumer_transaction_details)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        val transactionId = intent.getStringExtra(EXTRA_TRANSACTION_ID).orEmpty()
        binding.btnRetry.setOnClickListener { viewModel.loadTransaction(transactionId) }
        binding.btnQrAction.setOnClickListener {
            val transaction = viewModel.transaction.value ?: return@setOnClickListener
            if (transaction.qrCodeData.isNotBlank()) openQrScreen(transaction)
        }

        viewModel.transaction.observe(this) { transaction ->
            transaction?.let(::displayTransaction)
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
        }

        viewModel.loadTransaction(transactionId)
    }

    private fun displayTransaction(transaction: Transaction) {
        binding.progressBar.visibility = View.GONE
        binding.errorContent.visibility = View.GONE
        binding.detailsContent.visibility = View.VISIBLE
        binding.tvTransactionId.text = valueOrFallback(transaction.id)
        binding.tvTransactionCode.text = valueOrFallback(transaction.transactionCode)
        binding.tvReservationId.text = valueOrFallback(transaction.reservationId)
        binding.tvMicrogridNodeId.text = valueOrFallback(transaction.microgridNodeId)
        binding.tvEnergySlotId.text = valueOrFallback(transaction.energySlotId)
        binding.tvEnergyAmount.text = getString(
            R.string.energy_amount_kwh,
            transaction.energyAmount.toString()
        )
        binding.tvStatus.text = TransactionUiFormatters.statusLabel(transaction.status)
        binding.tvCreatedAt.text = TransactionUiFormatters.dateTime(transaction.createdAt)
        binding.tvUpdatedAt.text = TransactionUiFormatters.dateTime(transaction.updatedAt)
        binding.tvVerificationTime.text = TransactionUiFormatters.dateTime(transaction.verificationTime)
        binding.tvEnergyTransferTime.text = TransactionUiFormatters.dateTime(transaction.energyTransferTime)

        val hasQrData = transaction.qrCodeData.isNotBlank()
        binding.btnQrAction.visibility = if (hasQrData) View.VISIBLE else View.GONE
        binding.tvQrMessage.text = if (hasQrData) "" else getString(R.string.qr_not_generated_yet)
        binding.tvQrMessage.visibility = if (hasQrData) View.GONE else View.VISIBLE
    }

    private fun openQrScreen(transaction: Transaction) {
        startActivity(android.content.Intent(this, TransactionQrActivity::class.java).apply {
            putExtra(TransactionQrActivity.EXTRA_QR_CODE_DATA, transaction.qrCodeData)
            putExtra(TransactionQrActivity.EXTRA_TRANSACTION_ID, transaction.id)
            putExtra(TransactionQrActivity.EXTRA_TRANSACTION_CODE, transaction.transactionCode)
            putExtra(TransactionQrActivity.EXTRA_ENERGY_AMOUNT, transaction.energyAmount)
            putExtra(TransactionQrActivity.EXTRA_STATUS, transaction.status)
            putExtra(TransactionQrActivity.EXTRA_MICROGRID_NODE_ID, transaction.microgridNodeId)
            putExtra(TransactionQrActivity.EXTRA_ENERGY_SLOT_ID, transaction.energySlotId)
            putExtra(
                TransactionQrActivity.EXTRA_SCHEDULE_LABEL,
                buildString {
                    append(TransactionUiFormatters.dateTime(transaction.createdAt))
                    if (transaction.updatedAt.isNotBlank()) {
                        append(" • ")
                        append(TransactionUiFormatters.dateTime(transaction.updatedAt))
                    }
                }
            )
        })
    }

    private fun valueOrFallback(value: String?): String =
        value?.takeIf { it.isNotBlank() } ?: getString(R.string.not_available)

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }

    companion object {
        const val EXTRA_TRANSACTION_ID = "TRANSACTION_ID"
    }
}
