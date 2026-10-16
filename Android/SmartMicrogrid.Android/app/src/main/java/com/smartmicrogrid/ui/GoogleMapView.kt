package com.smartmicrogrid.ui

import android.annotation.SuppressLint
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.view.MotionEvent
import android.webkit.WebResourceRequest
import android.webkit.WebSettings
import android.webkit.WebView
import android.webkit.WebViewClient

/**
 * ============================================================================
 * Google Maps Interactive View Component
 * Project: Smart Solar Microgrid Trading System - SE4040 EAD
 * Purpose: Provides Google Maps API integration with interactive navigation,
 *          satellite/terrain views, and deep-linking into Google Maps app.
 * ============================================================================
 */
@SuppressLint("SetJavaScriptEnabled", "ClickableViewAccessibility")
class GoogleMapView(context: Context) : WebView(context) {

    private var currentLat: Double? = null
    private var currentLng: Double? = null
    private var currentName: String? = null

    init {
        settings.javaScriptEnabled = true
        settings.domStorageEnabled = true
        settings.loadWithOverviewMode = true
        settings.useWideViewPort = true
        settings.cacheMode = WebSettings.LOAD_DEFAULT
        settings.mixedContentMode = WebSettings.MIXED_CONTENT_NEVER_ALLOW
        settings.userAgentString = "${settings.userAgentString} SmartMicrogrid/1.0"

        webViewClient = object : WebViewClient() {
            override fun shouldOverrideUrlLoading(view: WebView, request: WebResourceRequest): Boolean {
                val url = request.url.toString()
                if (url.startsWith("https://maps.google.com") || url.startsWith("https://www.google.com/maps")) {
                    if (request.hasGesture()) {
                        openInGoogleMapsApp()
                        return true
                    }
                    return false
                }
                return false
            }
        }

        // Allow map panning and pinch-to-zoom inside parent ScrollView
        setOnTouchListener { view, event ->
            view.parent?.requestDisallowInterceptTouchEvent(
                event.actionMasked != MotionEvent.ACTION_UP && event.actionMasked != MotionEvent.ACTION_CANCEL
            )
            false
        }
    }

    /**
     * Renders the specified microgrid coordinates on Google Maps.
     */
    fun showLocation(latitude: Double, longitude: Double, name: String) {
        currentLat = latitude
        currentLng = longitude
        currentName = name

        val embedUrl = "https://maps.google.com/maps?q=$latitude,$longitude&hl=en&z=15&output=embed"
        loadUrl(embedUrl)
    }

    /**
     * Launches the native Google Maps app via Android Google Maps Intent API.
     */
    fun openInGoogleMapsApp() {
        val lat = currentLat ?: return
        val lng = currentLng ?: return
        val name = currentName ?: "Microgrid"

        val geoUri = Uri.parse("geo:$lat,$lng?q=$lat,$lng(${Uri.encode(name)})")
        val mapIntent = Intent(Intent.ACTION_VIEW, geoUri).apply {
            setPackage("com.google.android.apps.maps")
        }

        try {
            if (mapIntent.resolveActivity(context.packageManager) != null) {
                context.startActivity(mapIntent)
            } else {
                val webUri = Uri.parse("https://www.google.com/maps/search/?api=1&query=$lat,$lng")
                context.startActivity(Intent(Intent.ACTION_VIEW, webUri))
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    fun clearLocation() {
        currentLat = null
        currentLng = null
        currentName = null
        loadUrl("about:blank")
    }
}
