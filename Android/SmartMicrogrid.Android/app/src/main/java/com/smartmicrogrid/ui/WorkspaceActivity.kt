package com.smartmicrogrid.ui

import android.os.Bundle
import android.util.TypedValue
import android.view.View
import android.view.ViewGroup
import android.widget.FrameLayout
import android.widget.TextView
import androidx.annotation.LayoutRes
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import androidx.fragment.app.FragmentManager
import com.google.android.material.appbar.MaterialToolbar
import com.smartmicrogrid.R
import com.smartmicrogrid.utils.ServerDiscovery
import com.smartmicrogrid.utils.SessionManager

/** Shared page header and content shell, with the original home presentation. */
abstract class WorkspaceActivity : AppCompatActivity() {
    private lateinit var workspace: View

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Includes restored tabs and navigation initiated inside a fragment.
        supportFragmentManager.registerFragmentLifecycleCallbacks(object : FragmentManager.FragmentLifecycleCallbacks() {
            override fun onFragmentStarted(fm: FragmentManager, fragment: Fragment) {
                if (fragment.parentFragment == null && fragment.id == R.id.fragment_container) {
                    updateHeader(fragment.javaClass.simpleName)
                }
            }
        }, false)
    }

    override fun setContentView(@LayoutRes layoutResID: Int) {
        val shell = layoutInflater.inflate(R.layout.workspace_shell, null)
        val container = shell.findViewById<FrameLayout>(R.id.workspace_content)
        attachWorkspace(shell, layoutInflater.inflate(layoutResID, container, false))
    }

    override fun setContentView(view: View) {
        attachWorkspace(layoutInflater.inflate(R.layout.workspace_shell, null), view)
    }

    override fun setContentView(view: View, params: ViewGroup.LayoutParams) {
        setContentView(view)
    }

    private fun attachWorkspace(shell: View, content: View) {
        workspace = shell
        shell.findViewById<FrameLayout>(R.id.workspace_content).addView(
            content, FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
        )
        shell.findViewById<View>(R.id.workspace_back).apply {
            setOnClickListener { onBackPressedDispatcher.onBackPressed() }
        }
        shell.findViewById<View>(R.id.workspace_status_pill).setOnClickListener {
            val user = SessionManager.getUser()
            val endpoint = ServerDiscovery.cleanDisplayUrl(SessionManager.getBaseUrl())
            val info = if (user != null) {
                "${user.firstName} ${user.lastName}\nRole: ${SessionManager.getUserRole()}\nServer: $endpoint"
            } else {
                "SmartMicrogrid System Active\nConnected to: $endpoint"
            }
            android.widget.Toast.makeText(this, info, android.widget.Toast.LENGTH_SHORT).show()
        }

        // Use a single navigation header for detail screens as well as tabs.
        content.findViewById<MaterialToolbar>(R.id.toolbar)?.visibility = View.GONE
        super.setContentView(shell)
        updateHeader(javaClass.simpleName)
    }

    private fun updateHeader(screen: String) {
        if (!::workspace.isInitialized) return
        val home = screen in setOf("MainActivity", "ProsumerMainActivity", "MicrogridOperatorMainActivity", "AdminMainActivity", "HomeFragment", "AdminDashboardFragment", "OperatorDashboardFragment", "ProsumerHomeFragment")
        val rootScreen = javaClass.simpleName in setOf("MainActivity", "ProsumerMainActivity", "MicrogridOperatorMainActivity", "AdminMainActivity")
        val titleRes = when (screen) {
            "ProsumerExploreFragment" -> R.string.header_prosumer_explore
            "ProsumerBookingsFragment" -> R.string.header_reservations
            "ProsumerTransfersFragment" -> R.string.my_transactions
            "OperatorInfrastructureFragment" -> R.string.header_microgrid_operations
            "OperatorReservationsFragment" -> R.string.header_reservations
            "OperatorTransactionsFragment" -> R.string.header_operator_transfers
            "OperatorProfileActivity" -> R.string.header_profile
            "SearchEnergyFragment" -> R.string.header_nearby_microgrids
            "MicrogridOperatorMicrogridFragment" -> R.string.header_microgrid_operations
            "MyReservationsFragment", "ReservationsActivity" -> R.string.header_reservations
            "MyTransactionsFragment" -> R.string.my_transactions
            "PendingTransactionsFragment" -> R.string.pending_transactions
            "TransactionHistoryFragment" -> R.string.transaction_history
            "ApprovedReservationsFragment" -> R.string.approved_reservations
            "ProfileFragment" -> R.string.header_profile
            "AdminUsersFragment" -> R.string.workspace_users_title
            "AdminHealthFragment" -> R.string.workspace_health_title
            "AdminSettingsFragment" -> R.string.workspace_settings_title
            "MicrogridListActivity" -> R.string.header_microgrids
            "MicrogridDetailsActivity" -> R.string.solar_microgrid_details
            "EnergyAvailabilityActivity", "AvailableSlotsActivity" -> R.string.header_available_energy
            "EnergySlotActivity" -> R.string.header_energy_slots
            "EditProfileActivity" -> R.string.header_edit_profile
            "ChangePasswordActivity" -> R.string.header_change_password
            "TransactionDetailsActivity", "ProsumerTransactionDetailsActivity" -> R.string.transaction_details
            "EnergyTransferConfirmationActivity" -> R.string.energy_transfer_confirmation_title
            "TransactionCompletionActivity" -> R.string.transaction_completed
            "TransactionQrActivity" -> R.string.transaction_qr
            else -> R.string.workspace_brand
        }
        val role = when (SessionManager.getUserRole()) {
            "MicrogridOperator" -> "Operator"
            "Prosumer" -> "Prosumer"
            "Admin" -> "Admin"
            else -> ""
        }
        val user = SessionManager.getUser()
        workspace.findViewById<TextView>(R.id.workspace_title).apply {
            setText(if (home) R.string.workspace_brand else titleRes)
            setTextSize(TypedValue.COMPLEX_UNIT_SP, if (home) 17f else 18f)
        }
        workspace.findViewById<TextView>(R.id.workspace_subtitle).apply {
            text = if (home) {
                if (user != null) "${user.firstName} • ${role.uppercase()}" else "SOLAR ENERGY PLATFORM"
            } else {
                listOf(getString(R.string.workspace_brand), role).filter { it.isNotBlank() }.joinToString(" · ")
            }
            setTextSize(TypedValue.COMPLEX_UNIT_SP, if (home) 9.5f else 11f)
            letterSpacing = if (home) 0.08f else 0f
        }
        workspace.findViewById<View>(R.id.workspace_status_pill).visibility = if (home) View.VISIBLE else View.GONE
        workspace.findViewById<TextView>(R.id.workspace_status_label).text = role.uppercase().ifBlank { "ONLINE" }
        workspace.findViewById<View>(R.id.workspace_back).apply {
            visibility = if (rootScreen) View.GONE else View.VISIBLE
            layoutParams = layoutParams.apply {
                width = dp(if (home) 38 else 48)
                height = dp(if (home) 38 else 48)
            }
        }
        workspace.findViewById<View>(R.id.workspace_logo).apply {
            layoutParams = layoutParams.apply {
                width = dp(if (home) 36 else 30)
                height = dp(if (home) 36 else 30)
            }
        }
        workspace.findViewById<View>(R.id.workspace_topbar).apply {
            minimumHeight = dp(if (home) 60 else 56)
            setPaddingRelative(dp(12), dp(if (home) 6 else 4), dp(16), dp(if (home) 6 else 4))
            elevation = dp(if (home) 4 else 1).toFloat()
        }
    }

    private fun dp(value: Int): Int = (value * resources.displayMetrics.density).toInt()
}
