package com.smartmicrogrid.M1.availability

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
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
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_energy_slot, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val s = items[position]
        holder.nameTv.text = s.microgridName
        holder.locationTv.text = s.location
        holder.energyTv.text = "Available: ${s.availableAmount} kWh (Total: ${s.energyAmount} kWh)"
        holder.timeTv.text = "Start: ${s.startTime.take(16).replace('T', ' ')}"
        holder.priceTv.text = "$${s.pricePerUnit} / kWh"
    }

    override fun getItemCount() = items.size

    fun updateData(newItems: List<EnergyAvailabilitySlot>) {
        items = newItems
        notifyDataSetChanged()
    }
}
