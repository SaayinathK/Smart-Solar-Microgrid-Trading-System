package com.smartmicrogrid.M2

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.Toast
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
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

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
        adapter = ReservationAdapter({ row -> cancel(row) }, { row -> modify(row) })
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
                val syncedAt = cachedEntities.maxOf { it.cachedAt }
                binding.reservationSync.text = "Offline cache · last synced ${SimpleDateFormat("h:mm a", Locale.getDefault()).format(Date(syncedAt))}"
                render()
            }
            try {
                val response = RetrofitClient.apiService.getReservations()
                if (response.isSuccessful && response.body()?.success == true) {
                    reservations = response.body()?.data.orEmpty(); db.reservationDao().replaceAll(reservations.map { com.smartmicrogrid.data.local.ReservationEntity.fromDomain(it) })
                    binding.reservationSync.text = "Synced just now"; render()
                    val summary = RetrofitClient.apiService.getReservationSummary().body()?.data
                    summary?.let { binding.reservationCount.text = "${it.pendingCount} pending · ${it.approvedFutureCount} approved upcoming · ${"%.1f".format(it.totalEnergyReserved)} kWh reserved" }
                } else binding.reservationSync.text = "Offline · showing saved reservations"
            } catch (_: Exception) { binding.reservationSync.text = "Offline · showing saved reservations" }
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
            category && (statusFilter == "All statuses" || row.status.equals(statusFilter, true)) && (date.isBlank() || ReservationTime.localDate(row.startTime) == date) && (q.isBlank() || row.energySlotId.contains(q, true) || row.microgridNodeId.contains(q, true) || row.status.contains(q, true))
        }
        adapter.submit(filtered); binding.reservationEmpty.visibility = if (filtered.isEmpty()) View.VISIBLE else View.GONE
        binding.reservationCount.text = "${reservations.count { it.status == "Pending" }} pending · ${reservations.count { it.status == "Approved" }} approved"
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
