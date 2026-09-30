package com.smartmicrogrid.M4

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import com.google.android.material.materialswitch.MaterialSwitch
import com.google.android.material.snackbar.Snackbar
import com.google.android.material.textfield.TextInputEditText
import com.smartmicrogrid.R
import com.smartmicrogrid.models.SystemConfiguration
import com.smartmicrogrid.models.UpdateConfigurationRequest

/**
 * Platform settings. Details and the two feature toggles are saved separately
 * because they map to different endpoints, and the details call deliberately
 * omits MaintenanceMessage: sending it as null would erase a live banner.
 */
class AdminSettingsFragment : Fragment() {

    private val viewModel: AdminViewModel by viewModels()

    private var current: SystemConfiguration? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View = inflater.inflate(R.layout.fragment_admin_settings, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        view.findViewById<View>(R.id.m4_save_details).setOnClickListener { saveDetails() }
        view.findViewById<View>(R.id.m4_save_toggles).setOnClickListener { saveToggles() }

        viewModel.configuration.observe(viewLifecycleOwner) { config ->
            if (config != null) {
                current = config
                populate(config)
            }
        }

        viewModel.savedSuccessfully.observe(viewLifecycleOwner) { saved ->
            if (saved == true && view.isAttachedToWindow) {
                Snackbar.make(view, R.string.m4_saved, Snackbar.LENGTH_SHORT).show()
                viewModel.clearMessages()
            }
        }

        viewModel.errorMessage.observe(viewLifecycleOwner) { message ->
            if (message != null && view.isAttachedToWindow) {
                Snackbar.make(view, message, Snackbar.LENGTH_LONG).show()
                viewModel.clearMessages()
            }
        }

        viewModel.loadConfiguration()
    }

    private fun populate(config: SystemConfiguration) {
        val view = requireView()

        view.findViewById<TextInputEditText>(R.id.m4_platform_name).setText(config.platformName)
        view.findViewById<TextInputEditText>(R.id.m4_platform_description)
            .setText(config.platformDescription)
        view.findViewById<TextInputEditText>(R.id.m4_session_timeout)
            .setText(config.sessionTimeoutMinutes.toString())
        view.findViewById<TextInputEditText>(R.id.m4_max_login_attempts)
            .setText(config.maxLoginAttempts.toString())
        view.findViewById<TextInputEditText>(R.id.m4_default_page_size)
            .setText(config.defaultPageSize.toString())

        view.findViewById<MaterialSwitch>(R.id.m4_maintenance_mode).isChecked = config.maintenanceMode
        view.findViewById<TextInputEditText>(R.id.m4_maintenance_message)
            .setText(config.maintenanceMessage.orEmpty())
        view.findViewById<MaterialSwitch>(R.id.m4_allow_registration).isChecked = config.allowRegistration

        view.findViewById<TextView>(R.id.m4_config_updated).text =
            if (config.updatedAt.isBlank()) "" else
                getString(R.string.m4_last_updated, config.updatedAt)
    }

    private fun saveDetails() {
        val view = view ?: return

        val platformName = text(R.id.m4_platform_name)
        if (platformName.length < 3) {
            Snackbar.make(view, "Platform name must be at least 3 characters.", Snackbar.LENGTH_LONG).show()
            return
        }

        viewModel.saveConfiguration(
            UpdateConfigurationRequest(
                platformName = platformName,
                platformDescription = text(R.id.m4_platform_description),
                // Null keeps any existing banner; the toggle endpoint owns it.
                maintenanceMessage = null,
                sessionTimeoutMinutes = number(R.id.m4_session_timeout),
                maxLoginAttempts = number(R.id.m4_max_login_attempts),
                defaultPageSize = number(R.id.m4_default_page_size)
            )
        )
    }

    private fun saveToggles() {
        val view = view ?: return

        val baseline = current
        val maintenance = view.findViewById<MaterialSwitch>(R.id.m4_maintenance_mode).isChecked
        val registration = view.findViewById<MaterialSwitch>(R.id.m4_allow_registration).isChecked

        // Each toggle is applied only when it differs from the last value the
        // server reported, and they are independent: changing one must not skip
        // the other.
        val maintenanceChanged = baseline == null || maintenance != baseline!!.maintenanceMode
        val registrationChanged = baseline == null || registration != baseline!!.allowRegistration

        if (!maintenanceChanged && !registrationChanged) {
            Snackbar.make(view, "No toggle changes to apply.", Snackbar.LENGTH_SHORT).show()
            return
        }

        if (maintenanceChanged) {
            viewModel.setMaintenanceMode(maintenance, text(R.id.m4_maintenance_message).ifBlank { null })
        }

        if (registrationChanged) {
            viewModel.setRegistrationMode(registration)
        }
    }

    private fun text(id: Int): String =
        view?.findViewById<TextInputEditText>(id)?.text?.toString()?.trim().orEmpty()

    private fun number(id: Int): Int = text(id).toIntOrNull() ?: 0
}
