package com.smartmicrogrid.M2

import android.content.Intent
import android.os.Bundle
import com.smartmicrogrid.ui.WorkspaceActivity
import androidx.fragment.app.Fragment
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.smartmicrogrid.R
import com.smartmicrogrid.ui.NavigationMotion
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.utils.SessionManager

class ProsumerMainActivity : WorkspaceActivity() {
    private lateinit var bottomNav: BottomNavigationView
    private var exploreMode = 0
    private var exploreGrid: String? = null
    private var bookingsTab = 0
    private var bookingQuery: String? = null
    private var transfersTab = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        SessionManager.init(this)
        if (!SessionManager.isLoggedIn() || SessionManager.getUserRole() != "Prosumer") {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
            return
        }

        setContentView(R.layout.activity_prosumer_main)
        supportActionBar?.title = "Energy Trading"
        supportActionBar?.elevation = 0f

        bottomNav = findViewById(R.id.bottom_navigation)
        
        bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.nav_home -> loadFragment(com.smartmicrogrid.prosumer.ProsumerHomeFragment())
                R.id.nav_search -> loadFragment(exploreFragment())
                R.id.nav_reservations -> loadFragment(bookingsFragment())
                R.id.nav_transactions -> loadFragment(transfersFragment())
                R.id.nav_profile -> loadFragment(com.smartmicrogrid.ui.ProfileFragment())
                else -> false
            }
        }

        // Load default fragment
        if (savedInstanceState == null) {
            if (!route(intent)) bottomNav.selectedItemId = R.id.nav_home
        }
        NavigationMotion.configure(bottomNav)
    }

    override fun onNewIntent(intent: Intent) { super.onNewIntent(intent); setIntent(intent); route(intent) }
    private fun route(intent: Intent): Boolean = when (intent.getStringExtra("prosumer_destination")) {
        "explore" -> { openExplore(intent.getIntExtra("mode", 0), intent.getStringExtra("MICROGRID_ID")); true }
        "bookings" -> { openBookings(); true }
        else -> false
    }
    private fun exploreFragment() = com.smartmicrogrid.prosumer.ProsumerExploreFragment().apply { arguments = Bundle().apply { putInt("mode", exploreMode); putString("grid", exploreGrid) } }
    private fun bookingsFragment() = com.smartmicrogrid.prosumer.ProsumerBookingsFragment().apply { arguments = Bundle().apply { putInt("tab", bookingsTab); putString("query", bookingQuery) } }
    private fun transfersFragment() = com.smartmicrogrid.prosumer.ProsumerTransfersFragment().apply { arguments = Bundle().apply { putInt("tab", transfersTab) } }
    fun openExplore(mode: Int = 0, grid: String? = null) {
        exploreMode = mode; exploreGrid = grid
        if (bottomNav.selectedItemId == R.id.nav_search) loadFragment(exploreFragment()) else bottomNav.selectedItemId = R.id.nav_search
    }
    fun openBookings(tab: Int = 0, reference: String? = null) {
        bookingsTab = tab; bookingQuery = reference
        if (bottomNav.selectedItemId == R.id.nav_reservations) loadFragment(bookingsFragment()) else bottomNav.selectedItemId = R.id.nav_reservations
    }
    fun openTransfers(tab: Int = 0) {
        transfersTab = tab
        if (bottomNav.selectedItemId == R.id.nav_transactions) loadFragment(transfersFragment()) else bottomNav.selectedItemId = R.id.nav_transactions
    }

    private fun loadFragment(fragment: Fragment): Boolean {
        NavigationMotion.transition(supportFragmentManager.beginTransaction(), this)
            .replace(R.id.fragment_container, fragment)
            .commit()
        return true
    }
}
