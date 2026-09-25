package com.smartmicrogrid.M2

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.smartmicrogrid.R
import com.smartmicrogrid.models.Reservation
import com.smartmicrogrid.utils.SessionManager

class ReservationAdapter(
    private val onCancel: (Reservation) -> Unit,
    private val onModify: (Reservation) -> Unit,
    private val onApprove: ((Reservation) -> Unit)? = null,
    private val onReject: ((Reservation) -> Unit)? = null,
    private val onComplete: ((Reservation) -> Unit)? = null,
    private val onPass: ((Reservation) -> Unit)? = null
) : RecyclerView.Adapter<ReservationAdapter.Holder>() {

class ReservationAdapter(private val onCancel: (Reservation) -> Unit, private val onModify: (Reservation) -> Unit, private val onDetails: (Reservation) -> Unit = {}) : RecyclerView.Adapter<ReservationAdapter.Holder>() {
    private var items: List<Reservation> = emptyList()

    class Holder(view: View) : RecyclerView.ViewHolder(view) {
        val id: TextView = view.findViewById(R.id.reservation_id)
        val status: TextView = view.findViewById(R.id.reservation_status)
        val prosumer: TextView = view.findViewById(R.id.reservation_prosumer)
        val microgrid: TextView = view.findViewById(R.id.reservation_microgrid)
        val amount: TextView = view.findViewById(R.id.reservation_amount)
        val cost: TextView = view.findViewById(R.id.reservation_cost)
        val time: TextView = view.findViewById(R.id.reservation_time)
        val pass: Button = view.findViewById(R.id.reservation_pass)
        val approve: Button = view.findViewById(R.id.reservation_approve)
        val reject: Button = view.findViewById(R.id.reservation_reject)
        val complete: Button = view.findViewById(R.id.reservation_complete)
        val cancel: Button = view.findViewById(R.id.reservation_cancel)
        val modify: Button = view.findViewById(R.id.reservation_modify)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        Holder(LayoutInflater.from(parent.context).inflate(R.layout.item_reservation, parent, false))

    override fun getItemCount() = items.size

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = items[position]
        holder.id.text = "${item.microgridName.ifBlank { "Reservation ${item.id.takeLast(8)}" }} · slot ${item.energySlotId.takeLast(6)}"
        holder.amount.text = if (item.totalCost > 0) "%.1f kWh · Rs %.2f".format(item.energyAmount, item.totalCost) else "%.1f kWh".format(item.energyAmount)
        val locked = item.status in listOf("Pending", "Approved") && !ReservationTime.hasTwelveHourNotice(item.startTime)
        holder.time.text = "${item.location.takeIf { it.isNotBlank() }?.plus(" · ").orEmpty()}${ReservationTime.display(item.startTime)} – ${ReservationTime.display(item.endTime)}${if (locked) " · Changes close 12h before start" else ""}"
        holder.status.text = item.status
        holder.itemView.setOnClickListener { onDetails(item) }
        val canChange = item.status in listOf("Pending", "Approved") && !locked
        holder.cancel.visibility = if (canChange) View.VISIBLE else View.GONE
        holder.cancel.setOnClickListener { onCancel(item) }
        holder.modify.visibility = if (canChange) View.VISIBLE else View.GONE
        holder.modify.setOnClickListener { onModify(item) }

        holder.cancel.visibility = if (canModifyOrCancel) View.VISIBLE else View.GONE
        holder.cancel.setOnClickListener { onCancel(item) }
    }

    fun submit(items: List<Reservation>) {
        this.items = items
        notifyDataSetChanged()
    }
}
