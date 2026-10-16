package com.smartmicrogrid.operator

import android.app.DatePickerDialog
import android.app.TimePickerDialog
import android.text.InputType
import android.view.View
import android.view.WindowManager
import android.widget.*
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.Fragment
import androidx.core.widget.addTextChangedListener
import androidx.lifecycle.DefaultLifecycleObserver
import androidx.lifecycle.LifecycleOwner
import androidx.lifecycle.lifecycleScope
import com.google.android.material.textfield.TextInputEditText
import com.google.android.material.textfield.TextInputLayout
import com.smartmicrogrid.M3.TransactionUiFormatters
import com.smartmicrogrid.models.*
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale
import java.util.TimeZone

internal class OperatorForm(private val owner: Fragment, image: Int = com.smartmicrogrid.R.drawable.img_solar_farm, private val role: String = "MicrogridOperator") {
    private val ctx = owner.requireContext()
    val body = ctx.column().apply { setPadding(ctx.dp(20), ctx.dp(8), ctx.dp(20), 0) }
    private val error = ctx.label("", 13f).apply { visibility = View.GONE; setTextColor(ctx.getColor(com.smartmicrogrid.R.color.m3_error)); accessibilityLiveRegion = View.ACCESSIBILITY_LIVE_REGION_POLITE }
    init {
        body.addView(OperatorVisuals.thumbnail(ctx, image).apply {
            layoutParams = LinearLayout.LayoutParams(-1, ctx.dp(72)).apply { bottomMargin = ctx.dp(8) }
        })
    }
    fun note(text: String) { body.addView(ctx.label(text, 13f).apply { setPadding(0, ctx.dp(6), 0, ctx.dp(6)) }) }
    fun field(label: String, initial: String = "", number: Boolean = false, multiline: Boolean = false): TextInputEditText {
        val input = TextInputEditText(ctx).apply {
            setText(initial); textSize = 15f; minHeight = ctx.dp(52)
            inputType = if (number) InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_FLAG_DECIMAL or InputType.TYPE_NUMBER_FLAG_SIGNED else InputType.TYPE_CLASS_TEXT or (if (multiline) InputType.TYPE_TEXT_FLAG_MULTI_LINE else 0)
            setSingleLine(!multiline)
        }
        body.addView(TextInputLayout(ctx).apply {
            hint = label; boxBackgroundMode = TextInputLayout.BOX_BACKGROUND_OUTLINE
            addView(input)
        }, android.widget.LinearLayout.LayoutParams(-1, -2).apply { topMargin = ctx.dp(8) })
        return input
    }
    fun choice(label: String, values: List<String>, selected: Int = 0): Spinner {
        body.addView(ctx.label(label, 13f).apply { setPadding(0, ctx.dp(12), 0, 0) })
        return Spinner(ctx).apply {
            adapter = ArrayAdapter(ctx, android.R.layout.simple_spinner_dropdown_item, values)
            setSelection(selected.coerceIn(0, (values.size - 1).coerceAtLeast(0)))
            body.addView(this, android.widget.LinearLayout.LayoutParams(-1, ctx.dp(48)))
        }
    }
    fun date(label: String): Pair<TextInputEditText, () -> Long?> {
        val input = field(label)
        var selected: Long? = null
        input.isFocusable = false
        input.setOnClickListener {
            val calendar = Calendar.getInstance().apply { if (selected != null) timeInMillis = selected!! }
            DatePickerDialog(ctx, { _, year, month, day ->
                calendar.set(year, month, day)
                TimePickerDialog(ctx, { _, hour, minute ->
                    calendar.set(Calendar.HOUR_OF_DAY, hour); calendar.set(Calendar.MINUTE, minute)
                    calendar.set(Calendar.SECOND, 0); calendar.set(Calendar.MILLISECOND, 0)
                    selected = calendar.timeInMillis
                    input.setText(SimpleDateFormat("d MMM yyyy, h:mm a", Locale.getDefault()).format(calendar.time))
                }, calendar.get(Calendar.HOUR_OF_DAY), calendar.get(Calendar.MINUTE), false).show()
            }, calendar.get(Calendar.YEAR), calendar.get(Calendar.MONTH), calendar.get(Calendar.DAY_OF_MONTH)).show()
        }
        return input to { selected }
    }
    fun show(title: String, save: String = "Save", done: () -> Unit, submit: suspend () -> Unit) {
        body.addView(error)
        val scroll = ScrollView(ctx).apply { addView(body) }
        val dialog = AlertDialog.Builder(ctx).setTitle(title).setView(scroll).setNegativeButton("Cancel", null).setPositiveButton(save, null).create()
        val lifecycle = owner.viewLifecycleOwner.lifecycle
        val observer = object : DefaultLifecycleObserver { override fun onDestroy(owner: LifecycleOwner) { dialog.dismiss() } }
        lifecycle.addObserver(observer)
        var saving = false
        dialog.setOnDismissListener { lifecycle.removeObserver(observer) }
        dialog.setOnShowListener {
            OperatorMotion.enter(body)
            dialog.window?.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE)
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                if (saving) return@setOnClickListener
                saving = true
                dialog.getButton(AlertDialog.BUTTON_POSITIVE).isEnabled = false
                dialog.getButton(AlertDialog.BUTTON_POSITIVE).text = "Saving…"
                dialog.getButton(AlertDialog.BUTTON_NEGATIVE).isEnabled = false
                dialog.setCancelable(false)
                error.visibility = View.GONE
                owner.viewLifecycleOwner.lifecycleScope.launch {
                    try {
                        check(com.smartmicrogrid.utils.SessionManager.getUserRole() == role) { "Sign in again to continue." }; submit()
                        dialog.dismiss(); done()
                        Toast.makeText(ctx, "Saved successfully", Toast.LENGTH_SHORT).show()
                    } catch (cancelled: CancellationException) { throw cancelled }
                    catch (failure: Exception) {
                        error.text = failure.message ?: "Unable to save. Check your connection and retry."
                        error.visibility = View.VISIBLE; scroll.smoothScrollTo(0, body.height)
                    } finally {
                        saving = false
                        if (dialog.isShowing) {
                            dialog.getButton(AlertDialog.BUTTON_POSITIVE).isEnabled = true
                            dialog.getButton(AlertDialog.BUTTON_POSITIVE).text = save
                            dialog.getButton(AlertDialog.BUTTON_NEGATIVE).isEnabled = true
                            dialog.setCancelable(true)
                        }
                    }
                }
            }
        }
        dialog.show()
    }
}

internal object OperatorForms {
    private fun TextInputEditText.value() = text?.toString().orEmpty().trim()
    private fun TextInputEditText.number(label: String): Double = value().toDoubleOrNull()?.takeIf { it.isFinite() } ?: error("Enter a valid $label.")
    private fun utc(value: Long) = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'", Locale.US).apply { timeZone = TimeZone.getTimeZone("UTC") }.format(Date(value))

    fun grid(owner: Fragment, grid: Microgrid? = null, done: () -> Unit) {
        val form = OperatorForm(owner)
        val name = form.field("Microgrid name", grid?.name.orEmpty())
        val address = form.field("Location / address", grid?.location.orEmpty())
        val latitude = form.field("Latitude", grid?.latitude?.toString().orEmpty(), number = true)
        val longitude = form.field("Longitude", grid?.longitude?.toString().orEmpty(), number = true)
        val locate = owner.requireContext().action("Find coordinates from address") {}
        form.body.addView(locate)
        val locationStatus = owner.requireContext().label("Coordinates can also be entered manually.", 12f)
        form.body.addView(locationStatus)
        form.body.addView(owner.requireContext().action("Choose location on map") {
            val map = com.smartmicrogrid.ui.OpenStreetMapView(owner.requireContext()).apply {
                enableLocationPicking()
                val lat = latitude.value().toDoubleOrNull(); val lng = longitude.value().toDoubleOrNull()
                if (lat != null && lng != null && lat in -90.0..90.0 && lng in -180.0..180.0) showLocation(lat, lng, name.value())
            }
            val panel = owner.requireContext().column().apply {
                addView(owner.requireContext().label("Tap the map to place the microgrid marker.", 13f))
                addView(map, android.widget.LinearLayout.LayoutParams(-1, owner.requireContext().dp(300)))
            }
            val dialog = AlertDialog.Builder(owner.requireContext()).setTitle("Microgrid location").setView(panel).setNegativeButton("Cancel", null).setPositiveButton("Use location", null).create()
            val lifecycle = owner.viewLifecycleOwner.lifecycle
            val observer = object : DefaultLifecycleObserver { override fun onDestroy(owner: LifecycleOwner) { dialog.dismiss() } }
            lifecycle.addObserver(observer)
            dialog.setOnDismissListener { lifecycle.removeObserver(observer); map.destroy() }
            dialog.setOnShowListener {
                dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                    map.readPickedLocation { point ->
                        if (point == null) Toast.makeText(owner.requireContext(), "Tap a location on the map first.", Toast.LENGTH_SHORT).show()
                        else {
                            latitude.setText(point.first.toString()); longitude.setText(point.second.toString()); dialog.dismiss()
                            locationStatus.text = "Location selected. Looking up the address…"
                            owner.viewLifecycleOwner.lifecycleScope.launch {
                                try {
                                    val result = OperatorRepository().api.reverseLocation(point.first, point.second).data()
                                    address.setText(result.formattedAddress); locationStatus.text = "Check the selected address before saving."
                                } catch (cancelled: CancellationException) { throw cancelled }
                                catch (_: Exception) { locationStatus.text = "Coordinates selected. Enter or check the address manually." }
                            }
                        }
                    }
                }
            }
            dialog.show()
        })
        locate.setOnClickListener {
            if (address.value().length < 3) { address.error = "Enter an address first"; return@setOnClickListener }
            locate.isEnabled = false
            owner.viewLifecycleOwner.lifecycleScope.launch {
                try {
                    val result = OperatorRepository().api.locateAddress(address.value()).data()
                    latitude.setText(result.lat.toString()); longitude.setText(result.lng.toString()); address.setText(result.formattedAddress)
                    locationStatus.text = if (result.isApproximate) "Approximate location. Check the coordinates before saving." else "Location found. Check before saving."
                } catch (cancelled: CancellationException) { throw cancelled }
                catch (failure: Exception) { locationStatus.text = failure.message ?: "Location unavailable. Enter coordinates manually." }
                finally { locate.isEnabled = true }
            }
        }
        val capacity = form.field("Generation capacity (kW)", grid?.capacity?.toString().orEmpty(), true)
        val storage = form.field("Battery capacity (kWh)", grid?.batteryCapacity?.toString() ?: "0", true)
        val slots = form.field("Battery storage slots", grid?.batteryStorageSlots?.toString() ?: "0", true)
        val level = if (grid == null) form.field("Current battery level (kWh)", "0", true) else null
        val statuses = listOf("Active", "Inactive", "Maintenance", "Offline")
        val status = form.choice("Operating status", statuses, statuses.indexOf(grid?.status ?: "Active"))
        val description = form.field("Description (optional)", grid?.description.orEmpty(), multiline = true)
        form.show(if (grid == null) "Register microgrid" else "Edit ${grid.name}", done = done) {
            require(name.value().length in 3..100) { "Microgrid name must be 3 to 100 characters." }
            require(address.value().isNotBlank()) { "Enter a location." }
            val lat = latitude.number("latitude"); val lng = longitude.number("longitude")
            require(lat in -90.0..90.0 && lng in -180.0..180.0) { "Enter valid latitude and longitude." }
            val total = capacity.number("generation capacity"); val battery = storage.number("battery capacity")
            val count = slots.value().toIntOrNull() ?: error("Enter a whole number of storage slots.")
            require(total in 0.01..1000000.0 && battery in 0.0..1000000.0 && count in 0..100000) { "Check generation capacity and battery storage values." }
            require(grid == null || total >= grid.reservedCapacity + grid.usedCapacity) { "Capacity cannot be smaller than existing reserved and used capacity." }
            val charge = level?.number("battery level") ?: grid?.currentBatteryLevel ?: 0.0
            require(charge in 0.0..battery) { "Battery level must be within the storage capacity." }
            val selectedStatus = statuses[status.selectedItemPosition]
            val request = OperatorMicrogridRequest(name.value(), address.value(), description.value(), lat, lng, total, battery, count, charge, selectedStatus, selectedStatus == "Active")
            val api = OperatorRepository().api
            if (grid == null) api.createMicrogrid(request).checked() else api.editMicrogrid(grid.id, request).checked()
        }
    }

    fun battery(owner: Fragment, grid: Microgrid, done: () -> Unit) {
        val form = OperatorForm(owner, com.smartmicrogrid.R.drawable.img_battery_storage)
        form.note("${grid.name} · Storage capacity ${grid.batteryCapacity} kWh")
        val charge = form.field("Current charge (kWh)", grid.currentBatteryLevel.toString(), true)
        form.show("Update battery charge", done = done) {
            val level = charge.number("battery charge")
            require(level in 0.0..grid.batteryCapacity) { "Charge must be between 0 and ${grid.batteryCapacity} kWh." }
            OperatorRepository().api.updateBattery(grid.id, UpdateBatteryRequest(grid.batteryCapacity, level)).checked()
        }
    }

    fun publish(owner: Fragment, grids: List<Microgrid>, selectedGrid: String? = null, done: () -> Unit) {
        val active = grids.filter { it.status == "Active" && it.isActive }
        if (active.isEmpty()) { Toast.makeText(owner.requireContext(), "Register or activate a microgrid before publishing energy.", Toast.LENGTH_LONG).show(); return }
        val form = OperatorForm(owner, com.smartmicrogrid.R.drawable.bg_energy_home)
        val grid = form.choice("Microgrid", active.map { "${it.name} · ${it.availableCapacity} kWh free" }, active.indexOfFirst { it.id == selectedGrid }.coerceAtLeast(0))
        val amount = form.field("Energy to publish (kWh)", number = true)
        val start = form.date("Delivery starts (local time)").second
        val end = form.date("Delivery ends (local time)").second
        val price = form.field("Price per kWh (LKR)", number = true)
        form.show("Publish energy slot", "Publish", done) {
            val node = active[grid.selectedItemPosition]
            val quantity = amount.value().toDoubleOrNull(); val rate = price.value().toDoubleOrNull()
            OperatorRules.slotError(quantity, rate, start(), end(), node)?.let { error(it) }
            OperatorRepository().api.createEnergySlot(CreateEnergySlotRequest(node.id, quantity!!, quantity, utc(start()!!), utc(end()!!), rate!!)).checked()
        }
    }

    fun book(owner: Fragment, slots: List<EnergySlot>, done: () -> Unit) {
        val live = slots.filter { OperatorRules.isLive(it) }
        if (live.isEmpty()) { Toast.makeText(owner.requireContext(), "No available energy slots in your microgrids.", Toast.LENGTH_LONG).show(); return }
        val form = OperatorForm(owner, com.smartmicrogrid.R.drawable.bg_energy_home)
        val slot = form.choice("Available energy slot", live.map { "${it.microgridName} · ${it.availableAmount} kWh · LKR ${it.pricePerUnit}/kWh" })
        val details = owner.requireContext().label("", 13f); form.body.addView(details)
        slot.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onNothingSelected(parent: AdapterView<*>?) {}
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                val row = live[position]
                details.text = "${TransactionUiFormatters.dateTime(row.startTime)} to ${TransactionUiFormatters.dateTime(row.endTime)}"
            }
        }
        val nic = form.field("Prosumer NIC")
        val verify = owner.requireContext().action("Look up prosumer") {}; form.body.addView(verify)
        val identity = owner.requireContext().label("", 13f); form.body.addView(identity)
        verify.setOnClickListener {
            val value = nic.value().uppercase(Locale.ROOT)
            if (!OperatorRules.validNic(value)) { nic.error = "Enter a valid NIC"; return@setOnClickListener }
            verify.isEnabled = false
            owner.viewLifecycleOwner.lifecycleScope.launch {
                try {
                    val user = OperatorRepository().api.getUserByNic(value).data()
                    check(user.role == "Prosumer" && user.isActive) { "Choose an active prosumer account." }
                    identity.text = "${user.firstName} ${user.lastName} · ${user.nic}"
                } catch (cancelled: CancellationException) { throw cancelled }
                catch (failure: Exception) { identity.text = failure.message ?: "Prosumer not found." }
                finally { verify.isEnabled = true }
            }
        }
        val amount = form.field("Reserve energy (kWh)", number = true)
        val estimate = owner.requireContext().label("The selected slot determines the delivery window and price.", 12f)
        form.body.addView(estimate)
        amount.addTextChangedListener {
            val quantity = amount.value().toDoubleOrNull() ?: 0.0
            estimate.text = "Estimated total: LKR ${"%.2f".format(quantity * live[slot.selectedItemPosition.coerceAtLeast(0)].pricePerUnit)}"
        }
        form.show("Book for a prosumer", "Submit booking", done) {
            val row = live[slot.selectedItemPosition]; val quantity = amount.number("energy amount")
            val targetNic = nic.value().uppercase(Locale.ROOT)
            require(OperatorRules.validNic(targetNic)) { "Enter a valid prosumer NIC." }
            require(quantity >= 0.1 && quantity <= row.availableAmount) { "Amount must be between 0.1 and ${row.availableAmount} kWh." }
            require(OperatorRules.isLive(row)) { "This delivery window has ended. Refresh available energy." }
            OperatorRepository().api.createReservation(CreateReservationRequest(row.id, quantity, targetNic)).checked()
        }
    }
}
