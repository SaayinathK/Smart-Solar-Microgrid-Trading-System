package com.smartmicrogrid

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.M1.availability.EnergyAvailabilityActivity
import com.smartmicrogrid.M1.microgrid.MicrogridListActivity
import com.smartmicrogrid.M1.slots.EnergySlotActivity
import com.smartmicrogrid.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        com.smartmicrogrid.utils.SessionManager.init(this)
        if (!com.smartmicrogrid.utils.SessionManager.isLoggedIn()) {
            startActivity(Intent(this, com.smartmicrogrid.auth.LoginActivity::class.java))
            finish()
            return
        }

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = "Smart Microgrid Portal"
        binding.tvWelcome.text = "Welcome, ${com.smartmicrogrid.utils.SessionManager.getUserName()}!"

        binding.btnMicrogrids.setOnClickListener {
            startActivity(Intent(this, MicrogridListActivity::class.java))
        }

        binding.btnEnergyAvailability.setOnClickListener {
            startActivity(Intent(this, EnergyAvailabilityActivity::class.java))
        }

        binding.btnEnergySlots.setOnClickListener {
            startActivity(Intent(this, EnergySlotActivity::class.java))
        }

        binding.btnProfile.setOnClickListener {
            startActivity(Intent(this, com.smartmicrogrid.auth.ProfileActivity::class.java))
        }
    }
}
