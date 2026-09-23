package com.smartmicrogrid.ui

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.ViewModelProvider
import com.smartmicrogrid.R
import com.smartmicrogrid.auth.AuthViewModel
import com.smartmicrogrid.auth.ChangePasswordActivity
import com.smartmicrogrid.auth.EditProfileActivity
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.databinding.FragmentProfileBinding
import com.smartmicrogrid.models.User
import com.smartmicrogrid.utils.SessionManager
import com.smartmicrogrid.utils.ThemeManager

class ProfileFragment : Fragment() {

    private var _binding: FragmentProfileBinding? = null
    private val binding get() = _binding!!
    private lateinit var viewModel: AuthViewModel

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentProfileBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        
        viewModel = ViewModelProvider(this)[AuthViewModel::class.java]

        // Initialize Theme Manager Toggle Group
        val toggleGroup = binding.toggleGroupTheme
        when (ThemeManager.getCurrentTheme()) {
            ThemeManager.THEME_LIGHT -> toggleGroup.check(R.id.btn_theme_light)
            ThemeManager.THEME_DARK -> toggleGroup.check(R.id.btn_theme_dark)
            else -> toggleGroup.check(R.id.btn_theme_system)
        }

        toggleGroup.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (isChecked) {
                when (checkedId) {
                    R.id.btn_theme_light -> ThemeManager.applyTheme(ThemeManager.THEME_LIGHT)
                    R.id.btn_theme_dark -> ThemeManager.applyTheme(ThemeManager.THEME_DARK)
                    R.id.btn_theme_system -> ThemeManager.applyTheme(ThemeManager.THEME_SYSTEM)
                }
            }
        }

        setupListeners()
        observeViewModel()
    }

    override fun onResume() {
        super.onResume()
        // Reload profile from server each time fragment becomes visible
        viewModel.loadProfile()
    }

    private fun setupListeners() {
        binding.btnEditProfile.setOnClickListener {
            startActivity(Intent(requireActivity(), EditProfileActivity::class.java))
        }

        binding.btnChangePassword.setOnClickListener {
            startActivity(Intent(requireActivity(), ChangePasswordActivity::class.java))
        }

        binding.btnLogout.setOnClickListener {
            viewModel.logout()
            // Navigate back to Login and clear task stack
            val intent = Intent(requireActivity(), LoginActivity::class.java)
            intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            startActivity(intent)
            requireActivity().finish()
        }
    }

    private fun observeViewModel() {
        viewModel.isLoading.observe(viewLifecycleOwner) { loading ->
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        }

        viewModel.profileResult.observe(viewLifecycleOwner) { result ->
            result?.let {
                if (it.isSuccess) {
                    populateUI(it.getOrNull()!!)
                } else {
                    Toast.makeText(requireContext(), it.exceptionOrNull()?.message ?: "Failed to load profile", Toast.LENGTH_SHORT).show()
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

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
