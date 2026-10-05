package com.smartmicrogrid.M2

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import com.smartmicrogrid.R
import com.smartmicrogrid.utils.SessionManager

class ReservationsActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_m2_available_slots) // Uses standard container layout or frame
        
        lifecycleScope.launch {
            try {
                SessionManager.awaitReady()
                supportActionBar?.title = when (SessionManager.getUserRole()) {
                    "Admin" -> "All Energy Reservations"
                    "MicrogridOperator", "GridOperator" -> "Hub Energy Reservations & Transfer Verification"
                    else -> "My Energy Reservations"
                }
            } catch (error: Exception) {
                if (error is CancellationException) throw error
            }
        }

        supportActionBar?.setDisplayHomeAsUpEnabled(true)

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
