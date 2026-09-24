package com.smartmicrogrid.M3

import android.graphics.Bitmap
import android.graphics.Color
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel
import com.smartmicrogrid.R
import com.smartmicrogrid.databinding.ActivityTransactionQrBinding

class TransactionQrActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTransactionQrBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityTransactionQrBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = getString(R.string.transaction_qr)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        binding.tvTransactionId.text = intent.getStringExtra(EXTRA_TRANSACTION_ID)
            .orEmpty().ifBlank { getString(R.string.not_available) }
        binding.tvTransactionCode.text = intent.getStringExtra(EXTRA_TRANSACTION_CODE)
            .orEmpty().ifBlank { getString(R.string.not_available) }
        binding.tvEnergyAmount.text = getString(
            R.string.energy_amount_kwh,
            intent.getDoubleExtra(EXTRA_ENERGY_AMOUNT, 0.0).toString()
        )
        binding.tvStatus.text = intent.getStringExtra(EXTRA_STATUS)
            .orEmpty().ifBlank { getString(R.string.not_available) }

        val qrCodeData = intent.getStringExtra(EXTRA_QR_CODE_DATA).orEmpty()
        if (qrCodeData.isBlank()) {
            showQrError(getString(R.string.qr_payload_missing))
            return
        }

        renderQr(qrCodeData)
    }

    private fun renderQr(qrCodeData: String) {
        binding.progressBar.visibility = View.VISIBLE
        binding.qrImage.visibility = View.GONE
        binding.tvQrError.visibility = View.GONE

        try {
            val hints = mapOf(
                EncodeHintType.ERROR_CORRECTION to ErrorCorrectionLevel.M,
                EncodeHintType.MARGIN to 1
            )
            val matrix = QRCodeWriter().encode(
                qrCodeData,
                BarcodeFormat.QR_CODE,
                QR_SIZE,
                QR_SIZE,
                hints
            )
            val pixels = IntArray(QR_SIZE * QR_SIZE)
            for (y in 0 until QR_SIZE) {
                for (x in 0 until QR_SIZE) {
                    pixels[y * QR_SIZE + x] = if (matrix[x, y]) Color.BLACK else Color.WHITE
                }
            }
            binding.qrImage.setImageBitmap(
                Bitmap.createBitmap(pixels, QR_SIZE, QR_SIZE, Bitmap.Config.ARGB_8888)
            )
            binding.qrImage.visibility = View.VISIBLE
        } catch (_: Exception) {
            showQrError(getString(R.string.qr_render_failed))
        } finally {
            binding.progressBar.visibility = View.GONE
        }
    }

    private fun showQrError(message: String) {
        binding.qrImage.visibility = View.GONE
        binding.tvQrError.text = message
        binding.tvQrError.visibility = View.VISIBLE
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }

    companion object {
        const val EXTRA_QR_CODE_DATA = "QR_CODE_DATA"
        const val EXTRA_TRANSACTION_ID = "TRANSACTION_ID"
        const val EXTRA_TRANSACTION_CODE = "TRANSACTION_CODE"
        const val EXTRA_ENERGY_AMOUNT = "ENERGY_AMOUNT"
        const val EXTRA_STATUS = "TRANSACTION_STATUS"
        private const val QR_SIZE = 960
    }
}
