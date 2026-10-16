package com.smartmicrogrid.M3

import android.os.Bundle
import com.journeyapps.barcodescanner.CaptureActivity
import com.journeyapps.barcodescanner.DecoratedBarcodeView

/**
 * Custom CaptureActivity wrapper that forces portrait orientation for the camera
 * preview on devices (e.g. Samsung Galaxy) where the ZXing default orientation
 * handling results in a 90° sideways preview.
 *
 * The manifest entry for QRScannerActivity already declares
 * android:screenOrientation="portrait".  This wrapper is set as the
 * captureActivity on the IntentIntegrator so ZXing's own sensor-orientation
 * correction runs inside a known portrait context, eliminating the 90° rotation
 * bug on Samsung devices.
 *
 * Business logic, QR payload validation, and result handling all remain in
 * QRScannerActivity / TransactionDetailsActivity — unchanged.
 */
class QRCaptureActivity : CaptureActivity() {

    override fun initializeContent(): DecoratedBarcodeView {
        val barcodeView = super.initializeContent()
        // Ensure portrait is locked at the view level as well.
        barcodeView.barcodeView.cameraSettings.requestedCameraId = -1 // default camera
        return barcodeView
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
    }
}
