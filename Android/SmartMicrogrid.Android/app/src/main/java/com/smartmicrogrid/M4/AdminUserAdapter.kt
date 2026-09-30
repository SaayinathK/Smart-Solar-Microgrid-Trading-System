package com.smartmicrogrid.M4

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.smartmicrogrid.R
import com.smartmicrogrid.models.User

/**
 * Renders the user accounts an administrator can act on. The two buttons change
 * meaning with the current status: Activate appears whenever the account is not
 * active, and Suspend only appears for an account that is currently active.
 */
class AdminUserAdapter(
    private val onActivate: (User) -> Unit,
    private val onSuspend: (User) -> Unit
) : RecyclerView.Adapter<AdminUserAdapter.Holder>() {

    private var items: List<User> = emptyList()

    class Holder(view: View) : RecyclerView.ViewHolder(view) {
        val name: TextView = view.findViewById(R.id.m4_user_name)
        val email: TextView = view.findViewById(R.id.m4_user_email)
        val role: TextView = view.findViewById(R.id.m4_user_role)
        val status: TextView = view.findViewById(R.id.m4_user_status)
        val activate: TextView = view.findViewById(R.id.m4_user_activate)
        val suspend: TextView = view.findViewById(R.id.m4_user_suspend)
    }

    fun submit(newItems: List<User>) {
        items = newItems
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        Holder(LayoutInflater.from(parent.context).inflate(R.layout.item_m4_user, parent, false))

    override fun getItemCount() = items.size

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val user = items[position]
        val context = holder.itemView.context

        holder.name.text = "${user.firstName} ${user.lastName}".trim()
        holder.email.text = user.email
        holder.role.text = user.role

        val status = user.accountStatus.ifBlank {
            if (user.isActive) "Active" else "Inactive"
        }
        holder.status.text = status
        holder.status.setBackgroundColor(
            androidx.core.content.ContextCompat.getColor(
                context,
                when (status) {
                    "Active" -> R.color.m4_status_active_bg
                    "Suspended" -> R.color.m4_status_suspended_bg
                    "Pending" -> R.color.m4_status_pending_bg
                    else -> R.color.m4_status_inactive_bg
                }
            )
        )

        holder.activate.visibility = if (user.isActive) View.GONE else View.VISIBLE
        holder.activate.setOnClickListener { onActivate(user) }

        holder.suspend.visibility = if (user.isActive) View.VISIBLE else View.GONE
        holder.suspend.setOnClickListener { onSuspend(user) }
    }
}
