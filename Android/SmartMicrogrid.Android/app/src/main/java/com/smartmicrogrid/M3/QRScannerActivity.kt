package com.smartmicrogrid.M3

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.google.zxing.integration.android.IntentIntegrator

class QRScannerActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        startScanner()
    }

    private fun startScanner() {
        if (!packageManager.hasSystemFeature(PackageManager.FEATURE_CAMERA_ANY)) {
            finishWithError("Camera is unavailable on this device.")
            return
        }

        if (ContextCompat.checkSelfPermission(
                this,
                Manifest.permission.CAMERA
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            ActivityCompat.requestPermissions(
                this,
                arrayOf(Manifest.permission.CAMERA),
                CAMERA_PERMISSION_REQUEST
            )
            return
        }

        IntentIntegrator(this)
            .setDesiredBarcodeFormats(IntentIntegrator.QR_CODE)
            .setPrompt("Scan the Smart Microgrid transaction QR code")
            .setBeepEnabled(false)
            .setOrientationLocked(false)
            .initiateScan()
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)

        if (requestCode == CAMERA_PERMISSION_REQUEST) {
            if (grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED) {
                startScanner()
            } else {
                finishWithError("Camera permission is required to scan a QR code.")
            }
        }
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)

        if (requestCode != IntentIntegrator.REQUEST_CODE) {
            return
        }

        val result = IntentIntegrator.parseActivityResult(requestCode, resultCode, data)
        if (result == null) {
            finishWithError("QR scan failed.")
            return
        }

        if (result.contents.isNullOrBlank()) {
            finishWithError("QR scan cancelled.")
            return
        }

        val contents = result.contents
        val payload = parseTransactionPayload(contents)
        if (payload == null) {
            finishWithError("Invalid Smart Microgrid transaction QR code.")
            return
        }

        setResult(
            RESULT_OK,
            Intent().apply {
                putExtra(EXTRA_QR_DATA, contents)
                putExtra(EXTRA_TRANSACTION_ID, payload.transactionId)
                putExtra(EXTRA_TRANSACTION_CODE, payload.transactionCode)
            }
        )
        finish()
    }

    private fun parseTransactionPayload(contents: String): TransactionQrPayload? {
        val parts = contents.split('|')
        if (parts.size != 4 || parts[0] != "SMART-MICROGRID" || parts[1] != "TRANSACTION") {
            return null
        }

        val transactionId = parts[2].trim()
        val transactionCode = parts[3].trim()
        if (transactionId.isEmpty() || transactionCode.isEmpty()) {
            return null
        }

        return TransactionQrPayload(transactionId, transactionCode)
    }

    private fun finishWithError(message: String) {
        setResult(
            RESULT_CANCELED,
            Intent().putExtra(EXTRA_ERROR, message)
        )
        finish()
    }

    private data class TransactionQrPayload(
        val transactionId: String,
        val transactionCode: String
    )

    companion object {
        const val EXTRA_QR_DATA = "QR_DATA"
        const val EXTRA_TRANSACTION_ID = "TRANSACTION_ID"
        const val EXTRA_TRANSACTION_CODE = "TRANSACTION_CODE"
        const val EXTRA_ERROR = "QR_SCAN_ERROR"

        private const val CAMERA_PERMISSION_REQUEST = 1201
    }
}