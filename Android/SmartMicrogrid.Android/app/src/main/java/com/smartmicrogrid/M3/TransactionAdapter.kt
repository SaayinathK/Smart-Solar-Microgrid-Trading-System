package com.smartmicrogrid.M3

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.smartmicrogrid.R
import com.smartmicrogrid.models.Transaction

class TransactionAdapter(
    private var items: List<Transaction>,
    private val onItemClick: (Transaction) -> Unit
) : RecyclerView.Adapter<TransactionAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val transactionCode: TextView = view.findViewById(R.id.tv_transaction_code)
        val status: TextView = view.findViewById(R.id.tv_status)
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
        holder.status.text = transaction.status
        holder.amount.text = "Energy: ${transaction.energyAmount} kWh"
        holder.context.text = "Microgrid: ${transaction.microgridNodeId}  |  Slot: ${transaction.energySlotId}"
        holder.createdAt.text = "Created: ${transaction.createdAt}"
        holder.itemView.setOnClickListener { onItemClick(transaction) }
    }

    override fun getItemCount(): Int = items.size

    fun updateData(newItems: List<Transaction>) {
        items = newItems
        notifyDataSetChanged()
    }
}