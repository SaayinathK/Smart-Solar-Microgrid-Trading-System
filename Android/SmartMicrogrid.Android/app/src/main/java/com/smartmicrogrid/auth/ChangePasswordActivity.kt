package com.smartmicrogrid.auth

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.databinding.ActivityChangePasswordBinding
import com.smartmicrogrid.models.ChangePasswordRequest

class ChangePasswordActivity : AppCompatActivity() {

    private lateinit var binding: ActivityChangePasswordBinding
    private val viewModel: AuthViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityChangePasswordBinding.inflate(layoutInflater)
        setContentView(binding.root)
        supportActionBar?.title = "Change Password"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        setupListeners()
        observeViewModel()
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }

    private fun setupListeners() {
        binding.btnChangePassword.setOnClickListener {
            val currentPwd = binding.etCurrentPassword.text.toString()
            val newPwd = binding.etNewPassword.text.toString()
            val confirmPwd = binding.etConfirmPassword.text.toString()

            if (currentPwd.isEmpty() || newPwd.isEmpty() || confirmPwd.isEmpty()) {
                showError("All fields are required.")
                return@setOnClickListener
            }
            if (newPwd.length < 6) {
                showError("New password must be at least 6 characters.")
                return@setOnClickListener
            }
            if (newPwd != confirmPwd) {
                showError("New passwords do not match.")
                return@setOnClickListener
            }

            hideError()
            viewModel.changePassword(ChangePasswordRequest(currentPwd, newPwd, confirmPwd))
        }
    }

    private fun observeViewModel() {
        viewModel.isLoading.observe(this) { loading ->
            binding.btnChangePassword.isEnabled = !loading
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
            binding.btnChangePassword.text = if (loading) "Updating..." else "Update Password"
        }

        viewModel.passwordResult.observe(this) { result ->
            result?.let {
                if (it.isSuccess) {
                    Toast.makeText(this, "Password updated successfully", Toast.LENGTH_SHORT).show()
                    finish()
                } else {
                    showError(it.exceptionOrNull()?.message ?: "Failed to change password.")
                }
            }
        }
    }

    private fun showError(message: String) {
        binding.tvError.text = message
        binding.tvError.visibility = View.VISIBLE
    }

    private fun hideError() {
        binding.tvError.visibility = View.GONE
    }
}
