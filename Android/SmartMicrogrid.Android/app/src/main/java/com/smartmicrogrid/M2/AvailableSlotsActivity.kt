package com.smartmicrogrid.M2

import android.os.Bundle
import android.text.InputType
import android.text.TextWatcher
import android.text.Editable
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.button.MaterialButton
import com.smartmicrogrid.R
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.CreateReservationRequest
import com.smartmicrogrid.models.EnergyAvailabilitySlot
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.temporal.ChronoUnit

class AvailableSlotsActivity : AppCompatActivity() {
    private lateinit var adapter: AvailableSlotsAdapter
    private var allSlots = emptyList<EnergyAvailabilitySlot>()
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        SessionManager.init(this)
        if (!SessionManager.isLoggedIn() || SessionManager.getUserRole() != "Prosumer") {
            Toast.makeText(this, "Sign in as a prosumer to reserve energy.", Toast.LENGTH_LONG).show()
            finish()
            return
        }
        setContentView(R.layout.activity_m2_available_slots)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.title = "Available energy"
        adapter = AvailableSlotsAdapter { reserve(it) }
        findViewById<RecyclerView>(R.id.m2_slots_list).layoutManager = LinearLayoutManager(this)
        findViewById<RecyclerView>(R.id.m2_slots_list).adapter = adapter
        findViewById<View>(R.id.m2_slots_refresh).setOnClickListener { loadSlots() }
        val filterWatcher = object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) { renderSlots() }
            override fun afterTextChanged(s: Editable?) {}
        }
        findViewById<EditText>(R.id.m2_slots_location_filter).addTextChangedListener(filterWatcher)
        findViewById<EditText>(R.id.m2_slots_minimum_filter).addTextChangedListener(filterWatcher)
        findViewById<EditText>(R.id.m2_slots_date_filter).addTextChangedListener(filterWatcher)
        loadSlots()
    }
    override fun onSupportNavigateUp(): Boolean { finish(); return true }
    private fun loadSlots() {
        lifecycleScope.launch {
            findViewById<View>(R.id.m2_slots_loading).visibility = View.VISIBLE
            try {
                val result = RetrofitClient.apiService.getEnergyAvailability(microgridId = intent.getStringExtra("MICROGRID_ID"))
                if (!result.isSuccessful || result.body()?.success != true) throw Exception(result.body()?.message ?: "Unable to load energy slots")
                val now = Instant.now(); val latest = now.plus(7, ChronoUnit.DAYS)
                allSlots = result.body()?.data.orEmpty().filter { slot ->
                    val start = runCatching { Instant.parse(slot.startTime) }.getOrNull()
                    slot.availableAmount >= 0.1 && start != null && start.isAfter(now) && !start.isAfter(latest)
                }
                renderSlots()
            } catch (e: Exception) {
                allSlots = emptyList(); adapter.submit(emptyList())
                findViewById<TextView>(R.id.m2_slots_empty).text = e.message ?: "Unable to load available energy. Refresh to retry."
                findViewById<TextView>(R.id.m2_slots_empty).visibility = View.VISIBLE
            }
            finally { findViewById<View>(R.id.m2_slots_loading).visibility = View.GONE }
        }
    }
    private fun renderSlots() {
        val location = findViewById<EditText>(R.id.m2_slots_location_filter).text.toString().trim()
        val minEnergy = findViewById<EditText>(R.id.m2_slots_minimum_filter).text.toString().toDoubleOrNull() ?: 0.0
        val date = findViewById<EditText>(R.id.m2_slots_date_filter).text.toString().trim()
        val filtered = allSlots.filter { slot ->
            (location.isBlank() || slot.microgridName.contains(location, true) || slot.location.contains(location, true)) && slot.availableAmount >= minEnergy && (date.isBlank() || ReservationTime.localDate(slot.startTime) == date)
        }
        adapter.submit(filtered)
        val empty = findViewById<TextView>(R.id.m2_slots_empty)
        empty.text = if (allSlots.isEmpty()) "No future slots with available energy within seven days." else "No available slots match these filters."
        empty.visibility = if (filtered.isEmpty()) View.VISIBLE else View.GONE
    }
    private fun reserve(slot: EnergyAvailabilitySlot) {
        val input = EditText(this).apply { inputType = InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_FLAG_DECIMAL; hint = "Up to ${slot.availableAmount} kWh"; setText(minOf(5.0, slot.availableAmount).toString()) }
        AlertDialog.Builder(this).setTitle("Review energy reservation")
            .setMessage("${slot.microgridName} · ${slot.location}\n${ReservationTime.display(slot.startTime)} – ${ReservationTime.display(slot.endTime)}\nRs ${slot.pricePerUnit} per kWh")
            .setView(input).setNegativeButton("Back", null).setPositiveButton("Submit request") { _, _ ->
                val amount = input.text.toString().toDoubleOrNull()
                        if (amount == null || amount < 0.1 || amount > slot.availableAmount) { Toast.makeText(this, "Enter an amount between 0.1 kWh and the available capacity", Toast.LENGTH_LONG).show(); return@setPositiveButton }
                lifecycleScope.launch {
                    try {
                        val response = RetrofitClient.apiService.createReservation(CreateReservationRequest(slot.energySlotId, amount))
                        val created = response.body()?.data
                        if (!response.isSuccessful || response.body()?.success != true || created == null) throw Exception(response.body()?.message ?: "Reservation could not be created")
                        AlertDialog.Builder(this@AvailableSlotsActivity).setTitle("Reservation summary")
                            .setMessage("Request submitted\n${slot.microgridName}\n$amount kWh · Rs ${"%.2f".format(amount * slot.pricePerUnit)} estimated\n${ReservationTime.display(slot.startTime)} – ${ReservationTime.display(slot.endTime)}\nStatus: ${created.status}")
                            .setPositiveButton("Done") { _, _ -> loadSlots() }.show()
                    } catch (e: Exception) { Toast.makeText(this@AvailableSlotsActivity, e.message ?: "Reservation failed", Toast.LENGTH_LONG).show() }
                }
            }.show()
    }
}

private class AvailableSlotsAdapter(private val onReserve: (EnergyAvailabilitySlot) -> Unit) : RecyclerView.Adapter<AvailableSlotsAdapter.Holder>() {
    private var rows = emptyList<EnergyAvailabilitySlot>()
    class Holder(view: View) : RecyclerView.ViewHolder(view) {
        val name: TextView = view.findViewById(R.id.m2_slot_name)
        val location: TextView = view.findViewById(R.id.m2_slot_location)
        val energy: TextView = view.findViewById(R.id.m2_slot_energy)
        val time: TextView = view.findViewById(R.id.m2_slot_time)
        val price: TextView = view.findViewById(R.id.m2_slot_price)
        val action: MaterialButton = view.findViewById(R.id.m2_slot_reserve)
    }
    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) = Holder(LayoutInflater.from(parent.context).inflate(R.layout.item_m2_energy_slot, parent, false))
    override fun getItemCount() = rows.size
    override fun onBindViewHolder(holder: Holder, position: Int) {
        val row = rows[position]
        holder.name.text = row.microgridName; holder.location.text = row.location
        holder.energy.text = "${"%.1f".format(row.availableAmount)} kWh available"
        holder.time.text = "${ReservationTime.display(row.startTime)} – ${ReservationTime.display(row.endTime)}"
        holder.price.text = "Rs ${row.pricePerUnit} / kWh"
        holder.action.setOnClickListener { onReserve(row) }
    }
    fun submit(rows: List<EnergyAvailabilitySlot>) { this.rows = rows; notifyDataSetChanged() }
}
