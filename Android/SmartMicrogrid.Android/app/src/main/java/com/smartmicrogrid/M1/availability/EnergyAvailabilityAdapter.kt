package com.smartmicrogrid.M1.availability

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.progressindicator.LinearProgressIndicator
import com.smartmicrogrid.R
import com.smartmicrogrid.models.EnergyAvailabilitySlot

class EnergyAvailabilityAdapter(
    private var items: List<EnergyAvailabilitySlot>
) : RecyclerView.Adapter<EnergyAvailabilityAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val nameTv: TextView = view.findViewById(R.id.tv_hub_name)
        val locationTv: TextView = view.findViewById(R.id.tv_location)
        val energyTv: TextView = view.findViewById(R.id.tv_available_energy)
        val timeTv: TextView = view.findViewById(R.id.tv_time_window)
        val priceTv: TextView = view.findViewById(R.id.tv_price)
        val remainingTv: TextView = view.findViewById(R.id.tv_remaining)
        val availabilityProgress: LinearProgressIndicator = view.findViewById(R.id.progress_availability)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_energy_slot, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val s = items[position]
        holder.nameTv.text = s.microgridName
        holder.locationTv.text = s.location
        holder.energyTv.text = "${format(s.availableAmount)} kWh"
        val remaining = if (s.energyAmount > 0) ((s.availableAmount / s.energyAmount) * 100).toInt().coerceIn(0, 100) else 0
        holder.remainingTv.text = "$remaining%"
        holder.availabilityProgress.progress = remaining
        holder.timeTv.text = "Delivery: ${formatDate(s.startTime)} to ${formatDate(s.endTime)}"
        holder.priceTv.text = "$${format(s.pricePerUnit)} / kWh"
    }

    override fun getItemCount() = items.size

    fun updateData(newItems: List<EnergyAvailabilitySlot>) {
        items = newItems
        notifyDataSetChanged()
    }

    private fun format(value: Double) = String.format(java.util.Locale.getDefault(), "%.1f", value)

    private fun formatDate(value: String): String = value.replace('T', ' ').take(16)
}
