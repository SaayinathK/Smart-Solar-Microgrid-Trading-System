package com.smartmicrogrid.auth

import android.os.Bundle
import android.view.View
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.databinding.ActivityRegisterBinding
import com.smartmicrogrid.models.RegisterRequest

class RegisterActivity : AppCompatActivity() {

    private lateinit var binding: ActivityRegisterBinding
    private val viewModel: AuthViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityRegisterBinding.inflate(layoutInflater)
        setContentView(binding.root)
        supportActionBar?.title = "Prosumer Registration"

        setupListeners()
        observeViewModel()
    }

    private fun setupListeners() {
        binding.btnRegister.setOnClickListener {
            val firstName = binding.etFirstName.text.toString().trim()
            val lastName = binding.etLastName.text.toString().trim()
            val email = binding.etEmail.text.toString().trim()
            val phone = binding.etPhone.text.toString().trim()
            val nic = binding.etNic.text.toString().trim().uppercase()
            val password = binding.etPassword.text.toString()
            val confirmPassword = binding.etConfirmPassword.text.toString()

            // Validation
            if (firstName.isEmpty() || lastName.isEmpty()) {
                showError("First name and last name are required.")
                return@setOnClickListener
            }
            if (nic.isEmpty()) {
                showError("National Identity Card (NIC) is required for Prosumer registration.")
                return@setOnClickListener
            }
            if (email.isEmpty() || !android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                showError("A valid email address is required.")
                return@setOnClickListener
            }
            if (password.length < 6) {
                showError("Password must be at least 6 characters.")
                return@setOnClickListener
            }
            if (password != confirmPassword) {
                showError("Passwords do not match.")
                return@setOnClickListener
            }

            hideError()
            viewModel.register(
                RegisterRequest(
                    firstName = firstName,
                    lastName = lastName,
                    email = email,
                    phoneNumber = phone,
                    nic = nic,
                    role = "Prosumer",
                    password = password,
                    confirmPassword = confirmPassword
                )
            )
        }

        binding.tvLoginLink.setOnClickListener {
            finish() // Back to Login
        }
    }

    private fun observeViewModel() {
        viewModel.isLoading.observe(this) { loading ->
            binding.btnRegister.isEnabled = !loading
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
            binding.btnRegister.text = if (loading) "Creating..." else "Create Account"
        }

        viewModel.registerResult.observe(this) { result ->
            result?.let {
                if (it.isSuccess) {
                    binding.tvSuccess.text = "Account created successfully! You can now sign in."
                    binding.tvSuccess.visibility = View.VISIBLE
                    hideError()
                    // Delay then go back to login
                    binding.root.postDelayed({ finish() }, 1500)
                } else {
                    showError(it.exceptionOrNull()?.message ?: "Registration failed.")
                }
            }
        }
    }

    private fun showError(message: String) {
        binding.tvError.text = message
        binding.tvError.visibility = View.VISIBLE
        binding.tvSuccess.visibility = View.GONE
    }

    private fun hideError() {
        binding.tvError.visibility = View.GONE
    }
}
