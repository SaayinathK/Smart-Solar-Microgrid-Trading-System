package com.smartmicrogrid.ui

import android.annotation.SuppressLint
import android.content.Context
import android.content.Intent
import android.view.MotionEvent
import android.webkit.WebResourceRequest
import android.webkit.WebResourceResponse
import android.webkit.WebSettings
import android.webkit.WebView
import android.webkit.WebViewClient
import org.json.JSONObject
import java.io.ByteArrayInputStream

/** Local Leaflet page with HTTPS tiles; no JavaScript bridge or account/API key. */
@SuppressLint("SetJavaScriptEnabled", "ClickableViewAccessibility")
class OpenStreetMapView(context: Context) : WebView(context) {
    private var ready = false
    private var selection: JSONObject? = null

    init {
        settings.javaScriptEnabled = true
        settings.allowFileAccess = false
        settings.allowContentAccess = false
        settings.mixedContentMode = WebSettings.MIXED_CONTENT_NEVER_ALLOW
        settings.cacheMode = WebSettings.LOAD_DEFAULT
        settings.userAgentString = "${settings.userAgentString} SmartMicrogrid/1.0"
        webViewClient = object : WebViewClient() {
            override fun shouldInterceptRequest(view: WebView, request: WebResourceRequest): WebResourceResponse? {
                if (request.url.host != "appassets.androidplatform.net") return null
                val file = request.url.path?.removePrefix("/map/") ?: ""
                val mime = when (file) {
                    "index.html" -> "text/html"
                    "map.js", "leaflet/leaflet.js" -> "application/javascript"
                    "leaflet/leaflet.css" -> "text/css"
                    "leaflet/images/marker-icon.png", "leaflet/images/marker-icon-2x.png",
                    "leaflet/images/marker-shadow.png" -> "image/png"
                    else -> return WebResourceResponse("text/plain", "UTF-8", 404, "Not Found", emptyMap(), ByteArrayInputStream(byteArrayOf()))
                }
                return WebResourceResponse(mime, "UTF-8", context.assets.open("map/$file"))
            }

            override fun onPageFinished(view: WebView, url: String) {
                if (url != PAGE_URL) return
                ready = true
                renderSelection()
            }

            override fun shouldOverrideUrlLoading(view: WebView, request: WebResourceRequest): Boolean {
                if (request.url.toString() == PAGE_URL) return false
                if (request.url.scheme == "https" && request.hasGesture()) {
                    runCatching { context.startActivity(Intent(Intent.ACTION_VIEW, request.url)) }
                }
                return true
            }
        }
        // Let map gestures work inside the dashboard's vertical ScrollView.
        setOnTouchListener { view, event ->
            view.parent?.requestDisallowInterceptTouchEvent(event.actionMasked != MotionEvent.ACTION_UP && event.actionMasked != MotionEvent.ACTION_CANCEL)
            false
        }
        loadUrl(PAGE_URL)
    }

    fun showLocation(latitude: Double, longitude: Double, name: String) {
        selection = JSONObject().put("latitude", latitude).put("longitude", longitude).put("name", name)
        renderSelection()
    }

    fun clearLocation() { selection = null; renderSelection() }

    private fun renderSelection() {
        if (ready) evaluateJavascript("window.showMicrogrid(${selection ?: "null"});", null)
    }

    companion object {
        private const val PAGE_URL = "https://appassets.androidplatform.net/map/index.html"
    }
}
