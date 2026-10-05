package com.smartmicrogrid.M3

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.activity.OnBackPressedCallback
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.google.gson.Gson
import com.smartmicrogrid.databinding.ActivityTransactionCompletionBinding
import com.smartmicrogrid.models.Transaction

class TransactionCompletionActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTransactionCompletionBinding
    private val viewModel: EnergyTransferConfirmationViewModel by viewModels()
    private var transactionId = ""
    private var didAnimateCompletion = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityTransactionCompletionBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = getString(com.smartmicrogrid.R.string.transaction_completed)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() = openTransactionHistory()
        })

        transactionId = intent.getStringExtra(TransactionDetailsActivity.EXTRA_TRANSACTION_ID).orEmpty()
        binding.btnRetry.setOnClickListener { viewModel.loadTransaction(transactionId) }
        binding.btnViewTransaction.setOnClickListener {
            startActivity(
                Intent(this, TransactionDetailsActivity::class.java).apply {
                    putExtra(TransactionDetailsActivity.EXTRA_TRANSACTION_ID, transactionId)
                }
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
                binding.completionContent.visibility = View.GONE
                binding.errorContent.visibility = View.GONE
            }
        }
        viewModel.loadError.observe(this) { error ->
            if (error == null) return@observe
            binding.progressBar.visibility = View.GONE
            binding.completionContent.visibility = View.GONE
            binding.errorContent.visibility = View.VISIBLE
            binding.tvError.text = error
        }

        val completedTransaction = intent.getStringExtra(EXTRA_COMPLETED_TRANSACTION)
            ?.let { json -> runCatching { Gson().fromJson(json, Transaction::class.java) }.getOrNull() }
        if (completedTransaction != null) {
            displayTransaction(completedTransaction)
        } else {
            viewModel.loadTransaction(transactionId)
        }
    }

    private fun displayTransaction(transaction: Transaction) {
        binding.progressBar.visibility = View.GONE
        binding.errorContent.visibility = View.GONE
        binding.completionContent.visibility = View.VISIBLE
        binding.tvTransactionId.text = valueOrFallback(transaction.id, transactionId)
        binding.tvTransactionCode.text = valueOrFallback(transaction.transactionCode)
        binding.tvReservationId.text = valueOrFallback(transaction.reservationId)
        binding.tvProsumer.text = valueOrFallback(transaction.prosumerId)
        binding.tvEnergyAmount.text = "${transaction.energyAmount} kWh"
        binding.tvMicrogrid.text = valueOrFallback(transaction.microgridNodeId)
        binding.tvEnergySlot.text = valueOrFallback(transaction.energySlotId)
        binding.tvStatus.text = TransactionUiFormatters.statusLabel(transaction.status)
        binding.tvCompletionTime.text = TransactionUiFormatters.dateTime(transaction.energyTransferTime)
        if (!didAnimateCompletion) {
            didAnimateCompletion = true
            binding.tvCompletionSuccessIcon.apply {
                alpha = 0f
                scaleX = 0.9f
                scaleY = 0.9f
                animate().alpha(1f).scaleX(1f).scaleY(1f).setDuration(220L).start()
            }
        }
    }

    private fun valueOrFallback(value: String?, fallback: String = getString(com.smartmicrogrid.R.string.not_available)): String =
        value?.takeIf { it.isNotBlank() } ?: fallback

    override fun onSupportNavigateUp(): Boolean {
        openTransactionHistory()
        return true
    }

    private fun openTransactionHistory() {
        startActivity(
            Intent(this, MicrogridOperatorMainActivity::class.java)
                .putExtra(MicrogridOperatorMainActivity.EXTRA_OPEN_HISTORY, true)
        )
        finish()
    }

    companion object {
        const val EXTRA_COMPLETED_TRANSACTION = "COMPLETED_TRANSACTION"
    }
}
