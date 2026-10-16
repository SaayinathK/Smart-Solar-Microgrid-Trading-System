package com.smartmicrogrid.M2

import android.os.Bundle
import com.smartmicrogrid.ui.WorkspaceActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.utils.SessionManager

class ReservationsActivity : WorkspaceActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (com.smartmicrogrid.utils.SessionManager.getUserRole() == "Prosumer") {
            startActivity(android.content.Intent(this, com.smartmicrogrid.M2.ProsumerMainActivity::class.java).apply {
                flags = android.content.Intent.FLAG_ACTIVITY_CLEAR_TOP or android.content.Intent.FLAG_ACTIVITY_SINGLE_TOP
                putExtra("prosumer_destination", "bookings")
                putExtra("MICROGRID_ID", intent.getStringExtra("MICROGRID_ID"))
            })
            finish()
            return
        }
        setContentView(android.widget.FrameLayout(this).apply { id = R.id.fragment_container })
        
        val role = SessionManager.getUserRole()
        val title = when (role) {
            "Admin" -> "All Energy Reservations"
            "MicrogridOperator", "GridOperator" -> "Hub Energy Reservations & Transfer Verification"
            else -> "My Energy Reservations"
        }

        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.title = title

        if (savedInstanceState == null) {
            supportFragmentManager.beginTransaction()
                .replace(R.id.fragment_container, MyReservationsFragment())
                .commit()
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
