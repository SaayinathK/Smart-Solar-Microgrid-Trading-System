package com.smartmicrogrid.M4

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ProgressBar
import android.widget.TextView
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import com.google.android.material.snackbar.Snackbar
import com.smartmicrogrid.R
import java.util.Locale

/** Detailed service diagnostics: database, authentication, server and process. */
class AdminHealthFragment : Fragment() {

    private val viewModel: AdminViewModel by viewModels()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View = inflater.inflate(R.layout.fragment_admin_health, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        view.findViewById<View>(R.id.m4_health_refresh).setOnClickListener {
            viewModel.loadSystemHealth()
        }

        viewModel.health.observe(viewLifecycleOwner) { health ->
            if (health != null) render(view, health.api, health.database, health.databaseDetail,
                health.authentication, health.server, health.environment, health.version,
                health.databaseLatencyMs, health.processMemoryBytes, health.uptime)
        }

        viewModel.isLoading.observe(viewLifecycleOwner) { loading ->
            view.findViewById<ProgressBar>(R.id.m4_health_loading).visibility =
                if (loading) View.VISIBLE else View.GONE
        }

        viewModel.errorMessage.observe(viewLifecycleOwner) { message ->
            if (message != null && view.isAttachedToWindow) {
                Snackbar.make(view, message, Snackbar.LENGTH_LONG).show()
            }
        }

        viewModel.loadSystemHealth()
    }

    @Suppress("LongParameterList")
    private fun render(
        view: View,
        api: String,
        database: String,
        databaseDetail: String,
        authentication: String,
        server: String,
        environment: String,
        version: String,
        latencyMs: Double,
        memoryBytes: Long,
        uptime: String
    ) {
        view.findViewById<TextView>(R.id.m4_health_api).text =
            "${getString(R.string.m4_health_api)}: $api"
        view.findViewById<TextView>(R.id.m4_health_database).text =
            "${getString(R.string.m4_health_database)}: $database"
        view.findViewById<TextView>(R.id.m4_health_auth).text =
            "${getString(R.string.m4_health_auth)}: $authentication"
        view.findViewById<TextView>(R.id.m4_health_server).text =
            "${getString(R.string.m4_health_server)}: $server"

        view.findViewById<TextView>(R.id.m4_health_database_detail).apply {
            text = databaseDetail
            visibility = if (databaseDetail.isBlank()) View.GONE else View.VISIBLE
        }

        view.findViewById<TextView>(R.id.m4_health_environment).text =
            "${getString(R.string.m4_health_environment)}: $environment"
        view.findViewById<TextView>(R.id.m4_health_version).text =
            "${getString(R.string.m4_health_version)}: $version"
        view.findViewById<TextView>(R.id.m4_health_latency).text =
            String.format(
                Locale.US, "%s: %.1f ms",
                getString(R.string.m4_health_latency), latencyMs
            )
        view.findViewById<TextView>(R.id.m4_health_memory).text =
            "${getString(R.string.m4_health_memory)}: ${formatBytes(memoryBytes)}"
        view.findViewById<TextView>(R.id.m4_health_uptime).text =
            "${getString(R.string.m4_health_uptime)}: $uptime"
    }

    private fun formatBytes(bytes: Long): String = when {
        bytes <= 0 -> "-"
        bytes >= 1024L * 1024 * 1024 -> String.format(Locale.US, "%.2f GB", bytes / (1024.0 * 1024 * 1024))
        bytes >= 1024L * 1024 -> String.format(Locale.US, "%.1f MB", bytes / (1024.0 * 1024))
        else -> String.format(Locale.US, "%d KB", bytes / 1024)
    }
}
