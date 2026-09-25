package com.smartmicrogrid.M2

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.utils.SessionManager

class ReservationsActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_m2_available_slots) // Uses standard container layout or frame
        
        val role = SessionManager.getUserRole()
        val title = when (role) {
            "Admin" -> "All Energy Reservations"
            "MicrogridOperator", "GridOperator" -> "Hub Energy Reservations"
            "TransactionVerifier" -> "Approved Reservations"
            else -> "My Energy Reservations"
        }

        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.title = title

        if (savedInstanceState == null) {
            supportFragmentManager.beginTransaction()
                .replace(android.R.id.content, MyReservationsFragment())
                .commit()
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
