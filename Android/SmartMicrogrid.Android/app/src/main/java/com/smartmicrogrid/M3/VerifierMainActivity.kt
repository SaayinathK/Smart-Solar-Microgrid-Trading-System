package com.smartmicrogrid.M3

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.smartmicrogrid.R
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.ui.PlaceholderFragment
import com.smartmicrogrid.utils.SessionManager

class VerifierMainActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        SessionManager.init(this)
        if (!SessionManager.isLoggedIn() || SessionManager.getUserRole() != "TransactionVerifier") {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
            return
        }

        setContentView(R.layout.activity_verifier_main)
        supportActionBar?.title = "Verification Portal"
        supportActionBar?.elevation = 0f

        val bottomNav = findViewById<BottomNavigationView>(R.id.bottom_navigation)
        
        bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.nav_home -> loadFragment(com.smartmicrogrid.ui.HomeFragment())
                R.id.nav_scan -> loadFragment(PlaceholderFragment.newInstance("Scan QR Code"))
                R.id.nav_pending -> loadFragment(PlaceholderFragment.newInstance("Pending Tasks"))
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
