package com.smartmicrogrid.M3

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.smartmicrogrid.databinding.ItemApprovedReservationBinding
import com.smartmicrogrid.models.Reservation

class ApprovedReservationAdapter(
    private val onCreateTransaction: (Reservation) -> Unit
) : RecyclerView.Adapter<ApprovedReservationAdapter.ViewHolder>() {

    private var reservations: List<Reservation> = emptyList()
    private var createEnabled = true

    class ViewHolder(val binding: ItemApprovedReservationBinding) :
        RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val binding = ItemApprovedReservationBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return ViewHolder(binding)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val reservation = reservations[position]
        val context = holder.binding.root.context
        holder.binding.tvReservationId.text = "Reservation ID: ${reservation.id}"
        holder.binding.tvProsumer.text = "Prosumer: ${reservation.prosumerName?.takeIf(String::isNotBlank) ?: reservation.prosumerId}"
        holder.binding.tvEnergyAmount.text = "${reservation.energyAmount} kWh"
        holder.binding.tvMicrogrid.text = "Microgrid: ${reservation.microgridName?.takeIf(String::isNotBlank) ?: reservation.microgridNodeId}"
        val date = TransactionUiFormatters.dateTime(reservation.reservationDate)
        val start = TransactionUiFormatters.dateTime(reservation.startTime)
        val end = TransactionUiFormatters.dateTime(reservation.endTime)
        holder.binding.tvScheduledTime.text = "Scheduled: ${listOf(date.takeUnless { it == "Not available" }, "$start – $end").filterNotNull().joinToString(" · ")}"
        holder.binding.tvStatus.text = reservation.status.uppercase()
        holder.binding.btnCreateTransaction.isEnabled = createEnabled
        holder.binding.btnCreateTransaction.text = if (createEnabled) {
            context.getString(com.smartmicrogrid.R.string.create_transaction)
        } else {
            "Creating…"
        }
        holder.binding.btnCreateTransaction.setOnClickListener {
            if (createEnabled) onCreateTransaction(reservation)
        }
    }

    override fun getItemCount(): Int = reservations.size

    fun submit(items: List<Reservation>) {
        reservations = items
        notifyDataSetChanged()
    }

    fun setCreateEnabled(enabled: Boolean) {
        createEnabled = enabled
        notifyDataSetChanged()
    }
}
