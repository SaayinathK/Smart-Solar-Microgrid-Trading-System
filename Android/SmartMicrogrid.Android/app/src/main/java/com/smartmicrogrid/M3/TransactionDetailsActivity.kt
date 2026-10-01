package com.smartmicrogrid.M3

import android.content.Intent
import android.content.res.ColorStateList
import android.graphics.Color
import android.app.Dialog
import android.os.Bundle
import android.view.View
import android.view.WindowManager
import android.graphics.drawable.ColorDrawable
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.databinding.ActivityTransactionDetailsBinding
import com.smartmicrogrid.databinding.DialogVerificationSuccessBinding

class TransactionDetailsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTransactionDetailsBinding
    private val viewModel: TransactionDetailsViewModel by viewModels()
    private var displayedTransactionId = ""
    private var scannedQrData: String? = null
    private var lastDisplayedStatus: String? = null
    private var verificationSuccessDialog: Dialog? = null
    private val scannerLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == RESULT_OK) {
            val data = result.data
            val transactionId = data?.getStringExtra(QRScannerActivity.EXTRA_TRANSACTION_ID)
            val transactionCode = data?.getStringExtra(QRScannerActivity.EXTRA_TRANSACTION_CODE)
            val qrData = data?.getStringExtra(QRScannerActivity.EXTRA_QR_DATA)
            val current = viewModel.transaction.value
            val parsed = qrData?.let(::parseTransactionQr)

            when {
                parsed == null || transactionId.isNullOrBlank() || transactionCode.isNullOrBlank() -> {
                    showScanMessage(getString(R.string.invalid_transaction_qr), allowRetry = true)
                }
                transactionId != displayedTransactionId || parsed.first != displayedTransactionId -> {
                    showScanMessage(getString(R.string.different_transaction_qr), allowRetry = true)
                }
                current == null -> {
                    showScanMessage(getString(R.string.transaction_not_found), allowRetry = true)
                }
                current.status.equals("Verified", ignoreCase = true) -> {
                    showScanMessage(getString(R.string.transaction_already_verified), allowRetry = false)
                }
                !isVerificationEligible(current.status) -> {
                    showScanMessage(getString(R.string.transaction_cannot_be_verified), allowRetry = false)
                }
                parsed.second != current.transactionCode || transactionCode != current.transactionCode -> {
                    showScanMessage(getString(R.string.invalid_transaction_qr), allowRetry = true)
                }
                else -> {
                    scannedQrData = qrData
                    binding.tvScanResult.text = "Transaction code: $transactionCode"
                    binding.tvScanResult.visibility = View.VISIBLE
                    binding.verificationControls.visibility = View.VISIBLE
                    binding.tvVerificationMessage.text = "QR matches this transaction. Select Verify Transaction to continue."
                    binding.tvVerificationMessage.visibility = View.VISIBLE
                    binding.btnScanQr.setText(R.string.scan_again)
                }
            }
        } else {
            val message = result.data?.getStringExtra(QRScannerActivity.EXTRA_ERROR)
                ?: "QR scan cancelled."
            showScanMessage(message, allowRetry = true)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityTransactionDetailsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = "Transaction Details"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        val transactionId = intent.getStringExtra("TRANSACTION_ID") ?: ""
        displayedTransactionId = transactionId

        binding.btnScanQr.setOnClickListener {
            scannedQrData = null
            binding.verificationControls.visibility = View.GONE
            binding.tvScanResult.visibility = View.GONE
            binding.tvVerificationMessage.visibility = View.GONE
            scannerLauncher.launch(Intent(this, QRScannerActivity::class.java))
        }

        binding.btnGenerateQr.setOnClickListener {
            viewModel.generateTransactionQr(displayedTransactionId)
        }

        binding.btnViewQr.setOnClickListener {
            viewModel.transaction.value?.let { transaction ->
                if (transaction.qrCodeData.isNotBlank()) {
                    openQrScreen(
                        transaction.id,
                        transaction.transactionCode,
                        transaction.energyAmount,
                        transaction.status,
                        transaction.qrCodeData,
                        transaction.microgridNodeId,
                        transaction.energySlotId,
                        buildString {
                            append(TransactionUiFormatters.dateTime(transaction.createdAt))
                            if (transaction.updatedAt.isNotBlank()) {
                                append(" • ")
                                append(TransactionUiFormatters.dateTime(transaction.updatedAt))
                            }
                        }
                    )
                }
            }
        }

        binding.btnConfirmEnergyTransfer.setOnClickListener {
            if (viewModel.transaction.value?.status.equals("Verified", ignoreCase = true)) {
                startActivity(
                    Intent(this, EnergyTransferConfirmationActivity::class.java).apply {
                        putExtra(EXTRA_TRANSACTION_ID, displayedTransactionId)
                    }
                )
            }
        }

        binding.btnCancelVerification.setOnClickListener {
            scannedQrData = null
            binding.verificationControls.visibility = View.GONE
            binding.tvVerificationMessage.visibility = View.GONE
            binding.btnScanQr.setText(R.string.scan_again)
        }

        binding.btnVerifyTransaction.setOnClickListener {
            val qrData = scannedQrData
            if (qrData.isNullOrBlank()) {
                binding.tvVerificationMessage.text = "Scan a matching transaction QR code first."
                binding.tvVerificationMessage.visibility = View.VISIBLE
            } else {
                viewModel.verifyTransaction(displayedTransactionId, qrData)
            }
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
            // Error is shown in the full-screen error panel above — Toast not needed.
        }

        viewModel.isVerifying.observe(this) { verifying ->
            binding.verificationProgress.visibility = if (verifying) View.VISIBLE else View.GONE
            binding.btnVerifyTransaction.isEnabled = !verifying
            binding.btnCancelVerification.isEnabled = !verifying
        }

        viewModel.isGeneratingQr.observe(this) { generating ->
            binding.qrGenerationProgress.visibility = if (generating) View.VISIBLE else View.GONE
            binding.btnGenerateQr.isEnabled = !generating
            binding.btnGenerateQr.setText(
                if (generating) R.string.generating_qr else R.string.generate_qr
            )
        }

        viewModel.qrGenerationResult.observe(this) { result ->
            result ?: return@observe
            val transaction = viewModel.transaction.value
            if (transaction != null) {
                openQrScreen(
                    result.transactionId,
                    result.transactionCode,
                    transaction.energyAmount,
                    result.status,
                    result.qrCodeData,
                    transaction.microgridNodeId,
                    transaction.energySlotId,
                    buildString {
                        append(TransactionUiFormatters.dateTime(transaction.createdAt))
                        if (transaction.updatedAt.isNotBlank()) {
                            append(" • ")
                            append(TransactionUiFormatters.dateTime(transaction.updatedAt))
                        }
                    }
                )
            }
            viewModel.clearQrGenerationResult()
        }

        viewModel.qrGenerationError.observe(this) { error ->
            if (error.isNullOrBlank()) {
                binding.tvQrGenerationMessage.visibility = View.GONE
                return@observe
            }
            binding.tvQrGenerationMessage.text = error
            binding.tvQrGenerationMessage.visibility = View.VISIBLE
            // QR generation error is shown inline in tvQrGenerationMessage — Toast not needed.
        }

        viewModel.verificationResult.observe(this) { transaction ->
            transaction?.let {
                binding.verificationControls.visibility = View.GONE
                binding.tvVerificationMessage.text = if (
                    it.status.equals("Verified", ignoreCase = true)
                ) getString(R.string.transaction_verified) else "Verification response received. Status: ${it.status}"
                binding.tvVerificationMessage.visibility = View.VISIBLE
                scannedQrData = null
                if (it.status.equals("Verified", ignoreCase = true)) {
                    showVerificationSuccessDialog(it)
                }
            }
        }

        viewModel.verificationError.observe(this) { error ->
            error?.let {
                binding.tvVerificationMessage.text = it
                binding.tvVerificationMessage.visibility = View.VISIBLE
                scannedQrData = null
                binding.verificationControls.visibility = View.GONE
                if (isVerificationEligible(viewModel.transaction.value?.status.orEmpty())) {
                    binding.btnScanQr.visibility = View.VISIBLE
                    binding.btnScanQr.setText(R.string.scan_again)
                }
                if (it == getString(R.string.transaction_already_verified)) {
                    viewModel.loadTransaction(displayedTransactionId)
                }
                // Inline tvVerificationMessage already shows the error — Toast removed to avoid duplication.
            }
        }

        viewModel.loadTransaction(transactionId)
    }

    private fun showVerificationSuccessDialog(transaction: com.smartmicrogrid.models.Transaction) {
        if (verificationSuccessDialog?.isShowing == true || isFinishing || isDestroyed) return

        val dialog = Dialog(this)
        val dialogBinding = DialogVerificationSuccessBinding.inflate(layoutInflater)
        dialog.setContentView(dialogBinding.root)
        dialog.setCanceledOnTouchOutside(false)
        dialog.window?.apply {
            setBackgroundDrawable(ColorDrawable(Color.TRANSPARENT))
            addFlags(WindowManager.LayoutParams.FLAG_DIM_BEHIND)
            attributes = attributes.apply { dimAmount = 0.68f }
        }

        dialogBinding.tvEnergyValue.text = getString(
            R.string.energy_amount_kwh,
            transaction.energyAmount.toString()
        )
        dialogBinding.tvTransactionValue.text = transaction.transactionCode.ifBlank {
            transaction.id.ifBlank { getString(R.string.not_available) }
        }
        if (transaction.microgridNodeId.isBlank()) {
            dialogBinding.microgridSummary.visibility = View.GONE
        } else {
            dialogBinding.tvMicrogridValue.text = transaction.microgridNodeId
        }
        dialogBinding.btnClose.setOnClickListener { dialog.dismiss() }
        dialogBinding.btnContinue.setOnClickListener {
            dialog.dismiss()
            binding.btnConfirmEnergyTransfer.performClick()
        }
        dialog.setOnShowListener {
            val width = resources.displayMetrics.widthPixels
            val horizontalMargin = (32 * resources.displayMetrics.density).toInt()
            val maxWidth = (420 * resources.displayMetrics.density).toInt()
            dialog.window?.setLayout((width - horizontalMargin).coerceAtMost(maxWidth), WindowManager.LayoutParams.WRAP_CONTENT)
            dialogBinding.successIconContainer.apply {
                alpha = 0f
                scaleX = 0.86f
                scaleY = 0.86f
                animate().alpha(1f).scaleX(1f).scaleY(1f).setDuration(240L).start()
            }
        }
        dialog.setOnDismissListener { verificationSuccessDialog = null }
        verificationSuccessDialog = dialog
        dialog.show()
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
        binding.tvStatus.text = TransactionUiFormatters.statusLabel(transaction.status)
        binding.chipStatus.text = TransactionUiFormatters.statusLabel(transaction.status)
        val (statusTitle, statusDescription) = TransactionUiFormatters.statusSummary(transaction.status)
        binding.tvStatusMessage.text = statusDescription
        binding.tvStatusTitle.text = statusTitle
        val (statusBackground, statusForeground) = when (transaction.status.lowercase()) {
            "completed", "verified" -> R.color.m3_status_success_background to R.color.m3_status_success_foreground
            "rejected", "cancelled" -> R.color.m3_status_error_background to R.color.m3_status_error_foreground
            "pending" -> R.color.m3_status_warning_background to R.color.m3_status_warning_foreground
            else -> R.color.m3_status_info_background to R.color.m3_status_info_foreground
        }
        binding.chipStatus.chipBackgroundColor = ColorStateList.valueOf(getColor(statusBackground))
        binding.chipStatus.setTextColor(getColor(statusForeground))
        binding.tvStatus.setTextColor(getColor(statusForeground))
        // Issue 8: timestamps now show just the value (labels are separate Views in layout)
        binding.tvCreatedAt.text = TransactionUiFormatters.dateTime(transaction.createdAt)
        binding.tvUpdatedAt.text = TransactionUiFormatters.dateTime(transaction.updatedAt)
        binding.tvVerificationTime.text = TransactionUiFormatters.dateTime(transaction.verificationTime)
        binding.tvEnergyTransferTime.text = TransactionUiFormatters.dateTime(transaction.energyTransferTime)
        updateTimeline(transaction.status)
        binding.verificationSuccessCard.visibility = if (
            transaction.status.equals("Verified", ignoreCase = true)
        ) View.VISIBLE else View.GONE
        if (transaction.status.equals("Verified", ignoreCase = true) &&
            !lastDisplayedStatus.equals("Verified", ignoreCase = true)
        ) {
            binding.tvVerificationSuccessIcon.apply {
                alpha = 0f
                scaleX = 0.9f
                scaleY = 0.9f
                visibility = View.VISIBLE
                animate().alpha(1f).scaleX(1f).scaleY(1f).setDuration(210L).start()
            }
        }
        val isPending = transaction.status.equals("Pending", ignoreCase = true)
        val hasExistingQr = transaction.qrCodeData.isNotBlank() && (
            transaction.status.equals("QRGenerated", ignoreCase = true) ||
                transaction.status.equals("VerificationPending", ignoreCase = true)
            )
        binding.btnGenerateQr.visibility = if (isPending) View.VISIBLE else View.GONE
        binding.btnViewQr.visibility = if (hasExistingQr) View.VISIBLE else View.GONE
        val verificationEligible = isVerificationEligible(transaction.status)
        binding.btnScanQr.visibility = if (verificationEligible) View.VISIBLE else View.GONE
        binding.btnScanQr.setText(
            if (verificationEligible) R.string.scan_transaction_qr else R.string.scan_again
        )
        if (transaction.status.equals("Verified", ignoreCase = true) &&
            binding.tvVerificationMessage.visibility != View.VISIBLE
        ) {
            binding.tvVerificationMessage.text = getString(R.string.transaction_already_verified)
            binding.tvVerificationMessage.visibility = View.VISIBLE
        }
        val isVerified = transaction.status.equals("Verified", ignoreCase = true)
        val isTransferInProgress = transaction.status.equals("EnergyTransferInProgress", ignoreCase = true)
        binding.btnConfirmEnergyTransfer.visibility = if (isVerified || isTransferInProgress) {
            View.VISIBLE
        } else {
            View.GONE
        }
        binding.btnConfirmEnergyTransfer.setText(
            if (isTransferInProgress) R.string.continue_to_complete_transaction
            else R.string.continue_to_energy_transfer
        )
        lastDisplayedStatus = transaction.status
    }

    /**
     * Issue 3 fix: Updates the horizontal stepper nodes and connectors using proper
     * circle drawables (bg_timeline_node_complete / _active / _upcoming) instead of
     * Unicode symbols ✓●○ in TextViews.  Each node is a View; its sibling label
     * is a separate companion TextView.
     */
    private fun updateTimeline(status: String) {
        val step = when (status.lowercase()) {
            "pending" -> 0
            "qrgenerated", "verificationpending" -> 1
            "verified" -> 2
            "energytransferinprogress" -> 3
            "completed" -> 4
            else -> -1
        }
        binding.transactionTimeline.visibility = if (step >= 0) View.VISIBLE else View.GONE

        // Node views (circles) — now plain View elements with background drawables.
        val nodeViews = listOf(
            binding.tvTimelineCreated,
            binding.tvTimelineQr,
            binding.tvTimelineVerified,
            binding.tvTimelineTransfer,
            binding.tvTimelineCompleted
        )
        // Companion label TextViews.
        val labelViews = listOf(
            binding.tvTimelineCreatedLabel,
            binding.tvTimelineQrLabel,
            binding.tvTimelineVerifiedLabel,
            binding.tvTimelineTransferLabel,
            binding.tvTimelineCompletedLabel
        )

        nodeViews.forEachIndexed { index, nodeView ->
            val isComplete = index < step
            val isCurrent = index == step
            val drawable = when {
                isComplete -> R.drawable.bg_timeline_node_complete
                isCurrent  -> R.drawable.bg_timeline_node_active
                else       -> R.drawable.bg_timeline_node_upcoming
            }
            nodeView.background = getDrawable(drawable)
            val labelColor = when {
                isComplete -> R.color.m3_success
                isCurrent  -> R.color.primary
                else       -> R.color.m3_text_secondary
            }
            labelViews[index].setTextColor(getColor(labelColor))
            labelViews[index].alpha = if (index <= step) 1f else 0.65f
        }

        // Connector lines.
        val connectorViews = listOf(
            binding.timelineConnector1,
            binding.timelineConnector2,
            binding.timelineConnector3,
            binding.timelineConnector4
        )
        connectorViews.forEachIndexed { index, view ->
            val active = step > index
            view.setBackgroundColor(
                getColor(if (active) R.color.m3_success else R.color.m3_text_secondary)
            )
            view.alpha = if (active) 1f else 0.35f
        }
    }

    private fun showScanMessage(message: String, allowRetry: Boolean) {
        scannedQrData = null
        binding.verificationControls.visibility = View.GONE
        binding.tvScanResult.visibility = View.GONE
        binding.tvVerificationMessage.text = message
        binding.tvVerificationMessage.visibility = View.VISIBLE
        val canRetry = allowRetry && isVerificationEligible(viewModel.transaction.value?.status.orEmpty())
        binding.btnScanQr.visibility = if (canRetry) View.VISIBLE else View.GONE
        if (canRetry) binding.btnScanQr.setText(R.string.scan_again)
        // Inline tvVerificationMessage already shows the message — Toast removed to avoid duplication.
    }

    private fun parseTransactionQr(qrData: String): Pair<String, String>? {
        val parts = qrData.split('|')
        if (parts.size != 4 || parts[0] != "SMART-MICROGRID" || parts[1] != "TRANSACTION") {
            return null
        }
        val transactionId = parts[2].trim()
        val transactionCode = parts[3].trim()
        if (transactionId.isBlank() || transactionCode.isBlank()) return null
        return transactionId to transactionCode
    }

    private fun isVerificationEligible(status: String): Boolean =
        status.equals("QRGenerated", ignoreCase = true) ||
            status.equals("VerificationPending", ignoreCase = true)

    private fun openQrScreen(
        transactionId: String,
        transactionCode: String,
        energyAmount: Double,
        status: String,
        qrCodeData: String,
        microgridNodeId: String = "",
        energySlotId: String = "",
        scheduleLabel: String = ""
    ) {
        startActivity(Intent(this, TransactionQrActivity::class.java).apply {
            putExtra(TransactionQrActivity.EXTRA_TRANSACTION_ID, transactionId)
            putExtra(TransactionQrActivity.EXTRA_TRANSACTION_CODE, transactionCode)
            putExtra(TransactionQrActivity.EXTRA_ENERGY_AMOUNT, energyAmount)
            putExtra(TransactionQrActivity.EXTRA_STATUS, status)
            putExtra(TransactionQrActivity.EXTRA_QR_CODE_DATA, qrCodeData)
            putExtra(TransactionQrActivity.EXTRA_MICROGRID_NODE_ID, microgridNodeId)
            putExtra(TransactionQrActivity.EXTRA_ENERGY_SLOT_ID, energySlotId)
            putExtra(TransactionQrActivity.EXTRA_SCHEDULE_LABEL, scheduleLabel)
        })
    }

    private fun valueOrFallback(value: String?, fallback: String = "Not available"): String {
        return if (value.isNullOrBlank()) fallback else value
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }

    companion object {
        const val EXTRA_TRANSACTION_ID = "TRANSACTION_ID"
    }
}
