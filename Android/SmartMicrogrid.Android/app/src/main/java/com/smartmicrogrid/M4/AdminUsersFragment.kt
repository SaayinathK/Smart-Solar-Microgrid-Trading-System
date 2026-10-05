package com.smartmicrogrid.M4

import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.ProgressBar
import android.widget.Spinner
import android.widget.TextView
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.snackbar.Snackbar
import com.google.android.material.textfield.TextInputEditText
import com.smartmicrogrid.R
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.User
import kotlinx.coroutines.launch

/**
 * M4 account lifecycle. Reads go through [RetrofitClient] directly because user
 * management is read-mostly and the account-status action is a single call, so
 * there is nothing worth caching offline.
 */
class AdminUsersFragment : Fragment() {

    private val viewModel: AdminViewModel by viewModels()
    private lateinit var adapter: AdminUserAdapter

    private val statusOptions = listOf("All", "Active", "Inactive", "Suspended", "Pending")

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View = inflater.inflate(R.layout.fragment_admin_users, container, false)

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        adapter = AdminUserAdapter(
            onActivate = { user -> confirmStatusChange(user, "Active", getString(R.string.m4_activate)) },
            onSuspend = { user -> confirmStatusChange(user, "Suspended", getString(R.string.m4_suspend)) }
        )

        view.findViewById<RecyclerView>(R.id.m4_user_list).apply {
            layoutManager = LinearLayoutManager(requireContext())
            adapter = this@AdminUsersFragment.adapter
        }

        val spinner = view.findViewById<Spinner>(R.id.m4_user_status_filter)
        spinner.adapter = ArrayAdapter(
            requireContext(),
            android.R.layout.simple_spinner_dropdown_item,
            statusOptions
        )

        val search = view.findViewById<TextInputEditText>(R.id.m4_user_search)
        search.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, a: Int, b: Int, c: Int) = Unit
            override fun onTextChanged(s: CharSequence?, a: Int, b: Int, c: Int) = Unit
            override fun afterTextChanged(s: Editable?) = loadUsers(spinner.selectedItem?.toString())
        })

        spinner.onItemSelectedListener = object : android.widget.AdapterView.OnItemSelectedListener {
            override fun onItemSelected(
                parent: android.widget.AdapterView<*>?,
                view: View?,
                position: Int,
                id: Long
            ) = loadUsers(statusOptions[position])

            override fun onNothingSelected(parent: android.widget.AdapterView<*>?) = Unit
        }

        viewModel.isLoading.observe(viewLifecycleOwner) { loading ->
            view.findViewById<ProgressBar>(R.id.m4_user_loading).visibility =
                if (loading) View.VISIBLE else View.GONE
        }

        loadUsers("All")
    }

    private fun loadUsers(status: String? = "All") {
        val view = view ?: return
        view.findViewById<ProgressBar>(R.id.m4_user_loading).visibility = View.VISIBLE

        val search = view.findViewById<TextInputEditText>(R.id.m4_user_search).text?.toString()?.trim()

        viewLifecycleOwner.lifecycleScope.launch {
            val params = mutableMapOf<String, String>()
            if (!search.isNullOrEmpty()) params["search"] = search
            if (!status.isNullOrEmpty() && status != "All") params["accountStatus"] = status

            try {
                val response = RetrofitClient.apiService.getAllUsers(
                    search = params["search"],
                    accountStatus = params["accountStatus"]
                )
                val body = response.body()

                if (response.isSuccessful && body?.success == true) {
                    val users = body.data.orEmpty()
                    adapter.submit(users)
                    view.findViewById<TextView>(R.id.m4_user_empty).visibility =
                        if (users.isEmpty()) View.VISIBLE else View.GONE
                } else {
                    adapter.submit(emptyList())
                    view.findViewById<TextView>(R.id.m4_user_empty).visibility = View.VISIBLE
                    if (view.isAttachedToWindow) {
                        Snackbar.make(
                            view,
                            body?.message ?: "Failed to load users.",
                            Snackbar.LENGTH_LONG
                        ).show()
                    }
                }
            } catch (e: Exception) {
                adapter.submit(emptyList())
                view.findViewById<TextView>(R.id.m4_user_empty).visibility = View.VISIBLE
                if (view.isAttachedToWindow) {
                    Snackbar.make(
                        view,
                        e.message ?: "Error connecting to the server.",
                        Snackbar.LENGTH_LONG
                    ).show()
                }
            } finally {
                if (isAdded) {
                    view.findViewById<ProgressBar>(R.id.m4_user_loading).visibility = View.GONE
                }
            }
        }
    }

    private fun confirmStatusChange(user: User, newStatus: String, actionLabel: String) {
        AlertDialog.Builder(requireContext())
            .setTitle(actionLabel)
            .setMessage(
                if (newStatus == "Active") {
                    "Activate ${user.firstName} ${user.lastName}? They will be able to sign in again."
                } else {
                    "Suspend ${user.firstName} ${user.lastName}? They will be signed out on their next request."
                }
            )
            .setPositiveButton(actionLabel) { _, _ -> applyStatus(user, newStatus) }
            .setNegativeButton(android.R.string.cancel, null)
            .show()
    }

    private fun applyStatus(user: User, newStatus: String) {
        val view = view ?: return
        view.findViewById<ProgressBar>(R.id.m4_user_loading).visibility = View.VISIBLE

        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val response = RetrofitClient.apiService
                    .updateUserAccountStatus(user.id, mapOf("accountStatus" to newStatus))
                val body = response.body()

                if (response.isSuccessful && body?.success == true) {
                    if (view.isAttachedToWindow) {
                        Snackbar.make(
                            view,
                            body.message ?: "Account updated.",
                            Snackbar.LENGTH_SHORT
                        ).show()
                    }
                } else if (view.isAttachedToWindow) {
                    Snackbar.make(
                        view,
                        body?.message ?: "Failed to update the account.",
                        Snackbar.LENGTH_LONG
                    ).show()
                }
            } catch (e: Exception) {
                if (view.isAttachedToWindow) {
                    Snackbar.make(
                        view,
                        e.message ?: "Error connecting to the server.",
                        Snackbar.LENGTH_LONG
                    ).show()
                }
            } finally {
                loadUsers(
                    view.findViewById<Spinner>(R.id.m4_user_status_filter)
                        .selectedItem?.toString() ?: "All"
                )
            }
        }
    }
}
