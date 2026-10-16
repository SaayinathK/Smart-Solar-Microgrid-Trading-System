package com.smartmicrogrid.M3

import android.content.Intent
import android.os.Bundle
import com.smartmicrogrid.ui.WorkspaceActivity
import androidx.fragment.app.Fragment
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.smartmicrogrid.R
import com.smartmicrogrid.ui.NavigationMotion
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.utils.SessionManager
import com.smartmicrogrid.operator.*
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import android.widget.EditText
import android.widget.Toast

class MicrogridOperatorMainActivity : WorkspaceActivity() {
    private lateinit var bottomNav: BottomNavigationView
    private var infrastructureMode = 0
    private var transactionTab = 0
    private val scanner = registerForActivityResult(ActivityResultContracts.StartActivityForResult()) { result ->
        if (result.resultCode == RESULT_OK) {
            result.data?.getStringExtra(QRScannerActivity.EXTRA_QR_DATA)?.let(::openQrPayload)
        } else {
            result.data?.getStringExtra(QRScannerActivity.EXTRA_ERROR)?.takeUnless { it.contains("cancelled", true) }?.let {
                Toast.makeText(this, it, Toast.LENGTH_LONG).show()
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        SessionManager.init(this)

        if (!SessionManager.isLoggedIn() ||
            SessionManager.getUserRole() != "MicrogridOperator"
        ) {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
            return
        }

        setContentView(R.layout.activity_microgrid_operator_main)

        supportActionBar?.title = "Microgrid Operator Portal"
        supportActionBar?.elevation = 0f

        bottomNav =
            findViewById<BottomNavigationView>(R.id.bottom_navigation)

        bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {

                R.id.nav_home ->
                    loadFragment(
                        OperatorDashboardFragment()
                    )

                R.id.nav_grids ->
                    loadFragment(
                        OperatorInfrastructureFragment().apply { arguments = Bundle().apply { putInt("mode", infrastructureMode) } }
                    )

                R.id.nav_scan -> { startQrScan(); false }

                R.id.nav_transactions ->
                    loadFragment(OperatorTransactionsFragment().apply { arguments = Bundle().apply { putInt("tab", transactionTab) } })

                R.id.nav_pending ->
                    loadFragment(
                        OperatorReservationsFragment()
                    )

                else -> false
            }
        }

        if (savedInstanceState == null) {
            if (intent.getBooleanExtra(EXTRA_OPEN_HISTORY, false)) {
                transactionTab = 2
                bottomNav.selectedItemId = R.id.nav_transactions
            } else {
                bottomNav.selectedItemId = R.id.nav_home
            }
        }
        NavigationMotion.configure(bottomNav)
    }

    fun openGrids(mode: Int = 0) {
        infrastructureMode = mode
        if (bottomNav.selectedItemId == R.id.nav_grids) loadFragment(OperatorInfrastructureFragment().apply { arguments = Bundle().apply { putInt("mode", mode) } })
        else bottomNav.selectedItemId = R.id.nav_grids
    }
    fun openReservations() { bottomNav.selectedItemId = R.id.nav_pending }
    fun openTransfers(tab: Int = 0) {
        transactionTab = tab
        if (bottomNav.selectedItemId == R.id.nav_transactions) loadFragment(OperatorTransactionsFragment().apply { arguments = Bundle().apply { putInt("tab", tab) } })
        else bottomNav.selectedItemId = R.id.nav_transactions
    }
    fun openProfile() { startActivity(Intent(this, OperatorProfileActivity::class.java)) }
    fun startQrScan() { scanner.launch(Intent(this, QRScannerActivity::class.java)) }
    fun pasteQr() {
        val input = EditText(this).apply { hint = "Paste Smart Microgrid transaction QR payload"; minLines = 3 }
        val dialog = AlertDialog.Builder(this).setTitle("Open transaction QR").setView(input).setNegativeButton("Cancel", null).setPositiveButton("Review transaction", null).create()
        dialog.setOnShowListener {
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                if (TransactionQrPayload.parse(input.text.toString().trim()) == null) input.error = "Enter a valid Smart Microgrid transaction payload."
                else { openQrPayload(input.text.toString().trim()); dialog.dismiss() }
            }
        }
        dialog.show()
    }
    private fun openQrPayload(payload: String) {
        val parsed = TransactionQrPayload.parse(payload) ?: return
        startActivity(Intent(this, TransactionDetailsActivity::class.java).putExtra("TRANSACTION_ID", parsed.transactionId).putExtra(QRScannerActivity.EXTRA_QR_DATA, payload))
    }

    private fun loadFragment(fragment: Fragment): Boolean {
        NavigationMotion.transition(supportFragmentManager.beginTransaction(), this)
            .replace(R.id.fragment_container, fragment)
            .commit()

        return true
    }

    companion object {
        const val EXTRA_OPEN_HISTORY = "OPEN_TRANSACTION_HISTORY"
    }
}
