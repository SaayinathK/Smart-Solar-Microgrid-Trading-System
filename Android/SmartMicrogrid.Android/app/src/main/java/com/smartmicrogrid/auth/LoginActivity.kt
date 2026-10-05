package com.smartmicrogrid.auth

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
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

        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)
        supportActionBar?.hide()

        binding.btnLogin.isEnabled = false
        binding.progressBar.visibility = View.VISIBLE
        lifecycleScope.launch {
            try {
                SessionManager.awaitReady()
                if (SessionManager.isLoggedIn()) {
                    navigateToMain()
                } else {
                    setupListeners()
                    observeViewModel()
                }
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                showError("Unable to restore saved login details. Close and reopen the app to retry.")
            } finally {
                binding.progressBar.visibility = View.GONE
            }
        }
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

        binding.tvRegisterVerifierLink.setOnClickListener {
            startActivity(Intent(this, RegisterVerifierActivity::class.java))
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
                        "MicrogridOperator" -> {
                            startActivity(Intent(this, com.smartmicrogrid.M3.MicrogridOperatorMainActivity::class.java))
                            finish()
                        }
                        else -> {
                            startActivity(Intent(this, MainActivity::class.java))
                            finish()
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
        val destination = when (SessionManager.getUserRole()) {
            "Prosumer" -> com.smartmicrogrid.M2.ProsumerMainActivity::class.java
            "MicrogridOperator" -> com.smartmicrogrid.M3.MicrogridOperatorMainActivity::class.java
            else -> MainActivity::class.java
        }
        startActivity(Intent(this, destination))
        finish()
    }
}
