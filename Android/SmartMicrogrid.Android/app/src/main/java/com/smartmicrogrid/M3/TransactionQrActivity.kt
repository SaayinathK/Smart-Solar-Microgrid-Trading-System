package com.smartmicrogrid.M3

import android.content.ClipData
import android.content.ContentValues
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.Color
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.provider.MediaStore
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.FileProvider
import androidx.core.content.getSystemService
import androidx.core.view.ViewCompat
import com.google.android.material.color.MaterialColors
import com.google.android.material.snackbar.Snackbar
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel
import com.smartmicrogrid.R
import com.smartmicrogrid.databinding.ActivityTransactionQrBinding
import java.io.File
import java.io.FileOutputStream
import java.io.OutputStream
import java.util.Locale

class TransactionQrActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTransactionQrBinding
    private var currentQrBitmap: Bitmap? = null
    private var transactionCode: String = ""
    private var transactionId: String = ""

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityTransactionQrBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = getString(R.string.transaction_qr)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        binding.btnDownloadQr.setOnClickListener { saveQrImage() }
        binding.btnShareQr.setOnClickListener { shareQrImage() }
        binding.btnCopyTransactionId.setOnClickListener { copyTransactionId() }

        transactionCode = intent.getStringExtra(EXTRA_TRANSACTION_CODE).orEmpty().ifBlank {
            getString(R.string.not_available)
        }
        transactionId = intent.getStringExtra(EXTRA_TRANSACTION_ID).orEmpty().ifBlank {
            getString(R.string.not_available)
        }

        binding.tvTransactionCodeValue.text = transactionCode
        binding.tvTransactionIdValue.text = transactionId
        binding.tvEnergyAmountValue.text = formatEnergyAmount(
            intent.getDoubleExtra(EXTRA_ENERGY_AMOUNT, 0.0)
        )
        binding.tvMicrogridValue.text = intent.getStringExtra(EXTRA_MICROGRID_NODE_ID)
            .orEmpty().ifBlank { getString(R.string.not_available) }
        binding.tvEnergySlotValue.text = intent.getStringExtra(EXTRA_ENERGY_SLOT_ID)
            .orEmpty().ifBlank { getString(R.string.not_available) }
        binding.tvScheduleValue.text = intent.getStringExtra(EXTRA_SCHEDULE_LABEL)
            .orEmpty().ifBlank { getString(R.string.not_available) }

        val statusLabel = intent.getStringExtra(EXTRA_STATUS)
            .orEmpty().ifBlank { getString(R.string.not_available) }
            .let(TransactionUiFormatters::statusLabel)
        binding.tvStatus.text = statusLabel
        applyStatusStyle(statusLabel)

        val qrCodeData = intent.getStringExtra(EXTRA_QR_CODE_DATA).orEmpty().trim()
        if (qrCodeData.isBlank()) {
            showQrError(getString(R.string.qr_payload_missing))
            return
        }

        binding.heroContainer.animate().alpha(1f).scaleX(1f).scaleY(1f).setDuration(200L).start()
        renderQr(qrCodeData)
    }

    private fun applyStatusStyle(statusLabel: String) {
        val isReady = statusLabel.equals(getString(R.string.ready_for_verification), ignoreCase = true)
        val backgroundColor = if (isReady) {
            MaterialColors.getColor(binding.statusCard, com.google.android.material.R.attr.colorPrimaryContainer)
        } else {
            MaterialColors.getColor(binding.statusCard, com.google.android.material.R.attr.colorSecondaryContainer)
        }
        val foregroundColor = if (isReady) {
            MaterialColors.getColor(binding.statusCard, com.google.android.material.R.attr.colorOnPrimaryContainer)
        } else {
            MaterialColors.getColor(binding.statusCard, com.google.android.material.R.attr.colorOnSecondaryContainer)
        }
        binding.statusCard.setCardBackgroundColor(backgroundColor)
        binding.tvStatus.setTextColor(foregroundColor)
        // Use backgroundTintList to tint the oval shape drawable (ic_status_dot.xml)
        // without replacing it with a plain ColorDrawable (which would destroy the circle).
        binding.statusDot.backgroundTintList =
            android.content.res.ColorStateList.valueOf(foregroundColor)
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
            val bitmap = Bitmap.createBitmap(
                pixels,
                QR_SIZE,
                QR_SIZE,
                Bitmap.Config.ARGB_8888
            )
            currentQrBitmap = bitmap
            binding.qrImage.setImageBitmap(bitmap)
            binding.qrImage.visibility = View.VISIBLE
            binding.qrImage.alpha = 0f
            binding.qrImage.scaleX = 0.97f
            binding.qrImage.scaleY = 0.97f
            binding.qrImage.animate().alpha(1f).scaleX(1f).scaleY(1f).setDuration(200L).start()
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

    private fun formatEnergyAmount(value: Double): String =
        String.format(Locale.getDefault(), "%.1f kWh", value)

    private fun copyTransactionId() {
        val clipboard = getSystemService(android.content.ClipboardManager::class.java)
        val clip = ClipData.newPlainText("Transaction ID", transactionId)
        clipboard?.setPrimaryClip(clip)
        Snackbar.make(binding.root, getString(R.string.transaction_id_copied), Snackbar.LENGTH_SHORT).show()
    }

    private fun saveQrImage() {
        val bitmap = currentQrBitmap ?: binding.qrImage.drawable?.let { drawable ->
            val bitmapDrawable = drawable as? android.graphics.drawable.BitmapDrawable
            bitmapDrawable?.bitmap
        } ?: run {
            Snackbar.make(binding.root, getString(R.string.qr_save_failed), Snackbar.LENGTH_SHORT).show()
            return
        }

        val fileName = buildQrFileName(transactionCode)
        val saved = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            saveQrToMediaStore(bitmap, fileName)
        } else {
            saveQrToExternalAppStorage(bitmap, fileName)
        }

        if (saved) {
            binding.btnDownloadQr.icon = getDrawable(R.drawable.ic_check)
            binding.btnDownloadQr.text = getString(R.string.qr_saved_successfully)
            binding.btnDownloadQr.isEnabled = false
            binding.btnDownloadQr.postDelayed({
                binding.btnDownloadQr.icon = getDrawable(R.drawable.ic_download)
                binding.btnDownloadQr.text = getString(R.string.download_qr)
                binding.btnDownloadQr.isEnabled = true
            }, 1500)
            Snackbar.make(binding.root, getString(R.string.qr_saved_successfully), Snackbar.LENGTH_SHORT).show()
        } else {
            Snackbar.make(binding.root, getString(R.string.qr_save_failed), Snackbar.LENGTH_SHORT).show()
        }
    }

    private fun saveQrToMediaStore(bitmap: Bitmap, fileName: String): Boolean {
        val values = ContentValues().apply {
            put(MediaStore.Images.Media.DISPLAY_NAME, fileName)
            put(MediaStore.Images.Media.MIME_TYPE, "image/png")
            put(MediaStore.Images.Media.RELATIVE_PATH, Environment.DIRECTORY_PICTURES + "/SmartMicrogrid")
            put(MediaStore.Images.Media.IS_PENDING, 1)
        }

        val uri = contentResolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values)
            ?: return false

        return runCatching {
            contentResolver.openOutputStream(uri)?.use { output ->
                bitmap.compress(Bitmap.CompressFormat.PNG, 100, output)
            }
            values.clear()
            values.put(MediaStore.Images.Media.IS_PENDING, 0)
            contentResolver.update(uri, values, null, null)
            true
        }.getOrDefault(false)
    }

    private fun saveQrToExternalAppStorage(bitmap: Bitmap, fileName: String): Boolean {
        val directory = File(getExternalFilesDir(Environment.DIRECTORY_PICTURES), "SmartMicrogrid")
        if (!directory.exists()) {
            directory.mkdirs()
        }
        val file = File(directory, fileName)
        return runCatching {
            FileOutputStream(file).use { output ->
                bitmap.compress(Bitmap.CompressFormat.PNG, 100, output)
            }
            true
        }.getOrDefault(false)
    }

    private fun shareQrImage() {
        val bitmap = currentQrBitmap ?: return Toast.makeText(
            this,
            getString(R.string.qr_save_failed),
            Toast.LENGTH_SHORT
        ).show()

        val file = File(cacheDir, buildQrFileName(transactionCode))
        runCatching {
            FileOutputStream(file).use { output ->
                bitmap.compress(Bitmap.CompressFormat.PNG, 100, output)
            }
        }.onFailure {
            Toast.makeText(this, getString(R.string.qr_save_failed), Toast.LENGTH_SHORT).show()
            return
        }

        val uri = FileProvider.getUriForFile(
            this,
            "$packageName.fileprovider",
            file
        )
        val shareIntent = Intent(Intent.ACTION_SEND).apply {
            type = "image/png"
            putExtra(Intent.EXTRA_STREAM, uri)
            putExtra(Intent.EXTRA_SUBJECT, "Smart Microgrid Energy Transaction QR")
            putExtra(
                Intent.EXTRA_TEXT,
                "Smart Microgrid\nEnergy Transaction QR\n\nTransaction:\n${transactionCode}\n\nEnergy:\n${formatEnergyAmount(intent.getDoubleExtra(EXTRA_ENERGY_AMOUNT, 0.0))}\n\nMicrogrid:\n${binding.tvMicrogridValue.text}"
            )
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }

        startActivity(Intent.createChooser(shareIntent, "Share QR"))
        Snackbar.make(binding.root, getString(R.string.share_sheet_opened), Snackbar.LENGTH_SHORT).show()
    }

    private fun buildQrFileName(code: String): String {
        val safeCode = code.ifBlank { "transaction" }
            .replace(Regex("[^A-Za-z0-9._-]+"), "_")
            .trim('_')
        return "smart_microgrid_${safeCode.ifBlank { "transaction" }}.png"
            .lowercase(Locale.getDefault())
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
        const val EXTRA_MICROGRID_NODE_ID = "MICROGRID_NODE_ID"
        const val EXTRA_ENERGY_SLOT_ID = "ENERGY_SLOT_ID"
        const val EXTRA_SCHEDULE_LABEL = "SCHEDULE_LABEL"
        private const val QR_SIZE = 960
    }
}
