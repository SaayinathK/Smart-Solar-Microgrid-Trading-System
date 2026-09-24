package com.smartmicrogrid.auth

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.MainActivity
import com.smartmicrogrid.databinding.ActivityLoginBinding
import com.smartmicrogrid.utils.SessionManager

class LoginActivity : AppCompatActivity() {

    private lateinit var binding: ActivityLoginBinding
    private val viewModel: AuthViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Initialize session manager
        SessionManager.init(this)

        // If already logged in, skip to correct main
        if (SessionManager.isLoggedIn()) {
            val role = SessionManager.getUserRole()
            if (role == "Prosumer") {
                startActivity(Intent(this, com.smartmicrogrid.M2.ProsumerMainActivity::class.java))
            } else if (role == "TransactionVerifier") {
                startActivity(Intent(this, com.smartmicrogrid.M3.VerifierMainActivity::class.java))
            }
            finish()
            return
        }

        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)
        supportActionBar?.hide()

        setupListeners()
        observeViewModel()
    }

    private fun setupListeners() {
        binding.btnLogin.setOnClickListener {
            val email = binding.etEmail.text.toString().trim()
            val password = binding.etPassword.text.toString()

            // Client-side validation
            if (email.isEmpty()) {
                showError("Email address is required.")
                return@setOnClickListener
            }
            if (!android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                showError("Invalid email format.")
                return@setOnClickListener
            }
            if (password.isEmpty()) {
                showError("Password is required.")
                return@setOnClickListener
            }

            hideError()
            viewModel.login(email, password)
        }

        binding.tvRegisterLink.setOnClickListener {
            startActivity(Intent(this, RegisterActivity::class.java))
        }
    }

    private fun observeViewModel() {
        viewModel.isLoading.observe(this) { loading ->
            binding.btnLogin.isEnabled = !loading
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
            binding.btnLogin.text = if (loading) "Authenticating..." else "Sign In"
        }

        viewModel.loginResult.observe(this) { result ->
            result?.let {
                if (it.isSuccess) {
                    val role = SessionManager.getUserRole()
                    when (role) {
                        "Prosumer" -> {
                            startActivity(Intent(this, com.smartmicrogrid.M2.ProsumerMainActivity::class.java))
                            finish()
                        }
                        "TransactionVerifier" -> {
                            startActivity(Intent(this, com.smartmicrogrid.M3.VerifierMainActivity::class.java))
                            finish()
                        }
                        else -> {
                            SessionManager.logout() // Reject web roles
                            showError("Please use the Web Portal for administration.")
                        }
                    }
                } else {
                    showError(it.exceptionOrNull()?.message ?: "Login failed.")
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

    private fun navigateToMain() {
        startActivity(Intent(this, MainActivity::class.java))
        finish()
    }
}
