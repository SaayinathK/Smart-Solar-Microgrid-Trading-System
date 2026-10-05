package com.smartmicrogrid.M3

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.withResumed
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import androidx.fragment.app.Fragment
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.smartmicrogrid.R
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.utils.SessionManager

class MicrogridOperatorMainActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Restored fragments need their container before asynchronous session restoration.
        setContentView(R.layout.activity_microgrid_operator_main)

        SessionManager.init(this)
        lifecycleScope.launch {
            try {
                SessionManager.awaitReady()
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                startActivity(Intent(this@MicrogridOperatorMainActivity, LoginActivity::class.java))
                finish()
                return@launch
            }
            lifecycle.withResumed { showPortal(savedInstanceState) }
        }
    }

    private fun showPortal(savedInstanceState: Bundle?) {
        if (!SessionManager.isLoggedIn() || SessionManager.getUserRole() != "MicrogridOperator") {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
            return
        }

        setContentView(R.layout.activity_microgrid_operator_main)

        supportActionBar?.title = "Microgrid Operator Portal"
        supportActionBar?.elevation = 0f

        val bottomNav =
            findViewById<BottomNavigationView>(R.id.bottom_navigation)

        bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {

                R.id.nav_home ->
                    loadFragment(
                        com.smartmicrogrid.ui.HomeFragment()
                    )

                R.id.nav_scan ->
                    loadFragment(
                        com.smartmicrogrid.M1.microgrid.MicrogridOperatorMicrogridFragment()
                    )

                R.id.nav_transactions ->
                    loadFragment(PendingTransactionsFragment())

                R.id.nav_pending ->
                    loadFragment(
                        com.smartmicrogrid.M2.MyReservationsFragment()
                    )

                R.id.nav_profile ->
                    loadFragment(
                        com.smartmicrogrid.ui.ProfileFragment()
                    )

                else -> false
            }
        }

        if (savedInstanceState == null) {
            if (intent.getBooleanExtra(EXTRA_OPEN_HISTORY, false)) {
                bottomNav.selectedItemId = R.id.nav_transactions
                loadFragment(TransactionHistoryFragment())
            } else {
                bottomNav.selectedItemId = R.id.nav_home
            }
        }
    }

    private fun loadFragment(fragment: Fragment): Boolean {
        supportFragmentManager.beginTransaction()
            .replace(R.id.fragment_container, fragment)
            .commit()

        return true
    }

    companion object {
        const val EXTRA_OPEN_HISTORY = "OPEN_TRANSACTION_HISTORY"
    }
}
