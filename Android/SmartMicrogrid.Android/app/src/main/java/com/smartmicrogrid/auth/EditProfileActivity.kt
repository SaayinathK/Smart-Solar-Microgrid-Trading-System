package com.smartmicrogrid.auth

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import com.smartmicrogrid.databinding.ActivityEditProfileBinding
import com.smartmicrogrid.models.UpdateProfileRequest
import com.smartmicrogrid.utils.SessionManager

class EditProfileActivity : AppCompatActivity() {

    private lateinit var binding: ActivityEditProfileBinding
    private val viewModel: AuthViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityEditProfileBinding.inflate(layoutInflater)
        setContentView(binding.root)
        supportActionBar?.title = "Edit Profile"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        populateFields()
        setupListeners()
        observeViewModel()
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }

    private fun populateFields() {
        val user = SessionManager.getUser()
        if (user != null) {
            binding.etFirstName.setText(user.firstName)
            binding.etLastName.setText(user.lastName)
            binding.etPhone.setText(user.phoneNumber)
        }
    }

    private fun setupListeners() {
        binding.btnSave.setOnClickListener {
            val firstName = binding.etFirstName.text.toString().trim()
            val lastName = binding.etLastName.text.toString().trim()
            val phone = binding.etPhone.text.toString().trim()

            if (firstName.isEmpty() || lastName.isEmpty()) {
                showError("First name and last name are required.")
                return@setOnClickListener
            }

            hideError()
            viewModel.updateProfile(UpdateProfileRequest(firstName, lastName, phone))
        }
    }

    private fun observeViewModel() {
        viewModel.isLoading.observe(this) { loading ->
            binding.btnSave.isEnabled = !loading
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
            binding.btnSave.text = if (loading) "Saving..." else "Save Changes"
        }

        viewModel.updateResult.observe(this) { result ->
            result?.let {
                if (it.isSuccess) {
                    Toast.makeText(this, "Profile updated successfully", Toast.LENGTH_SHORT).show()
                    finish() // Back to Profile screen, which will reload
                } else {
                    showError(it.exceptionOrNull()?.message ?: "Update failed.")
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
