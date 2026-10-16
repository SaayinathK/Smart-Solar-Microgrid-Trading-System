package com.smartmicrogrid.M3

import android.os.Bundle
import android.view.View
import androidx.activity.viewModels
import com.smartmicrogrid.ui.WorkspaceActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.databinding.ActivityProsumerTransactionDetailsBinding
import com.smartmicrogrid.models.Transaction
import com.smartmicrogrid.operator.*
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch

class ProsumerTransactionDetailsActivity : WorkspaceActivity() {

    private lateinit var binding: ActivityProsumerTransactionDetailsBinding
    private val viewModel: TransactionDetailsViewModel by viewModels()
    private var transactionId = ""
    private var deliveryLabel: String? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProsumerTransactionDetailsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = getString(R.string.prosumer_transaction_details)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        transactionId = intent.getStringExtra(EXTRA_TRANSACTION_ID).orEmpty()
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

    }
    override fun onResume() { super.onResume(); if (transactionId.isNotBlank()) viewModel.loadTransaction(transactionId) }

    private fun displayTransaction(transaction: Transaction) {
        binding.progressBar.visibility = View.GONE
        binding.errorContent.visibility = View.GONE
        binding.detailsContent.visibility = View.VISIBLE
        OperatorMotion.enter(binding.detailsContent)
        binding.transferSummary.removeAllViews()
        binding.transferSummary.addView(OperatorVisuals.pill(this, TransactionUiFormatters.statusLabel(transaction.status), transaction.status in setOf("Cancelled", "Rejected")))
        binding.transferSummary.addView(label("${OperatorOverview.format(transaction.energyAmount)} kWh", 24f, true).apply { setPadding(0, context.dp(8), 0, context.dp(4)) })
        binding.transferSummary.addView(label(com.smartmicrogrid.prosumer.ProsumerRules.step(transaction.status), 14f))
        val progress = when (transaction.status) { "Pending" -> 20; "QRGenerated", "VerificationPending" -> 40; "Verified" -> 60; "EnergyTransferInProgress" -> 80; "Completed" -> 100; else -> 0 }
        binding.transferSummary.addView(OperatorMeterView(this, progress, OperatorVisuals.blue), android.widget.LinearLayout.LayoutParams(-1, dp(6)).apply { topMargin = dp(10) })
        lifecycleScope.launch {
            try {
                val booking = com.smartmicrogrid.data.remote.RetrofitClient.apiService.getReservationById(transaction.reservationId).data()
                deliveryLabel = "${TransactionUiFormatters.dateTime(booking.startTime)} to ${TransactionUiFormatters.dateTime(booking.endTime)}"
                binding.transferSummary.addView(label("Delivery: $deliveryLabel", 12f).apply { setPadding(0, context.dp(8), 0, 0) })
            } catch (cancelled: CancellationException) { throw cancelled }
            catch (_: Exception) { /* Transaction details and its QR remain available. */ }
        }
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
                deliveryLabel ?: "Delivery window unavailable. Check your booking."
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
