package com.smartmicrogrid.M2

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.Toast
import android.app.DatePickerDialog
import android.app.TimePickerDialog
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
        val dialogView = LayoutInflater.from(requireContext()).inflate(com.smartmicrogrid.R.layout.dialog_modify_reservation, null)
        val tvCurrentSlot = dialogView.findViewById<android.widget.TextView>(com.smartmicrogrid.R.id.tv_current_slot_info)
        val tvCurrentTime = dialogView.findViewById<android.widget.TextView>(com.smartmicrogrid.R.id.tv_current_time_info)
        val tvCurrentAmount = dialogView.findViewById<android.widget.TextView>(com.smartmicrogrid.R.id.tv_current_amount_info)
        val etAmount = dialogView.findViewById<android.widget.EditText>(com.smartmicrogrid.R.id.et_modify_energy_amount)
        val spinnerSlots = dialogView.findViewById<android.widget.Spinner>(com.smartmicrogrid.R.id.spinner_modify_slot)
        val tvSlotHint = dialogView.findViewById<android.widget.TextView>(com.smartmicrogrid.R.id.tv_slot_status_hint)
        val tvSelectedTime = dialogView.findViewById<android.widget.TextView>(com.smartmicrogrid.R.id.tv_selected_start_time)
        val btnPickTime = dialogView.findViewById<View>(com.smartmicrogrid.R.id.btn_pick_time)
        val tvCostEstimate = dialogView.findViewById<android.widget.TextView>(com.smartmicrogrid.R.id.tv_estimated_cost)
        val btnCancel = dialogView.findViewById<android.widget.Button>(com.smartmicrogrid.R.id.btn_dialog_cancel)
        val btnSave = dialogView.findViewById<android.widget.Button>(com.smartmicrogrid.R.id.btn_dialog_save)

        tvCurrentSlot.text = "Slot: ${row.energySlotId}"
        tvCurrentTime.text = "Delivery: ${ReservationTime.display(row.startTime)}"
        tvCurrentAmount.text = "Allocated: ${row.energyAmount} kWh"
        etAmount.setText(row.energyAmount.toString())
        tvSelectedTime.text = ReservationTime.display(row.startTime)

        var selectedSlotId = row.energySlotId
        var selectedStartTimeUtc: String? = null
        var unitPrice = row.pricePerUnit

        fun updateCost() {
            val amt = etAmount.text.toString().toDoubleOrNull() ?: 0.0
            val total = amt * unitPrice
            tvCostEstimate.text = if (total > 0) "LKR %.2f".format(total) else "Market rate"
        }
        updateCost()
        etAmount.addTextChangedListener(TextWatcherAdapter { updateCost() })

        // Load available slots
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val slotsRes = RetrofitClient.apiService.getEnergySlots(microgridId = row.microgridNodeId, status = "Available")
                val slotList = slotsRes.body()?.data.orEmpty()
                val slots = if (slotList.isNotEmpty()) slotList else {
                    val availRes = RetrofitClient.apiService.getEnergyAvailability()
                    availRes.body()?.data.orEmpty().filter { it.microgridNodeId == row.microgridNodeId }.map { a ->
                        com.smartmicrogrid.models.EnergySlot(
                            id = a.energySlotId,
                            microgridNodeId = a.microgridNodeId,
                            microgridName = a.microgridName,
                            location = a.location,
                            energyAmount = a.energyAmount,
                            availableAmount = a.availableAmount,
                            startTime = a.startTime,
                            endTime = a.endTime,
                            pricePerUnit = a.pricePerUnit
                        )
                    }
                }

                val options = mutableListOf<String>()
                options.add("Keep Current Slot (${ReservationTime.display(row.startTime)})")

                val alternateSlots = slots.filter { it.id != row.energySlotId }
                alternateSlots.forEach { s ->
                    options.add("${ReservationTime.display(s.startTime)} • ${s.availableAmount} kWh avail (LKR ${s.pricePerUnit}/kWh)")
                }

                val spinnerAdapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, options)
                spinnerSlots.adapter = spinnerAdapter

                spinnerSlots.onItemSelectedListener = object : android.widget.AdapterView.OnItemSelectedListener {
                    override fun onItemSelected(p0: android.widget.AdapterView<*>?, p1: View?, position: Int, id: Long) {
                        if (position == 0) {
                            selectedSlotId = row.energySlotId
                            unitPrice = row.pricePerUnit
                            if (selectedStartTimeUtc == null) {
                                tvSelectedTime.text = ReservationTime.display(row.startTime)
                            }
                        } else {
                            val chosen = alternateSlots[position - 1]
                            selectedSlotId = chosen.id
                            unitPrice = chosen.pricePerUnit
                            selectedStartTimeUtc = chosen.startTime
                            tvSelectedTime.text = ReservationTime.display(chosen.startTime)
                        }
                        updateCost()
                    }
                    override fun onNothingSelected(p0: android.widget.AdapterView<*>?) {}
                }

                if (alternateSlots.isNotEmpty()) {
                    tvSlotHint.text = "${alternateSlots.size} alternate slot(s) available on this microgrid."
                } else {
                    tvSlotHint.text = "No alternate slots currently published for this microgrid."
                }
            } catch (e: Exception) {
                tvSlotHint.text = "Using default slot options."
            }
        }

        // Custom Time picker
        btnPickTime.setOnClickListener {
            val now = java.util.Calendar.getInstance()
            android.app.DatePickerDialog(
                requireContext(),
                { _, year, month, day ->
                    android.app.TimePickerDialog(
                        requireContext(),
                        { _, hour, minute ->
                            val cal = java.util.Calendar.getInstance(java.util.TimeZone.getTimeZone("UTC"))
                            cal.set(year, month, day, hour, minute, 0)
                            cal.set(java.util.Calendar.MILLISECOND, 0)
                            val instant = cal.toInstant()
                            selectedStartTimeUtc = instant.toString()
                            tvSelectedTime.text = ReservationTime.display(selectedStartTimeUtc!!)
                        },
                        now.get(java.util.Calendar.HOUR_OF_DAY),
                        now.get(java.util.Calendar.MINUTE),
                        false
                    ).show()
                },
                now.get(java.util.Calendar.YEAR),
                now.get(java.util.Calendar.MONTH),
                now.get(java.util.Calendar.DAY_OF_MONTH)
            ).show()
        }

        val dialog = AlertDialog.Builder(requireContext())
            .setView(dialogView)
            .create()

        btnCancel.setOnClickListener { dialog.dismiss() }

        btnSave.setOnClickListener {
            val amount = etAmount.text.toString().toDoubleOrNull()
            if (amount == null || amount <= 0) {
                Toast.makeText(requireContext(), "Enter an energy amount greater than zero.", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            btnSave.isEnabled = false
            btnSave.text = "Updating..."

            viewLifecycleOwner.lifecycleScope.launch {
                try {
                    val req = com.smartmicrogrid.models.UpdateReservationRequest(
                        energySlotId = selectedSlotId,
                        energyAmount = amount,
                        startTime = selectedStartTimeUtc
                    )
                    val response = RetrofitClient.apiService.updateReservation(row.id, req)
                    if (!response.isSuccessful || response.body()?.success != true) {
                        throw Exception(response.body()?.message ?: "Update failed")
                    }

                    dialog.dismiss()

                    val updatedTime = selectedStartTimeUtc ?: row.startTime
                    AlertDialog.Builder(requireContext())
                        .setTitle("Reservation Updated")
                        .setMessage("Your reservation has been modified:\n\n• Energy Amount: $amount kWh\n• Delivery Time: ${ReservationTime.display(updatedTime)}\n• Slot ID: $selectedSlotId\n\nStatus: ${row.status}")
                        .setPositiveButton("Done", null)
                        .show()

                    refresh()
                } catch (e: Exception) {
                    btnSave.isEnabled = true
                    btnSave.text = "Update Reservation"
                    Toast.makeText(requireContext(), e.message ?: "Unable to update reservation", Toast.LENGTH_LONG).show()
                }
            }
        }

        dialog.show()
    }

    override fun onDestroyView() { super.onDestroyView(); _binding = null }
}

class TextWatcherAdapter(private val changed: () -> Unit) : android.text.TextWatcher {
    override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
    override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) { changed() }
    override fun afterTextChanged(s: android.text.Editable?) {}
}
