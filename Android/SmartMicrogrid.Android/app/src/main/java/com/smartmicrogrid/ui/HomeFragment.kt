package com.smartmicrogrid.ui

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import android.widget.LinearLayout
import android.widget.HorizontalScrollView
import android.widget.Button
import android.widget.FrameLayout
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import com.smartmicrogrid.M2.ReservationsActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.Microgrid
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.launch
import kotlinx.coroutines.CancellationException

class HomeFragment : Fragment() {

    private var map: OpenStreetMapView? = null
    private var nodes = emptyList<Microgrid>()
    private var selectedNodeId: String? = null

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        val view = inflater.inflate(R.layout.fragment_home, container, false)
        
        // Personalize the greeting
        val user = SessionManager.getUser()
        val nameView = view.findViewById<TextView>(R.id.tv_greeting_name)
        if (user != null) {
            nameView.text = getString(R.string.solar_greeting, user.firstName)
        }

        view.findViewById<View>(R.id.btn_view_bookings).setOnClickListener {
            startActivity(android.content.Intent(requireContext(), ReservationsActivity::class.java))
        }

        return view
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        selectedNodeId = savedInstanceState?.getString("selected_grid_id") ?: selectedNodeId
        map = OpenStreetMapView(requireContext()).also {
            view.findViewById<FrameLayout>(R.id.map_container).addView(it,
                FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT))
        }

        loadDashboard(view)
        loadNodes(view)
    }

    private fun loadDashboard(view: View) {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val summaryResponse = RetrofitClient.apiService.getReservationSummary()
                val reservationsResponse = RetrofitClient.apiService.getReservations(pageSize = 100)
                val summary = summaryResponse.body()?.data
                val reservations = reservationsResponse.body()?.data.orEmpty()

                view.findViewById<TextView>(R.id.tv_pending_count).text = (summary?.pendingCount ?: reservations.count { it.status.equals("Pending", true) }).toString()
                view.findViewById<TextView>(R.id.tv_active_count).text = (summary?.approvedFutureCount ?: reservations.count { it.status.equals("Approved", true) }).toString()
                view.findViewById<TextView>(R.id.tv_booking_summary).text = "${reservations.size} bookings · ${summary?.completedThisMonthCount ?: reservations.count { it.status.equals("Completed", true) }} completed this month"
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                view.findViewById<TextView>(R.id.tv_booking_summary).text = error.message ?: "Unable to load bookings"
            }
        }
    }

    private fun loadNodes(view: View) {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val response = RetrofitClient.apiService.getMicrogrids(status = "Active", isActive = true)
                check(response.isSuccessful && response.body()?.success == true) { "Unable to load microgrids" }
                nodes = response.body()?.data.orEmpty()
                view.findViewById<TextView>(R.id.tv_node_count).text = getString(R.string.grid_map_node_count, nodes.size)
                selectedNodeId = nodes.find { it.id == selectedNodeId }?.id
                    ?: nodes.firstOrNull { hasLocation(it) }?.id ?: nodes.firstOrNull()?.id
                renderNodeList(view)
                if (nodes.isEmpty()) {
                    showMapMessage(view, R.string.grid_map_empty)
                    view.findViewById<TextView>(R.id.tv_map_hint).setText(R.string.grid_map_hint)
                }
                renderSelectedMarker()
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                nodes = emptyList()
                map?.clearLocation()
                view.findViewById<TextView>(R.id.tv_node_count).setText(R.string.grid_map_error)
                showMapMessage(view, R.string.grid_map_error)
                view.findViewById<TextView>(R.id.tv_map_hint).setText(R.string.grid_map_hint)
                view.findViewById<LinearLayout>(R.id.grid_node_list).apply {
                    removeAllViews()
                    addView(Button(context).apply {
                        setText(R.string.grid_map_retry)
                        setOnClickListener { isEnabled = false; loadNodes(view) }
                    })
                }
            }
        }
    }

    private fun renderNodeList(view: View) {
        val list = view.findViewById<LinearLayout>(R.id.grid_node_list)
        list.removeAllViews()
        nodes.forEach { node ->
            val row = layoutInflater.inflate(R.layout.item_dashboard_microgrid, list, false)
            row.tag = node.id
            row.isSelected = node.id == selectedNodeId
            row.findViewById<TextView>(R.id.tv_grid_name).text = node.name
            row.findViewById<TextView>(R.id.tv_grid_location).text =
                if (hasLocation(node)) node.location else getString(R.string.grid_map_missing_location)
            row.contentDescription = getString(R.string.grid_map_selection, node.name, node.location)
            row.setOnClickListener {
                selectedNodeId = node.id
                for (index in 0 until list.childCount) {
                    list.getChildAt(index).isSelected = list.getChildAt(index).tag == node.id
                }
                renderSelectedMarker()
            }
            list.addView(row)
        }
        list.post {
            val selected = (0 until list.childCount).map { list.getChildAt(it) }.firstOrNull { it.isSelected }
            if (selected != null) view.findViewById<HorizontalScrollView>(R.id.grid_node_scroll).smoothScrollTo(selected.left, 0)
        }
    }

    private fun hasLocation(node: Microgrid): Boolean =
        node.latitude.isFinite() && node.longitude.isFinite()
            && node.latitude in -90.0..90.0 && node.longitude in -180.0..180.0
            && !(node.latitude == 0.0 && node.longitude == 0.0)

    private fun showMapMessage(view: View, message: Int) {
        view.findViewById<TextView>(R.id.tv_map_message).apply {
            setText(message)
            visibility = View.VISIBLE
        }
    }

    private fun renderSelectedMarker() {
        val view = view ?: return
        val node = nodes.find { it.id == selectedNodeId }
        map?.clearLocation()
        if (node == null) return
        view.findViewById<TextView>(R.id.tv_map_hint).text = getString(R.string.grid_map_selection, node.name, node.location)
        if (!hasLocation(node)) {
            showMapMessage(view, R.string.grid_map_unavailable)
            return
        }
        val mapView = map ?: return
        view.findViewById<View>(R.id.tv_map_message).visibility = View.GONE
        mapView.showLocation(node.latitude, node.longitude, node.name)
    }

    override fun onSaveInstanceState(outState: Bundle) {
        outState.putString("selected_grid_id", selectedNodeId)
        super.onSaveInstanceState(outState)
    }

    override fun onDestroyView() {
        map?.let {
            (it.parent as? ViewGroup)?.removeView(it)
            it.stopLoading()
            it.destroy()
        }
        map = null
        super.onDestroyView()
    }

    override fun onResume() { super.onResume(); map?.onResume() }
    override fun onPause() { map?.onPause(); super.onPause() }
}
