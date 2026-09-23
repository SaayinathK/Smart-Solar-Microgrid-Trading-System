package com.smartmicrogrid.M1.microgrid

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.smartmicrogrid.R
import com.smartmicrogrid.models.Microgrid

class MicrogridAdapter(
    private var items: List<Microgrid>,
    private val onItemClick: (Microgrid) -> Unit
) : RecyclerView.Adapter<MicrogridAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val nameTv: TextView = view.findViewById(R.id.tv_name)
        val locationTv: TextView = view.findViewById(R.id.tv_location)
        val capacityTv: TextView = view.findViewById(R.id.tv_capacity)
        val statusTv: TextView = view.findViewById(R.id.tv_status)
        val batteryTv: TextView = view.findViewById(R.id.tv_battery)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_microgrid, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val m = items[position]
        holder.nameTv.text = m.name
        holder.locationTv.text = m.location
        holder.capacityTv.text = "Capacity: ${m.capacity} kWh (Avail: ${m.availableCapacity} kWh)"
        holder.statusTv.text = m.status
        holder.batteryTv.text = "Battery: ${m.batteryPercentage.toInt()}% (${m.currentBatteryLevel} kWh)"

        holder.itemView.setOnClickListener { onItemClick(m) }
    }

    override fun getItemCount() = items.size

    fun updateData(newItems: List<Microgrid>) {
        items = newItems
        notifyDataSetChanged()
    }
}
