package com.smartmicrogrid.prosumer

import android.content.Intent
import android.graphics.Bitmap
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.espresso.Espresso.onView
import androidx.test.espresso.action.ViewActions.click
import androidx.test.espresso.assertion.ViewAssertions.matches
import androidx.test.espresso.matcher.ViewMatchers.isDisplayed
import androidx.test.espresso.matcher.ViewMatchers.withText
import com.smartmicrogrid.M2.ProsumerMainActivity
import com.smartmicrogrid.R
import com.smartmicrogrid.data.local.AppDatabase
import com.smartmicrogrid.data.remote.RetrofitClient
import com.smartmicrogrid.models.LoginRequest
import com.smartmicrogrid.operator.data
import com.smartmicrogrid.ui.ProfileFragment
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Assume.assumeTrue
import org.junit.Test
import org.junit.runner.RunWith
import java.io.File

/** Opt-in demo navigation check. Never submits bookings, changes profiles or advances transfers. */
@RunWith(AndroidJUnit4::class)
class ProsumerWorkspaceTest {
    @Test fun demoProsumerCanNavigateEveryDestination() {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val args = InstrumentationRegistry.getArguments()
        assumeTrue("Run explicitly with -e demoProsumerSmoke true", args.getString("demoProsumerSmoke") == "true")
        val context = instrumentation.targetContext
        SessionManager.init(context)
        val originalUser = SessionManager.getUser()
        val originalToken = SessionManager.getToken()
        val originalBase = SessionManager.getBaseUrl()
        val dao = AppDatabase.getDatabase(context).reservationDao()
        val originalCache = runBlocking { dao.all() }
        var activity: ProsumerMainActivity? = null
        try {
            val login = runBlocking { RetrofitClient.apiService.login(LoginRequest("prosumer@microgrid.com", "Prosumer123!")).data() }
            assertEquals("Prosumer", login.user.role)
            SessionManager.setSession(login.token, login.user)
            val repository = ProsumerRepository()
            runBlocking {
                assertTrue(repository.reservations(context).all { ProsumerRules.owns(it.prosumerId, login.user) })
                assertTrue(repository.transactions().all { ProsumerRules.owns(it.prosumerId, login.user) })
                repository.offers(); repository.grids()
            }
            activity = instrumentation.startActivitySync(Intent(context, ProsumerMainActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)) as ProsumerMainActivity
            val host = activity
            fun waitFor(label: String, ready: () -> Boolean) {
                val deadline = System.currentTimeMillis() + 25000
                while (System.currentTimeMillis() < deadline) {
                    var done = false
                    instrumentation.runOnMainSync { done = ready() }
                    if (done) { instrumentation.waitForIdleSync(); return }
                    Thread.sleep(150)
                }
                fail("Timed out waiting for $label")
            }
            fun texts(view: View): List<String> = (if (view is TextView) listOf(view.text.toString()) else emptyList()) +
                (if (view is ViewGroup) (0 until view.childCount).flatMap { texts(view.getChildAt(it)) } else emptyList())
            fun capture(name: String) {
                Thread.sleep(700) // Include the compositor's rounded clipping and settled entry motion.
                instrumentation.waitForIdleSync()
                val bitmap = requireNotNull(instrumentation.uiAutomation.takeScreenshot())
                val folder = File(context.getExternalFilesDir(null), "prosumer-review").apply { mkdirs() }
                File(folder, "$name.png").outputStream().use { bitmap.compress(Bitmap.CompressFormat.PNG, 100, it) }
                bitmap.recycle()
            }
            fun listLoaded(type: Class<*>) = host.supportFragmentManager.findFragmentById(R.id.fragment_container)?.let { fragment ->
                type.isInstance(fragment) && fragment.view != null && !descendantRefreshing(fragment.requireView())
            } == true
            waitFor("home data") { listLoaded(ProsumerHomeFragment::class.java) && texts(host.window.decorView).any { it.startsWith("Updated just now") } }
            Thread.sleep(800); capture("home")
            instrumentation.runOnMainSync { host.openExplore() }
            waitFor("energy offers") { listLoaded(ProsumerExploreFragment::class.java) && texts(host.window.decorView).any { it.contains("offers ·") } }
            capture("energy-offers")
            val bookable = runBlocking { repository.offers().firstOrNull { ProsumerRules.bookingIssue(it) == null } }
            if (bookable != null) {
                instrumentation.runOnMainSync { ProsumerForms.book(host.supportFragmentManager.findFragmentById(R.id.fragment_container)!!, bookable) {} }
                onView(withText("Reserve clean energy")).check(matches(isDisplayed()))
                val screenshot = instrumentation.uiAutomation.takeScreenshot()
                if (screenshot != null) {
                    val folder = File(context.getExternalFilesDir(null), "prosumer-review").apply { mkdirs() }
                    File(folder, "booking-review.png").outputStream().use { screenshot.compress(Bitmap.CompressFormat.PNG, 100, it) }
                    screenshot.recycle()
                }
                onView(withText("Cancel")).perform(click())
            }
            instrumentation.runOnMainSync { host.openExplore(1) }
            waitFor("microgrids") { listLoaded(ProsumerExploreFragment::class.java) && texts(host.window.decorView).any { it.contains("active microgrids ·") } }
            capture("microgrids")
            instrumentation.runOnMainSync { host.openExplore(2) }
            waitFor("capacity") { listLoaded(ProsumerExploreFragment::class.java) && texts(host.window.decorView).any { it.contains("kW generation") } }
            capture("capacity")
            instrumentation.runOnMainSync { host.openExplore(3) }
            waitFor("storage") { listLoaded(ProsumerExploreFragment::class.java) && texts(host.window.decorView).any { it.contains("storage systems ·") } }
            capture("storage")
            instrumentation.runOnMainSync { host.openExplore(4) }
            waitFor("published slots") { listLoaded(ProsumerExploreFragment::class.java) && texts(host.window.decorView).any { it.contains("published slots ·") } }
            capture("energy-slots")
            instrumentation.runOnMainSync { host.openBookings(2) }
            waitFor("bookings") { listLoaded(ProsumerBookingsFragment::class.java) && texts(host.window.decorView).any { it.contains("awaiting approval ·") } }
            capture("bookings")
            instrumentation.runOnMainSync { host.openTransfers() }
            waitFor("transfers") { listLoaded(ProsumerTransfersFragment::class.java) && texts(host.window.decorView).any { it.contains("in progress ·") } }
            capture("transfers")
            instrumentation.runOnMainSync { host.findViewById<com.google.android.material.bottomnavigation.BottomNavigationView>(R.id.bottom_navigation).selectedItemId = R.id.nav_profile }
            waitFor("profile") { listLoaded(ProfileFragment::class.java) && texts(host.window.decorView).contains(login.user.nic) }
            capture("profile")
        } finally {
            instrumentation.runOnMainSync { activity?.finish() }
            instrumentation.waitForIdleSync()
            if (originalUser != null && originalToken != null) SessionManager.setSession(originalToken, originalUser) else SessionManager.logout()
            SessionManager.setBaseUrl(originalBase)
            runBlocking { dao.replaceAll(originalCache) }
            assertEquals("The original phone account must be restored", originalUser?.id, SessionManager.getUser()?.id)
        }
    }
    private fun descendantRefreshing(view: View): Boolean =
        (view is androidx.swiperefreshlayout.widget.SwipeRefreshLayout && view.isRefreshing) ||
            (view is ViewGroup && (0 until view.childCount).any { descendantRefreshing(view.getChildAt(it)) })
}
