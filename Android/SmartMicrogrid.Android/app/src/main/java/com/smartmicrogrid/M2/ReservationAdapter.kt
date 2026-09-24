package com.smartmicrogrid.M2

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.smartmicrogrid.R
import com.smartmicrogrid.models.Reservation

class ReservationAdapter(private val onCancel: (Reservation) -> Unit, private val onModify: (Reservation) -> Unit) : RecyclerView.Adapter<ReservationAdapter.Holder>() {
    private var items: List<Reservation> = emptyList()
    class Holder(view: View) : RecyclerView.ViewHolder(view) {
        val id: TextView = view.findViewById(R.id.reservation_id)
        val amount: TextView = view.findViewById(R.id.reservation_amount)
        val time: TextView = view.findViewById(R.id.reservation_time)
        val status: TextView = view.findViewById(R.id.reservation_status)
        val cancel: Button = view.findViewById(R.id.reservation_cancel)
        val modify: Button = view.findViewById(R.id.reservation_modify)
    }
    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) = Holder(LayoutInflater.from(parent.context).inflate(R.layout.item_reservation, parent, false))
    override fun getItemCount() = items.size
    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = items[position]
        holder.id.text = "Reservation ${item.id.takeLast(8)} · slot ${item.energySlotId.takeLast(6)}"
        holder.amount.text = "%.1f kWh".format(item.energyAmount)
        holder.time.text = "${ReservationTime.display(item.startTime)} – ${ReservationTime.display(item.endTime)}"
        holder.status.text = item.status
        holder.cancel.visibility = if (item.status == "Pending" || item.status == "Approved") View.VISIBLE else View.GONE
        holder.cancel.setOnClickListener { onCancel(item) }
        holder.modify.visibility = holder.cancel.visibility
        holder.modify.setOnClickListener { onModify(item) }
    }
    fun submit(items: List<Reservation>) { this.items = items; notifyDataSetChanged() }
}
