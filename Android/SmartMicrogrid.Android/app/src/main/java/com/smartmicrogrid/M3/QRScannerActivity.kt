package com.smartmicrogrid.M3

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import com.smartmicrogrid.ui.WorkspaceActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.google.zxing.integration.android.IntentIntegrator

class QRScannerActivity : WorkspaceActivity() {

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
            .setOrientationLocked(true)
            // Use custom portrait-locked CaptureActivity to fix the 90° sideways
            // camera preview on Samsung Galaxy devices (Issue #1 — camera rotation).
            .setCaptureActivity(QRCaptureActivity::class.java)
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
                finishWithError("Camera permission is required to scan transaction QR codes.")
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
        val payload = TransactionQrPayload.parse(contents)
        if (payload == null) {
            finishWithError("Invalid Smart Microgrid transaction QR.")
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

    private fun finishWithError(message: String) {
        setResult(
            RESULT_CANCELED,
            Intent().putExtra(EXTRA_ERROR, message)
        )
        finish()
    }

    companion object {
        const val EXTRA_QR_DATA = "QR_DATA"
        const val EXTRA_TRANSACTION_ID = "TRANSACTION_ID"
        const val EXTRA_TRANSACTION_CODE = "TRANSACTION_CODE"
        const val EXTRA_ERROR = "QR_SCAN_ERROR"

        private const val CAMERA_PERMISSION_REQUEST = 1201
    }
}
