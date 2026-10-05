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
            startActivity(Intent(this, mainActivityForRole(SessionManager.getUserRole())))
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

        updateServerDisplay()
        binding.tvServerConfig.setOnClickListener {
            showServerConfigDialog()
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
                    startActivity(Intent(this, mainActivityForRole(SessionManager.getUserRole())))
                    finish()
                } else {
                    showError(it.exceptionOrNull()?.message ?: "Login failed.")
                }
            }
        }
    }

    /**
     * Admins land on the M4 administration portal, which is the only module they
     * have access to. Every other role keeps its existing entry point.
     */
    private fun mainActivityForRole(role: String): Class<*> = when (role) {
        "Admin" -> com.smartmicrogrid.M4.AdminMainActivity::class.java
        "Prosumer" -> com.smartmicrogrid.M2.ProsumerMainActivity::class.java
        "MicrogridOperator" -> com.smartmicrogrid.M3.MicrogridOperatorMainActivity::class.java
        else -> MainActivity::class.java
    }

    private fun showError(message: String) {
        binding.tvError.text = message
        binding.tvError.visibility = View.VISIBLE
    }

    private fun hideError() {
        binding.tvError.visibility = View.GONE
    }

    private fun updateServerDisplay() {
        val current = SessionManager.getBaseUrl()
        val cleaned = current.removePrefix("http://").removePrefix("https://").removeSuffix("/api/").removeSuffix("/")
        binding.tvServerConfig.text = "Server: $cleaned (Tap to change)"
    }

    private fun showServerConfigDialog() {
        val input = android.widget.EditText(this).apply {
            val current = SessionManager.getBaseUrl()
            val cleaned = current.removePrefix("http://").removePrefix("https://").removeSuffix("/api/").removeSuffix("/")
            setText(cleaned)
            hint = "192.168.1.6:5050"
            setSelection(text.length)
        }

        val container = android.widget.FrameLayout(this).apply {
            setPadding(60, 20, 60, 0)
            addView(input)
        }

        androidx.appcompat.app.AlertDialog.Builder(this)
            .setTitle("Server Connection")
            .setMessage("Enter the server laptop's IP address and port (e.g. 192.168.1.6:5050):")
            .setView(container)
            .setPositiveButton("Save") { _, _ ->
                var text = input.text.toString().trim()
                if (text.isNotEmpty()) {
                    if (!text.startsWith("http://") && !text.startsWith("https://")) {
                        text = "http://$text"
                    }
                    if (!text.endsWith("/api/")) {
                        text = text.removeSuffix("/") + "/api/"
                    }
                    SessionManager.setBaseUrl(text)
                    updateServerDisplay()
                    android.widget.Toast.makeText(this, "Server URL updated.", android.widget.Toast.LENGTH_SHORT).show()
                }
            }
            .setNegativeButton("Cancel", null)
            .show()
    }
}
