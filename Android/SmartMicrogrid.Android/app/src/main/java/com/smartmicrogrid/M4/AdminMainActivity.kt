package com.smartmicrogrid.M4

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.smartmicrogrid.R
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.utils.SessionManager

/**
 * M4 entry point. The whole module is gated on the Admin role here as well as
 * on the server, because the server's [Authorize] attribute is the only real
 * boundary and the client check exists purely to avoid showing an operator a
 * screen that would immediately 403.
 */
class AdminMainActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        SessionManager.init(this)

        if (!SessionManager.isLoggedIn() || SessionManager.getUserRole() != "Admin") {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
            return
        }

        setContentView(R.layout.activity_admin_main)

        supportActionBar?.title = getString(R.string.m4_portal_title)
        supportActionBar?.elevation = 0f

        val bottomNav = findViewById<BottomNavigationView>(R.id.m4_bottom_navigation)

        bottomNav.setOnItemSelectedListener { item ->
            when (item.itemId) {
                R.id.m4_nav_dashboard -> loadFragment(AdminDashboardFragment())
                R.id.m4_nav_users -> loadFragment(AdminUsersFragment())
                R.id.m4_nav_health -> loadFragment(AdminHealthFragment())
                R.id.m4_nav_settings -> loadFragment(AdminSettingsFragment())
                else -> false
            }
        }

        if (savedInstanceState == null) {
            bottomNav.selectedItemId = R.id.m4_nav_dashboard
        }
    }

    private fun loadFragment(fragment: Fragment): Boolean {
        supportFragmentManager.beginTransaction()
            .replace(R.id.fragment_container, fragment)
            .commit()
        return true
    }
}
