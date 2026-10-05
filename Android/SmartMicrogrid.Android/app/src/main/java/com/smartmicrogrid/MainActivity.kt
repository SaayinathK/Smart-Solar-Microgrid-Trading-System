package com.smartmicrogrid

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import com.smartmicrogrid.M1.availability.EnergyAvailabilityActivity
import com.smartmicrogrid.M1.microgrid.MicrogridListActivity
import com.smartmicrogrid.M1.slots.EnergySlotActivity
import com.smartmicrogrid.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        
        SessionManager.init(this)
        lifecycleScope.launch {
            try {
                SessionManager.awaitReady()
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                startActivity(Intent(this@MainActivity, com.smartmicrogrid.auth.LoginActivity::class.java))
                finish()
                return@launch
            }
            showPortal()
        }
    }

    private fun showPortal() {
        if (!SessionManager.isLoggedIn()) {
            startActivity(Intent(this, com.smartmicrogrid.auth.LoginActivity::class.java))
            finish()
            return
        }

        supportActionBar?.title = "Smart Microgrid Portal"
        binding.tvWelcome.text = "Welcome, ${SessionManager.getUserName()}!"

        binding.btnMicrogrids.setOnClickListener {
            startActivity(Intent(this, MicrogridListActivity::class.java))
        }

        binding.btnEnergyAvailability.setOnClickListener {
            startActivity(Intent(this, EnergyAvailabilityActivity::class.java))
        }

        binding.btnEnergySlots.setOnClickListener {
            startActivity(Intent(this, EnergySlotActivity::class.java))
        }

        binding.btnReservations.setOnClickListener {
            startActivity(Intent(this, com.smartmicrogrid.M2.ReservationsActivity::class.java))
        }


    }
}
