package com.smartmicrogrid.auth

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.MainActivity
import com.smartmicrogrid.databinding.ActivityLoginBinding
import com.smartmicrogrid.utils.SessionManager
import com.smartmicrogrid.utils.ServerDiscovery
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.launch

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
        val cleaned = ServerDiscovery.cleanDisplayUrl(current)
        binding.tvServerConfig.text = cleaned

        lifecycleScope.launch {
            binding.tvServerConfig.text = "$cleaned (Checking...)"
            val isOnline = ServerDiscovery.probeUrl(current)
            if (isOnline) {
                binding.tvServerStatusDot.backgroundTintList = android.content.res.ColorStateList.valueOf(android.graphics.Color.parseColor("#10B981"))
                binding.tvServerStatusLabel.text = "Server Online (Connected)"
                binding.tvServerStatusLabel.setTextColor(android.graphics.Color.parseColor("#10B981"))
                binding.tvServerConfig.text = cleaned
            } else {
                binding.tvServerStatusDot.backgroundTintList = android.content.res.ColorStateList.valueOf(android.graphics.Color.parseColor("#EF4444"))
                binding.tvServerStatusLabel.text = "Server Offline (Tap to fix)"
                binding.tvServerStatusLabel.setTextColor(android.graphics.Color.parseColor("#EF4444"))
                binding.tvServerConfig.text = "$cleaned • Unreachable"
            }
        }
    }

    private fun triggerAutoDetect() {
        binding.btnAutoDetectServer.text = "Scanning..."
        binding.btnAutoDetectServer.isEnabled = false
        lifecycleScope.launch {
            val found = ServerDiscovery.autoDiscoverServer(this@LoginActivity)
            if (found != null) {
                SessionManager.setBaseUrl(found)
                updateServerDisplay()
                android.widget.Toast.makeText(
                    this@LoginActivity,
                    "Connected to active server: ${ServerDiscovery.cleanDisplayUrl(found)}",
                    android.widget.Toast.LENGTH_LONG
                ).show()
            } else {
                updateServerDisplay()
                android.widget.Toast.makeText(
                    this@LoginActivity,
                    "No server responded automatically. Tap the connection card to enter the laptop's IP manually.",
                    android.widget.Toast.LENGTH_LONG
                ).show()
            }
            binding.btnAutoDetectServer.text = "Auto-Detect"
            binding.btnAutoDetectServer.isEnabled = true
        }
    }

    private fun showServerConfigDialog() {
        val current = SessionManager.getBaseUrl()
        val cleaned = ServerDiscovery.cleanDisplayUrl(current)

        val layout = android.widget.LinearLayout(this).apply {
            orientation = android.widget.LinearLayout.VERTICAL
            setPadding(50, 20, 50, 10)
        }

        val input = android.widget.EditText(this).apply {
            setText(cleaned)
            hint = "e.g. 192.168.1.6:5050"
            setSelection(text.length)
        }
        layout.addView(input)

        val presetLayout = android.widget.LinearLayout(this).apply {
            orientation = android.widget.LinearLayout.HORIZONTAL
            setPadding(0, 16, 0, 8)
        }

        fun makeChip(title: String, target: String): android.widget.TextView {
            return android.widget.TextView(this).apply {
                text = title
                textSize = 11f
                setTextColor(android.graphics.Color.parseColor("#2563EB"))
                setBackgroundResource(com.smartmicrogrid.R.drawable.bg_status_pill)
                setPadding(20, 10, 20, 10)
                val params = android.widget.LinearLayout.LayoutParams(
                    android.widget.LinearLayout.LayoutParams.WRAP_CONTENT,
                    android.widget.LinearLayout.LayoutParams.WRAP_CONTENT
                ).apply { setMargins(0, 0, 12, 0) }
                layoutParams = params
                setOnClickListener {
                    input.setText(target)
                }
            }
        }

        presetLayout.addView(makeChip("Emulator", "10.0.2.2:5050"))
        presetLayout.addView(makeChip("Wi-Fi Default", "192.168.1.6:5050"))
        presetLayout.addView(makeChip("IIS Port 80", "192.168.1.6:80"))
        layout.addView(presetLayout)

        val tvProbeResult = android.widget.TextView(this).apply {
            text = "Tap 'Test' to verify reachability before saving."
            textSize = 11f
            setTextColor(android.graphics.Color.parseColor("#94A3B8"))
            setPadding(4, 12, 4, 8)
        }
        layout.addView(tvProbeResult)

        val dialog = androidx.appcompat.app.AlertDialog.Builder(this)
            .setTitle("Server Connection Setup")
            .setMessage("Set the API server IP and port. Kestrel runs on :5050, and IIS runs on :80 or :5000:")
            .setView(layout)
            .setNeutralButton("Test") { _, _ -> }
            .setPositiveButton("Save & Connect") { _, _ ->
                val text = input.text.toString().trim()
                if (text.isNotEmpty()) {
                    val formatted = ServerDiscovery.formatBaseUrl(text)
                    SessionManager.setBaseUrl(formatted)
                    updateServerDisplay()
                    android.widget.Toast.makeText(this, "Connecting to ${ServerDiscovery.cleanDisplayUrl(formatted)}", android.widget.Toast.LENGTH_SHORT).show()
                }
            }
            .setNegativeButton("Cancel", null)
            .create()

        dialog.show()

        // Override neutral button so it doesn't dismiss the dialog on click
        dialog.getButton(androidx.appcompat.app.AlertDialog.BUTTON_NEUTRAL)?.setOnClickListener {
            val text = input.text.toString().trim()
            if (text.isEmpty()) {
                tvProbeResult.text = "Please enter an IP and port to test."
                return@setOnClickListener
            }
            val formatted = ServerDiscovery.formatBaseUrl(text)
            tvProbeResult.text = "Testing connection to ${ServerDiscovery.cleanDisplayUrl(formatted)}..."
            tvProbeResult.setTextColor(android.graphics.Color.parseColor("#3B82F6"))

            lifecycleScope.launch {
                val ok = ServerDiscovery.probeUrl(formatted)
                if (ok) {
                    tvProbeResult.text = "🟢 Reachable! Server responded successfully."
                    tvProbeResult.setTextColor(android.graphics.Color.parseColor("#10B981"))
                } else {
                    tvProbeResult.text = "🔴 Unreachable. Check that the laptop and phone are on the same Wi-Fi network."
                    tvProbeResult.setTextColor(android.graphics.Color.parseColor("#EF4444"))
                }
            }
        }
    }
}
