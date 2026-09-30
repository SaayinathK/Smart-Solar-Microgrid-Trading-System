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
import com.smartmicrogrid.models.AdminDashboard
import java.util.Locale

/**
 * Read-only overview of the whole platform. M1/M2/M3 numbers come from the
 * dashboard endpoint, which flags each component as Live or Unavailable so a
 * single broken component degrades one card instead of the screen.
 */
class AdminDashboardFragment : Fragment() {

    private val viewModel: AdminViewModel by viewModels()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View = inflater.inflate(R.layout.fragment_admin_dashboard, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        view.findViewById<View>(R.id.m4_refresh).setOnClickListener {
            viewModel.loadDashboard()
        }

        viewModel.dashboard.observe(viewLifecycleOwner) { dashboard ->
            if (dashboard != null) render(dashboard)
        }

        viewModel.isLoading.observe(viewLifecycleOwner) { loading ->
            view.findViewById<ProgressBar>(R.id.m4_loading).visibility =
                if (loading) View.VISIBLE else View.GONE
        }

        viewModel.errorMessage.observe(viewLifecycleOwner) { message ->
            if (message != null && view.isAttachedToWindow) {
                Snackbar.make(view, message, Snackbar.LENGTH_LONG).show()
            }
        }

        viewModel.loadDashboard()
    }

    private fun render(dashboard: AdminDashboard) {
        val view = requireView()

        // Maintenance mode is the single most important signal on this screen.
        val banner = view.findViewById<TextView>(R.id.m4_maintenance_banner)
        if (dashboard.maintenanceMode) {
            val message = dashboard.activity.recentActivity
                .firstOrNull { it.module == "System Configuration" }
                ?.description
                .orEmpty()
            banner.text = getString(R.string.m4_maintenance_banner, message)
            banner.visibility = View.VISIBLE
        } else {
            banner.visibility = View.GONE
        }

        view.bindStat(
            R.id.m4_stat_users,
            primary = dashboard.users.totalUsers.toString(),
            primaryLabel = getString(R.string.m4_stat_total_users),
            secondary = dashboard.users.activeUsers.toString(),
            secondaryLabel = getString(R.string.m4_stat_active_users),
            note = buildString {
                append(getString(R.string.m4_stat_suspended_users)).append(": ")
                append(dashboard.users.suspendedUsers)
                append("  ·  ")
                append(getString(R.string.m4_stat_pending_users)).append(": ")
                append(dashboard.users.pendingUsers)
            }
        )

        val platform = dashboard.platform
        view.bindStat(
            R.id.m4_stat_microgrids,
            primary = platform.microgridCount.toString(),
            primaryLabel = getString(R.string.m4_stat_microgrids),
            secondary = String.format(
                Locale.US, "%.1f / %.1f kWh",
                platform.totalAvailableCapacity, platform.totalCapacity
            ),
            secondaryLabel = "Available capacity",
            note = if (platform.microgrids.isLive) null
            else getString(R.string.m4_source_unavailable)
        )

        view.bindStat(
            R.id.m4_stat_reservations,
            primary = platform.reservationCount.toString(),
            primaryLabel = getString(R.string.m4_stat_reservations),
            secondary = platform.pendingReservationCount.toString(),
            secondaryLabel = getString(R.string.m4_stat_pending_reservations),
            note = if (platform.reservations.isLive) null
            else getString(R.string.m4_source_unavailable)
        )

        view.bindStat(
            R.id.m4_stat_transactions,
            primary = platform.transactionCount.toString(),
            primaryLabel = getString(R.string.m4_stat_transactions),
            secondary = platform.completedTransactionCount.toString(),
            secondaryLabel = getString(R.string.m4_stat_completed),
            note = if (platform.transactions.isLive) null
            else getString(R.string.m4_source_unavailable)
        )

        val health = dashboard.health
        val healthCard = view.findViewById<View>(R.id.m4_health_summary)
        healthCard.findViewById<TextView>(R.id.m4_health_api).text =
            "${getString(R.string.m4_health_api)}: ${health.api}"
        healthCard.findViewById<TextView>(R.id.m4_health_database).text =
            "${getString(R.string.m4_health_database)}: ${health.database}"
        healthCard.findViewById<TextView>(R.id.m4_health_auth).text =
            "${getString(R.string.m4_health_auth)}: ${health.authentication}"
        healthCard.findViewById<TextView>(R.id.m4_health_server).text =
            "${getString(R.string.m4_health_server)}: ${health.server}"
    }

    /**
     * Fills one of the included stat cards. The include's android:id overrides the
     * root id of [R.layout.item_m4_stat], so every card's children live under its
     * own view scope.
     */
    private fun View.bindStat(
        @androidx.annotation.IdRes cardId: Int,
        primary: String,
        primaryLabel: String,
        secondary: String,
        secondaryLabel: String,
        note: String?
    ) {
        val card = findViewById<View>(cardId)
        card.findViewById<TextView>(R.id.m4_stat_primary_value).text = primary
        card.findViewById<TextView>(R.id.m4_stat_primary_label).text = primaryLabel
        card.findViewById<TextView>(R.id.m4_stat_secondary_value).text = secondary
        card.findViewById<TextView>(R.id.m4_stat_secondary_label).text = secondaryLabel

        val noteView = card.findViewById<TextView>(R.id.m4_stat_note)
        if (note.isNullOrBlank()) {
            noteView.visibility = View.GONE
        } else {
            noteView.text = note
            noteView.visibility = View.VISIBLE
        }
    }
}
