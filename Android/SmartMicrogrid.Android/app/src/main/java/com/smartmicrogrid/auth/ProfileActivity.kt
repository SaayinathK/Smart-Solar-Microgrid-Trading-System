package com.smartmicrogrid.auth

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.databinding.ActivityProfileBinding
import com.smartmicrogrid.models.User
import com.smartmicrogrid.utils.SessionManager

class ProfileActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProfileBinding
    private val viewModel: AuthViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProfileBinding.inflate(layoutInflater)
        setContentView(binding.root)
        supportActionBar?.title = "My Profile"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        setupListeners()
        observeViewModel()
    }

    override fun onResume() {
        super.onResume()
        // Reload profile from server each time screen becomes visible
        viewModel.loadProfile()
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }

    private fun setupListeners() {
        binding.btnEditProfile.setOnClickListener {
            startActivity(Intent(this, EditProfileActivity::class.java))
        }

        binding.btnChangePassword.setOnClickListener {
            startActivity(Intent(this, ChangePasswordActivity::class.java))
        }

        binding.btnLogout.setOnClickListener {
            viewModel.logout()
            // Navigate back to Login and clear task stack
            val intent = Intent(this, LoginActivity::class.java)
            intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            startActivity(intent)
            finish()
        }
    }

    private fun observeViewModel() {
        viewModel.isLoading.observe(this) { loading ->
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        }

        viewModel.profileResult.observe(this) { result ->
            result?.let {
                if (it.isSuccess) {
                    populateUI(it.getOrNull()!!)
                } else {
                    Toast.makeText(this, it.exceptionOrNull()?.message ?: "Failed to load profile", Toast.LENGTH_SHORT).show()
                    // Fallback to local session data if network fails
                    SessionManager.getUser()?.let { user -> populateUI(user) }
                }
            }
        }
    }

    private fun populateUI(user: User) {
        binding.tvName.text = "${user.firstName} ${user.lastName}"
        binding.tvInitials.text = user.firstName.take(1).uppercase()
        binding.tvRole.text = user.role
        binding.tvEmail.text = user.email
        binding.tvPhone.text = user.phoneNumber
        
        if (user.isActive) {
            binding.tvStatus.text = "Active"
            binding.tvStatus.setTextColor(android.graphics.Color.parseColor("#10b981")) // emerald
        } else {
            binding.tvStatus.text = "Inactive"
            binding.tvStatus.setTextColor(android.graphics.Color.parseColor("#ef4444")) // red
        }
    }
}
