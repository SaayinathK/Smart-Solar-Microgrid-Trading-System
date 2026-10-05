package com.smartmicrogrid.M3

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import android.content.res.ColorStateList
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.chip.Chip
import com.smartmicrogrid.R
import com.smartmicrogrid.models.Transaction

class TransactionAdapter(
    private var items: List<Transaction>,
    private val onItemClick: (Transaction) -> Unit
) : RecyclerView.Adapter<TransactionAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val transactionCode: TextView = view.findViewById(R.id.tv_transaction_code)
        val status: Chip = view.findViewById(R.id.tv_status)
        val amount: TextView = view.findViewById(R.id.tv_amount)
        val context: TextView = view.findViewById(R.id.tv_context)
        val createdAt: TextView = view.findViewById(R.id.tv_created_at)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_transaction, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val transaction = items[position]
        holder.transactionCode.text = transaction.transactionCode.ifBlank {
            "Transaction ${transaction.id.takeLast(8)}"
        }
        holder.status.text = TransactionUiFormatters.statusLabel(transaction.status)
        val (statusBackground, statusForeground) = when (transaction.status.lowercase()) {
            "completed", "verified" -> R.color.m3_status_success_background to R.color.m3_status_success_foreground
            "rejected", "cancelled" -> R.color.m3_status_error_background to R.color.m3_status_error_foreground
            "pending" -> R.color.m3_status_warning_background to R.color.m3_status_warning_foreground
            else -> R.color.m3_status_info_background to R.color.m3_status_info_foreground
        }
        holder.status.chipBackgroundColor = ColorStateList.valueOf(
            holder.itemView.context.getColor(statusBackground)
        )
        holder.status.setTextColor(holder.itemView.context.getColor(statusForeground))
        holder.amount.text = "Energy: ${transaction.energyAmount} kWh"
        holder.context.text = "Microgrid: ${transaction.microgridNodeId}  |  Slot: ${transaction.energySlotId}"
        holder.createdAt.text = "Created: ${TransactionUiFormatters.dateTime(transaction.createdAt)}"
        holder.itemView.setOnClickListener { onItemClick(transaction) }
    }

    override fun getItemCount(): Int = items.size

    fun updateData(newItems: List<Transaction>) {
        items = newItems
        notifyDataSetChanged()
    }
}
