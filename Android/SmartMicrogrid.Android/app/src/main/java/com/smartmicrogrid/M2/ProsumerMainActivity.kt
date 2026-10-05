package com.smartmicrogrid.M2

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

class ProsumerMainActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Restored fragments need their container before asynchronous session restoration.
        setContentView(R.layout.activity_prosumer_main)
        
        SessionManager.init(this)
        lifecycleScope.launch {
            try {
                SessionManager.awaitReady()
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                startActivity(Intent(this@ProsumerMainActivity, LoginActivity::class.java))
                finish()
                return@launch
            }
            lifecycle.withResumed { showPortal(savedInstanceState) }
        }
    }

    private fun showPortal(savedInstanceState: Bundle?) {
        if (!SessionManager.isLoggedIn() || SessionManager.getUserRole() != "Prosumer") {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
            return
        }

        supportActionBar?.title = "Energy Trading"
        supportActionBar?.elevation = 0f

        val bottomNav = findViewById<BottomNavigationView>(R.id.bottom_navigation)
        
        bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.nav_home -> loadFragment(com.smartmicrogrid.ui.HomeFragment())
                R.id.nav_search -> loadFragment(com.smartmicrogrid.M1.microgrid.SearchEnergyFragment())
                R.id.nav_reservations -> loadFragment(MyReservationsFragment())
                R.id.nav_profile -> loadFragment(com.smartmicrogrid.ui.ProfileFragment())
                else -> false
            }
        }

        // Load default fragment
        if (savedInstanceState == null) {
            bottomNav.selectedItemId = R.id.nav_home
        }
    }

    private fun loadFragment(fragment: Fragment): Boolean {
        supportFragmentManager.beginTransaction()
            .replace(R.id.fragment_container, fragment)
            .commit()
        return true
    }
}
