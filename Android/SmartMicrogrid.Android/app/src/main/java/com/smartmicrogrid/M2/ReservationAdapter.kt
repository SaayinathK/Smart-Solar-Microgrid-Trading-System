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
        val role = SessionManager.getUserRole()
        val isStaff = role in listOf("MicrogridOperator", "Admin")
        val canVerifyTransfer = role == "MicrogridOperator" || role == "Admin"
        val isProsumer = role == "Prosumer" || role == "Admin"

        val vCode = item.verificationCode?.ifBlank { null }
            ?: "SMG-RES-${if (item.id.length >= 6) item.id.takeLast(6).uppercase() else "000000"}"

        holder.id.text = vCode
        holder.status.text = item.status

        // Status color styling
        val statusColor = when (item.status.lowercase()) {
            "approved" -> 0xFF059669.toInt()
            "pending" -> 0xFFD97706.toInt()
            "completed" -> 0xFF2563EB.toInt()
            "rejected", "cancelled" -> 0xFFDC2626.toInt()
            else -> 0xFF64748B.toInt()
        }
        holder.status.setTextColor(statusColor)

        // Prosumer & Microgrid names
        val pName = if (!item.prosumerName.isNullOrBlank()) item.prosumerName else "Prosumer"
        holder.prosumer.text = "$pName · NIC: ${item.prosumerId.ifBlank { "—" }}"

        val mName = if (!item.microgridName.isNullOrBlank()) item.microgridName else "Microgrid Hub"
        holder.microgrid.text = "$mName (${item.microgridNodeId.takeLast(6)})"

        // Energy & cost
        holder.amount.text = "%.1f kWh".format(item.energyAmount)
        val totalCost = if (item.totalEstimatedCost > 0) item.totalEstimatedCost else (item.energyAmount * item.pricePerUnit)
        holder.cost.text = if (totalCost > 0) "Est. $%.2f".format(totalCost) else ""

        // Schedule
        holder.time.text = "${ReservationTime.display(item.startTime)} – ${ReservationTime.display(item.endTime)}"

        // Digital Pass action
        holder.pass.setOnClickListener { onPass?.invoke(item) }

        // Operator approve/reject
        if (isStaff && item.status == "Pending") {
            holder.approve.visibility = View.VISIBLE
            holder.approve.setOnClickListener { onApprove?.invoke(item) }
            holder.reject.visibility = View.VISIBLE
            holder.reject.setOnClickListener { onReject?.invoke(item) }
        } else {
            holder.approve.visibility = View.GONE
            holder.reject.visibility = View.GONE
        }

        // Grid operator verifies the QR/pass data and completes the transfer.
        if (canVerifyTransfer && item.status == "Approved") {
            holder.complete.visibility = View.VISIBLE
            holder.complete.setOnClickListener { onComplete?.invoke(item) }
        } else {
            holder.complete.visibility = View.GONE
        }

        // Prosumer cancel/modify
        val canModifyOrCancel = isProsumer && (item.status == "Pending" || item.status == "Approved")
        holder.modify.visibility = if (canModifyOrCancel) View.VISIBLE else View.GONE
        holder.modify.setOnClickListener { onModify(item) }

        holder.cancel.visibility = if (canModifyOrCancel) View.VISIBLE else View.GONE
        holder.cancel.setOnClickListener { onCancel(item) }
    }

    fun submit(items: List<Reservation>) {
        this.items = items
        notifyDataSetChanged()
    }
}
