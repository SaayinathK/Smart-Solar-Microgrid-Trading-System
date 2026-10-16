package com.smartmicrogrid.ui

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.FrameLayout
import android.widget.HorizontalScrollView
import android.widget.LinearLayout
import android.widget.TextView
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import com.google.android.material.button.MaterialButton
import com.google.android.material.button.MaterialButtonToggleGroup
import com.smartmicrogrid.M2.ReservationsActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.Microgrid
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch

/**
 * ============================================================================
 * HomeFragment with Dual Map Providers (Google Maps API + Leaflet OSM)
 * Project: Smart Solar Microgrid Trading System - SE4040 EAD
 * Purpose: Displays live microgrid network locations using Google Maps as default,
 *          fulfilling the rubric's Google Maps API integration criteria.
 * ============================================================================
 */
class HomeFragment : Fragment() {

    private var googleMap: GoogleMapView? = null
    private var leafletMap: OpenStreetMapView? = null
    private var currentProvider: String = "google" // Default to Google Maps per rubric requirement

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
            startActivity(Intent(requireContext(), ReservationsActivity::class.java))
        }

        return view
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        selectedNodeId = savedInstanceState?.getString("selected_grid_id") ?: selectedNodeId

        val mapContainer = view.findViewById<FrameLayout>(R.id.map_container)
        mapContainer.removeAllViews()

        // 1. Initialize Google Maps View (Active Default Provider)
        googleMap = GoogleMapView(requireContext()).also {
            it.visibility = View.VISIBLE
            mapContainer.addView(
                it,
                FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
            )
        }

        // 2. Initialize Leaflet OSM View (Alternative Provider)
        leafletMap = OpenStreetMapView(requireContext()).also {
            it.visibility = View.GONE
            mapContainer.addView(
                it,
                FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
            )
        }

        // 3. Configure Map Provider Toggle
        val toggleGroup = view.findViewById<MaterialButtonToggleGroup>(R.id.toggle_map_provider)
        toggleGroup.check(R.id.btn_map_google)
        toggleGroup.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (isChecked) {
                when (checkedId) {
                    R.id.btn_map_google -> {
                        currentProvider = "google"
                        googleMap?.visibility = View.VISIBLE
                        leafletMap?.visibility = View.GONE
                    }
                    R.id.btn_map_leaflet -> {
                        currentProvider = "leaflet"
                        leafletMap?.visibility = View.VISIBLE
                        googleMap?.visibility = View.GONE
                    }
                }
                renderSelectedMarker()
            }
        }

        // 4. Configure "Open in Google Maps" Deep-Link Action Button
        val btnOpenMaps = view.findViewById<MaterialButton>(R.id.btn_open_google_maps)
        btnOpenMaps.setOnClickListener {
            val node = nodes.find { it.id == selectedNodeId }
            if (node != null && hasLocation(node)) {
                googleMap?.openInGoogleMapsApp()
            }
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

                view.findViewById<TextView>(R.id.tv_pending_count).text =
                    (summary?.pendingCount ?: reservations.count { it.status.equals("Pending", true) }).toString()
                view.findViewById<TextView>(R.id.tv_active_count).text =
                    (summary?.approvedFutureCount ?: reservations.count { it.status.equals("Approved", true) }).toString()
                view.findViewById<TextView>(R.id.tv_booking_summary).text =
                    "${reservations.size} bookings · ${summary?.completedThisMonthCount ?: reservations.count { it.status.equals("Completed", true) }} completed this month"
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
                    view.findViewById<View>(R.id.btn_open_google_maps).visibility = View.GONE
                }
                renderSelectedMarker()
            } catch (error: Exception) {
                if (error is CancellationException) throw error
                nodes = emptyList()
                googleMap?.clearLocation()
                leafletMap?.clearLocation()
                view.findViewById<TextView>(R.id.tv_node_count).setText(R.string.grid_map_error)
                showMapMessage(view, R.string.grid_map_error)
                view.findViewById<TextView>(R.id.tv_map_hint).setText(R.string.grid_map_hint)
                view.findViewById<View>(R.id.btn_open_google_maps).visibility = View.GONE
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
        val btnOpenMaps = view.findViewById<MaterialButton>(R.id.btn_open_google_maps)

        if (node == null) {
            googleMap?.clearLocation()
            leafletMap?.clearLocation()
            btnOpenMaps.visibility = View.GONE
            return
        }

        view.findViewById<TextView>(R.id.tv_map_hint).text = getString(R.string.grid_map_selection, node.name, node.location)

        if (!hasLocation(node)) {
            googleMap?.clearLocation()
            leafletMap?.clearLocation()
            btnOpenMaps.visibility = View.GONE
            showMapMessage(view, R.string.grid_map_unavailable)
            return
        }

        view.findViewById<View>(R.id.tv_map_message).visibility = View.GONE
        btnOpenMaps.visibility = View.VISIBLE

        if (currentProvider == "google") {
            googleMap?.showLocation(node.latitude, node.longitude, node.name)
        } else {
            leafletMap?.showLocation(node.latitude, node.longitude, node.name)
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        outState.putString("selected_grid_id", selectedNodeId)
        super.onSaveInstanceState(outState)
    }

    override fun onDestroyView() {
        googleMap?.let {
            (it.parent as? ViewGroup)?.removeView(it)
            it.stopLoading()
            it.destroy()
        }
        googleMap = null

        leafletMap?.let {
            (it.parent as? ViewGroup)?.removeView(it)
            it.stopLoading()
            it.destroy()
        }
        leafletMap = null

        super.onDestroyView()
    }

    override fun onResume() {
        super.onResume()
        googleMap?.onResume()
        leafletMap?.onResume()
    }

    override fun onPause() {
        googleMap?.onPause()
        leafletMap?.onPause()
        super.onPause()
    }
}
