package com.smartmicrogrid.ui

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import com.smartmicrogrid.R
import com.smartmicrogrid.M2.AvailableSlotsActivity
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.launch

class HomeFragment : Fragment() {

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        val view = inflater.inflate(R.layout.fragment_home, container, false)
        
        return view
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val user = SessionManager.getUser()
        val nameView = view.findViewById<TextView>(R.id.tv_greeting_name)
        if (user != null) {
            nameView.text = "${user.firstName}!"
        }

        view.findViewById<View>(R.id.home_book_energy).setOnClickListener {
            startActivity(android.content.Intent(requireContext(), AvailableSlotsActivity::class.java))
        }
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val summaryResponse = RetrofitClient.apiService.getReservationSummary()
                if (summaryResponse.isSuccessful && summaryResponse.body()?.success == true) {
                    val summary = summaryResponse.body()?.data
                    view.findViewById<TextView>(R.id.home_pending_count).text = summary?.pendingCount?.toString() ?: "0"
                    view.findViewById<TextView>(R.id.home_upcoming_count).text = summary?.approvedFutureCount?.toString() ?: "0"
                }
                val reservationsResponse = RetrofitClient.apiService.getReservations()
                val rows = if (reservationsResponse.isSuccessful && reservationsResponse.body()?.success == true) reservationsResponse.body()?.data.orEmpty() else emptyList()
                val next = rows.filter { it.status == "Approved" && runCatching { java.time.Instant.parse(it.startTime).isAfter(java.time.Instant.now()) }.getOrDefault(false) }
                    .minByOrNull { it.startTime }
                view.findViewById<TextView>(R.id.home_next_reservation).text = next?.let { "${it.microgridName.ifBlank { "Microgrid" }} · ${it.energyAmount} kWh" } ?: "No upcoming approved reservation"
                view.findViewById<TextView>(R.id.home_next_reservation_time).text = next?.let { "${com.smartmicrogrid.M2.ReservationTime.display(it.startTime)} – ${com.smartmicrogrid.M2.ReservationTime.display(it.endTime)}" }.orEmpty()
            } catch (_: Exception) {
                view.findViewById<TextView>(R.id.home_next_reservation).text = "Reservation summary unavailable"
            }
        }
        
    }
}
