package com.smartmicrogrid.M2

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.Toast
import android.app.DatePickerDialog
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.databinding.FragmentReservationsBinding
import com.smartmicrogrid.models.Reservation
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.launch

class MyReservationsFragment : Fragment() {
    private var _binding: FragmentReservationsBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: ReservationAdapter
    private var reservations = listOf<Reservation>()
    private var historySelected = false
    private var statusFilter = "All statuses"
    private val db by lazy { AppDatabase.getDatabase(requireContext()) }

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, state: Bundle?): View {
        _binding = FragmentReservationsBinding.inflate(inflater, container, false); return binding.root
    }
    override fun onViewCreated(view: View, state: Bundle?) {
        adapter = ReservationAdapter(
            onCancel = { row -> cancel(row) },
            onModify = { row -> modify(row) },
            onApprove = { row -> approve(row) },
            onReject = { row -> reject(row) },
            onComplete = { row -> complete(row) },
            onPass = { row -> showPass(row) }
        )
        binding.reservationList.layoutManager = LinearLayoutManager(requireContext()); binding.reservationList.adapter = adapter
        binding.reservationBrowseSlots.setOnClickListener { startActivity(android.content.Intent(requireContext(), AvailableSlotsActivity::class.java)) }
        binding.reservationStatus.adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, listOf("All statuses", "Pending", "Approved", "Rejected", "Cancelled", "Completed", "Expired"))
        binding.reservationStatus.setSelection(0)
        binding.reservationStatus.onItemSelectedListener = object : android.widget.AdapterView.OnItemSelectedListener {
            override fun onNothingSelected(parent: android.widget.AdapterView<*>?) {}
            override fun onItemSelected(parent: android.widget.AdapterView<*>?, v: View?, position: Int, id: Long) { statusFilter = listOf("All statuses", "Pending", "Approved", "Rejected", "Cancelled", "Completed", "Expired")[position]; render() }
        }
        binding.reservationTabs.addTab(binding.reservationTabs.newTab().setText("Current / Pending"))
        binding.reservationTabs.addTab(binding.reservationTabs.newTab().setText("History"))
        binding.reservationTabs.addOnTabSelectedListener(object : com.google.android.material.tabs.TabLayout.OnTabSelectedListener {
            override fun onTabSelected(tab: com.google.android.material.tabs.TabLayout.Tab) { historySelected = tab.position == 1; render() }
            override fun onTabUnselected(tab: com.google.android.material.tabs.TabLayout.Tab) {}
            override fun onTabReselected(tab: com.google.android.material.tabs.TabLayout.Tab) {}
        })
        binding.reservationRefresh.setOnRefreshListener { refresh() }
        binding.reservationSearch.addTextChangedListener(TextWatcherAdapter { render() })
        binding.reservationDateFilter.setOnClickListener {
            val today = java.util.Calendar.getInstance()
            DatePickerDialog(
                requireContext(),
                { _, year, month, day ->
                    binding.reservationDateFilter.setText("%04d-%02d-%02d".format(year, month + 1, day))
                },
                today.get(java.util.Calendar.YEAR),
                today.get(java.util.Calendar.MONTH),
                today.get(java.util.Calendar.DAY_OF_MONTH)
            ).show()
        }
        binding.reservationDateFilter.addTextChangedListener(TextWatcherAdapter { render() })
        refresh()
    }
    private fun refresh() {
        viewLifecycleOwner.lifecycleScope.launch {
            binding.reservationRefresh.isRefreshing = true
            val cachedEntities = db.reservationDao().all()
            val cached = cachedEntities.map { it.toDomain() }
            if (cached.isNotEmpty()) {
                reservations = cached
                render()
            }
            try {
                val response = RetrofitClient.apiService.getReservations()
                if (response.isSuccessful && response.body()?.success == true) {
                    reservations = response.body()?.data.orEmpty(); db.reservationDao().replaceAll(reservations.map { com.smartmicrogrid.data.local.ReservationEntity.fromDomain(it) })
                    render()
                    val summary = RetrofitClient.apiService.getReservationSummary().body()?.data
                    summary?.let { binding.reservationCount.text = "${it.pendingCount} pending · ${it.approvedFutureCount} approved upcoming · ${"%.1f".format(it.totalEnergyReserved)} kWh reserved" }
                }
            } catch (_: Exception) { }
            binding.reservationRefresh.isRefreshing = false
            if (reservations.isEmpty()) binding.reservationEmpty.visibility = View.VISIBLE
        }
    }
    private fun render() {
        val q = binding.reservationSearch.text?.toString()?.trim().orEmpty()
        val filtered = reservations.filter { row ->
            val activeStatus = row.status in listOf("Pending", "Approved")
            val category = if (historySelected) !activeStatus else activeStatus
            val date = binding.reservationDateFilter.text?.toString()?.trim().orEmpty()
            val vCode = row.verificationCode.orEmpty()
            val pName = row.prosumerName.orEmpty()
            val mName = row.microgridName.orEmpty()
            category && (statusFilter == "All statuses" || row.status.equals(statusFilter, true)) && (date.isBlank() || ReservationTime.localDate(row.startTime) == date) && (q.isBlank() || row.energySlotId.contains(q, true) || row.microgridNodeId.contains(q, true) || row.status.contains(q, true) || vCode.contains(q, true) || pName.contains(q, true) || mName.contains(q, true))
        }
        adapter.submit(filtered); binding.reservationEmpty.visibility = if (filtered.isEmpty()) View.VISIBLE else View.GONE
        binding.reservationCount.text = "${reservations.count { it.status == "Pending" }} pending · ${reservations.count { it.status == "Approved" }} approved"
    }

    private fun approve(row: Reservation) {
        AlertDialog.Builder(requireContext())
            .setTitle("Approve reservation?")
            .setMessage("Approve allocation of ${row.energyAmount} kWh for ${row.prosumerName ?: "NIC: ${row.prosumerId}"}?")
            .setNegativeButton("Cancel", null)
            .setPositiveButton("Approve") { _, _ ->
                viewLifecycleOwner.lifecycleScope.launch {
                    try {
                        val response = RetrofitClient.apiService.approveReservation(row.id)
                        if (!response.isSuccessful || response.body()?.success != true)
                            throw Exception(response.body()?.message ?: "Approval failed")
                        Toast.makeText(requireContext(), "Reservation approved", Toast.LENGTH_SHORT).show()
                        refresh()
                    } catch (e: Exception) {
                        Toast.makeText(requireContext(), e.message ?: "Approval failed", Toast.LENGTH_LONG).show()
                    }
                }
            }.show()
    }

    private fun reject(row: Reservation) {
        val input = android.widget.EditText(requireContext()).apply { hint = "Reason for rejection (optional)" }
        AlertDialog.Builder(requireContext())
            .setTitle("Reject reservation")
            .setMessage("Reject reservation of ${row.energyAmount} kWh? Capacity will be released.")
            .setView(input)
            .setNegativeButton("Back", null)
            .setPositiveButton("Reject") { _, _ ->
                val reason = input.text.toString().trim()
                viewLifecycleOwner.lifecycleScope.launch {
                    try {
                        val body = if (reason.isNotEmpty()) mapOf("reason" to reason) else emptyMap()
                        val response = RetrofitClient.apiService.rejectReservation(row.id, body)
                        if (!response.isSuccessful || response.body()?.success != true)
                            throw Exception(response.body()?.message ?: "Rejection failed")
                        Toast.makeText(requireContext(), "Reservation rejected", Toast.LENGTH_SHORT).show()
                        refresh()
                    } catch (e: Exception) {
                        Toast.makeText(requireContext(), e.message ?: "Rejection failed", Toast.LENGTH_LONG).show()
                    }
                }
            }.show()
    }

    private fun complete(row: Reservation) {
        AlertDialog.Builder(requireContext())
            .setTitle("Confirm energy transfer?")
            .setMessage("Verify physical dispatch of ${row.energyAmount} kWh and mark completed?")
            .setNegativeButton("Back", null)
            .setPositiveButton("Verify & Complete") { _, _ ->
                viewLifecycleOwner.lifecycleScope.launch {
                    try {
                        val response = RetrofitClient.apiService.completeReservation(row.id)
                        if (!response.isSuccessful || response.body()?.success != true)
                            throw Exception(response.body()?.message ?: "Completion failed")
                        Toast.makeText(requireContext(), "Reservation fulfilled and completed", Toast.LENGTH_SHORT).show()
                        refresh()
                    } catch (e: Exception) {
                        Toast.makeText(requireContext(), e.message ?: "Completion failed", Toast.LENGTH_LONG).show()
                    }
                }
            }.show()
    }

    private fun showPass(row: Reservation) {
        val vCode = row.verificationCode?.ifBlank { null }
            ?: "SMG-RES-${if (row.id.length >= 6) row.id.takeLast(6).uppercase() else "000000"}"
        val pName = if (!row.prosumerName.isNullOrBlank()) row.prosumerName else "Registered Prosumer"
        val mName = if (!row.microgridName.isNullOrBlank()) row.microgridName else "Microgrid Hub"
        val cost = if (row.totalEstimatedCost > 0) row.totalEstimatedCost else (row.energyAmount * row.pricePerUnit)
        val costStr = if (cost > 0) "$%.2f".format(cost) else "Market rate"

        val details = """
TOKEN: $vCode

Status: ${row.status}
Prosumer: $pName (NIC: ${row.prosumerId})
Hub: $mName

Allocation: ${row.energyAmount} kWh
Est. Cost: $costStr

Scheduled Delivery Window:
${ReservationTime.display(row.startTime)} to ${ReservationTime.display(row.endTime)}
        """.trimIndent()

        AlertDialog.Builder(requireContext())
            .setTitle("Official Reservation Pass")
            .setMessage(details)
            .setPositiveButton("Done", null)
            .show()
    }

    private fun cancel(row: Reservation) {
        AlertDialog.Builder(requireContext()).setTitle("Cancel reservation?").setMessage("This will return ${row.energyAmount} kWh to the slot. Approved reservations need at least 12 hours' notice.")
            .setNegativeButton("Keep", null).setPositiveButton("Cancel reservation") { _, _ ->
                viewLifecycleOwner.lifecycleScope.launch {
                    try {
                        val response = RetrofitClient.apiService.cancelReservation(row.id)
                        if (!response.isSuccessful || response.body()?.success != true) throw Exception(response.body()?.message ?: "Cancellation failed")
                        AlertDialog.Builder(requireContext()).setTitle("Cancellation summary")
                            .setMessage("Reservation cancelled\n${row.energyAmount} kWh\n${ReservationTime.display(row.startTime)}\nStatus: Cancelled")
                            .setPositiveButton("Done", null).show()
                        refresh()
                    }
                    catch (e: Exception) { Toast.makeText(requireContext(), e.message ?: "Unable to cancel reservation", Toast.LENGTH_LONG).show() }
                }
            }.show()
    }
    private fun modify(row: Reservation) {
        val input = android.widget.EditText(requireContext()).apply { inputType = android.text.InputType.TYPE_CLASS_NUMBER or android.text.InputType.TYPE_NUMBER_FLAG_DECIMAL; setText(row.energyAmount.toString()) }
        AlertDialog.Builder(requireContext()).setTitle("Modify energy amount").setMessage("Changes are allowed at least 12 hours before delivery.").setView(input)
            .setNegativeButton("Back", null).setPositiveButton("Review") { _, _ ->
                val amount = input.text.toString().toDoubleOrNull()
                if (amount == null || amount <= 0) { Toast.makeText(requireContext(), "Enter an amount above zero", Toast.LENGTH_LONG).show(); return@setPositiveButton }
                viewLifecycleOwner.lifecycleScope.launch {
                    try {
                        val response = RetrofitClient.apiService.updateReservation(row.id, com.smartmicrogrid.models.CreateReservationRequest(row.energySlotId, amount))
                        if (!response.isSuccessful || response.body()?.success != true) throw Exception(response.body()?.message ?: "Update failed")
                        AlertDialog.Builder(requireContext()).setTitle("Reservation summary").setMessage("Updated request\n$amount kWh\n${ReservationTime.display(row.startTime)}\nStatus: ${row.status}").setPositiveButton("Done", null).show(); refresh()
                    } catch (e: Exception) { Toast.makeText(requireContext(), e.message ?: "Unable to update reservation", Toast.LENGTH_LONG).show() }
                }
            }.show()
    }
    override fun onDestroyView() { super.onDestroyView(); _binding = null }
}

class TextWatcherAdapter(private val changed: () -> Unit) : android.text.TextWatcher {
    override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
    override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) { changed() }
    override fun afterTextChanged(s: android.text.Editable?) {}
}
