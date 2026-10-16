package com.smartmicrogrid.auth

import android.view.View
import android.widget.LinearLayout
import android.widget.TextView
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import com.google.android.material.textfield.TextInputEditText
import com.google.android.material.textfield.TextInputLayout
import com.smartmicrogrid.R
import com.smartmicrogrid.utils.Constants
import com.smartmicrogrid.utils.ServerDiscovery
import com.smartmicrogrid.utils.SessionManager
import com.smartmicrogrid.utils.ThemeManager
import kotlinx.coroutines.launch
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull

/** Shared appearance and connection controls for both authentication screens. */
object AuthScreenControls {
    fun bind(activity: AppCompatActivity, themeButton: View, connectionButton: View) {
        themeButton.setOnClickListener {
            val modes = arrayOf(ThemeManager.THEME_SYSTEM, ThemeManager.THEME_LIGHT, ThemeManager.THEME_DARK)
            AlertDialog.Builder(activity)
                .setTitle("Appearance")
                .setSingleChoiceItems(arrayOf("System default", "Light", "Dark"), modes.indexOf(ThemeManager.getCurrentTheme())) { dialog, index ->
                    dialog.dismiss()
                    ThemeManager.applyTheme(modes[index])
                }
                .setNegativeButton("Cancel", null)
                .show()
        }
        connectionButton.setOnClickListener { showConnectionDialog(activity) }
    }

    private fun showConnectionDialog(activity: AppCompatActivity) {
        val spacing = (24 * activity.resources.displayMetrics.density).toInt()
        val field = TextInputLayout(activity).apply {
            hint = "Server address"
            boxBackgroundMode = TextInputLayout.BOX_BACKGROUND_OUTLINE
        }
        val input = TextInputEditText(field.context).apply {
            setSingleLine(true)
            inputType = android.text.InputType.TYPE_CLASS_TEXT or android.text.InputType.TYPE_TEXT_VARIATION_URI
            setText(ServerDiscovery.cleanDisplayUrl(SessionManager.getBaseUrl()))
            setSelection(text?.length ?: 0)
        }
        field.addView(input)
        val status = TextView(activity).apply {
            text = "Checking connection…"
            textSize = 14f
            setPadding(0, spacing / 2, 0, 0)
            accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE
        }
        val content = LinearLayout(activity).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(spacing, spacing / 2, spacing, 0)
            addView(field)
            addView(status)
        }
        val dialog = AlertDialog.Builder(activity)
            .setTitle("Connection settings")
            .setMessage("Use the same Wi-Fi network as the server.")
            .setView(content)
            .setPositiveButton("Connect", null)
            .setNeutralButton("Auto-detect", null)
            .setNegativeButton("Close", null)
            .create()
        dialog.show()

        fun showStatus(message: String, ok: Boolean) {
            status.text = message
            status.setTextColor(ContextCompat.getColor(activity, if (ok) R.color.auth_success else R.color.auth_error))
        }
        fun setBusy(busy: Boolean) {
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).isEnabled = !busy
            dialog.getButton(AlertDialog.BUTTON_NEUTRAL).isEnabled = !busy
            input.isEnabled = !busy
        }
        val initialProbe = activity.lifecycleScope.launch {
            val online = ServerDiscovery.checkCurrentConnection()
            showStatus(if (online) "Connected" else "Server unavailable. Start the API, then reconnect.", online)
        }
        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
            val raw = input.text?.toString().orEmpty().trim()
            val formatted = ServerDiscovery.formatBaseUrl(raw)
            val url = formatted.toHttpUrlOrNull()
            if (raw.isBlank() || url == null || url.username.isNotEmpty() || url.password.isNotEmpty() || url.query != null || url.fragment != null) {
                field.error = "Enter a valid server address, such as ${ServerDiscovery.cleanDisplayUrl(Constants.DEFAULT_LAN_URL)}."
                return@setOnClickListener
            }
            field.error = null
            initialProbe.cancel()
            setBusy(true)
            status.text = "Connecting…"
            activity.lifecycleScope.launch {
                val online = ServerDiscovery.probeUrl(formatted)
                if (online) SessionManager.setBaseUrl(formatted)
                showStatus(if (online) "Connected" else "Cannot reach this server. Check the address and that the API is running.", online)
                setBusy(false)
            }
        }
        dialog.getButton(AlertDialog.BUTTON_NEUTRAL).setOnClickListener {
            initialProbe.cancel()
            field.error = null
            setBusy(true)
            status.text = "Finding server…"
            activity.lifecycleScope.launch {
                val found = ServerDiscovery.autoDiscoverServer(activity)
                if (found != null) {
                    SessionManager.setBaseUrl(found)
                    input.setText(ServerDiscovery.cleanDisplayUrl(found))
                }
                showStatus(if (found != null) "Connected" else "No server found. Start the API or enter its address.", found != null)
                setBusy(false)
            }
        }
    }
}
