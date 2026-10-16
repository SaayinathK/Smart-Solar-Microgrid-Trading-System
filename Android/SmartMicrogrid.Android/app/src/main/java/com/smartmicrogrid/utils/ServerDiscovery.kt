package com.smartmicrogrid.utils

import android.content.Context
import android.net.wifi.WifiManager
import android.os.Build
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.net.InetAddress
import java.net.NetworkInterface
import java.util.concurrent.TimeUnit

/**
 * ============================================================================
 * ServerDiscovery
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Automatically discovers, probes, and configures the active backend
 *          API address across changing Wi-Fi networks, emulators, and IIS/Kestrel.
 * ============================================================================
 */
object ServerDiscovery {

    private val probeClient = OkHttpClient.Builder()
        .connectTimeout(1200, TimeUnit.MILLISECONDS)
        .readTimeout(1200, TimeUnit.MILLISECONDS)
        .build()

    fun isEmulator(): Boolean {
        return (Build.BRAND.startsWith("generic") && Build.DEVICE.startsWith("generic"))
                || Build.FINGERPRINT.startsWith("generic")
                || Build.FINGERPRINT.startsWith("unknown")
                || Build.HARDWARE.contains("goldfish")
                || Build.HARDWARE.contains("ranchu")
                || Build.MODEL.contains("google_sdk")
                || Build.MODEL.contains("Emulator")
                || Build.MODEL.contains("Android SDK built for x86")
                || Build.MANUFACTURER.contains("Genymotion")
                || Build.PRODUCT.contains("sdk_google")
                || Build.PRODUCT.contains("google_sdk")
                || Build.PRODUCT.contains("sdk")
                || Build.PRODUCT.contains("sdk_x86")
                || Build.PRODUCT.contains("vbox86p")
                || Build.PRODUCT.contains("emulator")
                || Build.PRODUCT.contains("simulator")
    }

    /**
     * Resolves the device's current IPv4 address on the active network.
     */
    fun getDeviceIp(): String? {
        try {
            val interfaces = NetworkInterface.getNetworkInterfaces()
            while (interfaces.hasMoreElements()) {
                val iface = interfaces.nextElement()
                if (iface.isLoopback || !iface.isUp) continue
                val addresses = iface.inetAddresses
                while (addresses.hasMoreElements()) {
                    val addr = addresses.nextElement()
                    if (!addr.isLoopbackAddress && addr is java.net.Inet4Address) {
                        return addr.hostAddress
                    }
                }
            }
        } catch (_: Exception) {}
        return null
    }

    /**
     * Gathers a prioritized list of candidate backend URLs to test.
     */
    fun getCandidateUrls(context: Context?): List<String> {
        val candidates = linkedSetOf<String>()

        // 1. Currently configured / saved URL
        val saved = SessionManager.getBaseUrl()
        if (saved.isNotBlank()) {
            candidates.add(formatBaseUrl(saved))
        }

        // 2. Android emulator host loopback
        if (isEmulator()) {
            candidates.add("http://10.0.2.2:5050/api/")
            candidates.add("http://10.0.2.2:80/api/")
            candidates.add("http://10.0.2.2:5000/api/")
        }

        // 3. Known default laptop Wi-Fi IP
        candidates.add("http://192.168.1.6:5050/api/")
        candidates.add("http://192.168.1.6:80/api/")

        // 4. Generate candidates based on current device subnet if available
        val deviceIp = getDeviceIp()
        if (deviceIp != null && deviceIp.contains(".")) {
            val parts = deviceIp.split(".")
            if (parts.size == 4) {
                val subnetPrefix = "${parts[0]}.${parts[1]}.${parts[2]}"
                // Common laptop / gateway addresses on same subnet
                candidates.add("http://$subnetPrefix.6:5050/api/")
                candidates.add("http://$subnetPrefix.1:5050/api/")
                candidates.add("http://$subnetPrefix.2:5050/api/")
                candidates.add("http://$subnetPrefix.100:5050/api/")
                candidates.add("http://$subnetPrefix.6:80/api/")
            }
        }

        // 5. Localhost fallback
        candidates.add("http://127.0.0.1:5050/api/")

        return candidates.toList()
    }

    /**
     * Probes an individual URL by requesting its microgrids endpoint or health.
     * Returns true if the server responds with a valid HTTP status.
     */
    suspend fun probeUrl(url: String): Boolean = withContext(Dispatchers.IO) {
        val formatted = formatBaseUrl(url)
        val probeEndpoint = formatted + "microgrids"
        try {
            val request = Request.Builder()
                .url(probeEndpoint)
                .get()
                .build()
            probeClient.newCall(request).execute().use { response ->
                // Status 200, 401, or 404 indicates an active HTTP server responded
                response.code in 200..404
            }
        } catch (_: Exception) {
            false
        }
    }

    /**
     * Checks if the currently configured server is actively reachable.
     */
    suspend fun checkCurrentConnection(): Boolean {
        return probeUrl(SessionManager.getBaseUrl())
    }

    /**
     * Probes candidate addresses concurrently and returns the first responding URL.
     */
    suspend fun autoDiscoverServer(context: Context?): String? = withContext(Dispatchers.IO) {
        val candidates = getCandidateUrls(context)

        // Try saved first
        val saved = SessionManager.getBaseUrl()
        if (probeUrl(saved)) {
            return@withContext saved
        }

        // Probe remaining candidates in parallel with fast timeouts
        val remaining = candidates.filter { formatBaseUrl(it) != formatBaseUrl(saved) }
        val deferredProbes = remaining.map { url ->
            async {
                if (probeUrl(url)) url else null
            }
        }

        val results = deferredProbes.awaitAll()
        results.firstOrNull { it != null }
    }

    fun formatBaseUrl(raw: String): String {
        var clean = raw.trim()
        if (!clean.startsWith("http://") && !clean.startsWith("https://")) {
            clean = "http://$clean"
        }
        if (!clean.endsWith("/api/")) {
            clean = clean.removeSuffix("/")
            if (!clean.endsWith("/api")) {
                clean += "/api/"
            } else {
                clean += "/"
            }
        }
        return clean
    }

    fun cleanDisplayUrl(url: String): String {
        return url.removePrefix("http://")
            .removePrefix("https://")
            .removeSuffix("/api/")
            .removeSuffix("/api")
            .removeSuffix("/")
    }
}
