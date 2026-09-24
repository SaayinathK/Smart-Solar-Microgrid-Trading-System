package com.smartmicrogrid.M2

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.smartmicrogrid.R
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.ui.PlaceholderFragment
import com.smartmicrogrid.utils.SessionManager

class ProsumerMainActivity : AppCompatActivity() {

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

        val bottomNav = findViewById<BottomNavigationView>(R.id.bottom_navigation)
        
        bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.nav_home -> loadFragment(com.smartmicrogrid.ui.HomeFragment())
                R.id.nav_search -> loadFragment(com.smartmicrogrid.M1.microgrid.SearchEnergyFragment())
                R.id.nav_reservations -> loadFragment(PlaceholderFragment.newInstance("My Reservations"))
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
