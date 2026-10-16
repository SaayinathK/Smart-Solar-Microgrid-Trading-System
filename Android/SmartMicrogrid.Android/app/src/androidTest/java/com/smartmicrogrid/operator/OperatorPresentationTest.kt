package com.smartmicrogrid.operator

import android.content.res.Configuration
import android.graphics.Bitmap
import android.graphics.Canvas
import android.view.LayoutInflater
import android.view.View
import android.widget.LinearLayout
import androidx.appcompat.view.ContextThemeWrapper
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.smartmicrogrid.R
import org.junit.Assert.*
import org.junit.Test
import org.junit.runner.RunWith

/** Exercises native measurement/drawing and real theme resources without using an account or API. */
@RunWith(AndroidJUnit4::class)
class OperatorPresentationTest {
    @Test fun compactDashboardMeasuresAndDrawsInBothThemesWithLargeText() {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        instrumentation.runOnMainSync {
            for (night in listOf(Configuration.UI_MODE_NIGHT_NO, Configuration.UI_MODE_NIGHT_YES)) {
                val base = instrumentation.targetContext
                val config = Configuration(base.resources.configuration).apply {
                    uiMode = Configuration.UI_MODE_TYPE_NORMAL or night
                    fontScale = 1.3f
                }
                val ctx = ContextThemeWrapper(base.createConfigurationContext(config), R.style.Theme_SmartMicrogrid)
                val root = ctx.column()
                val welcome = OperatorVisuals.photo(ctx, "Welcome, Kalaranee Kanesamoorthy!", "Your network, bookings and energy transfers.", R.drawable.bg_energy_home, 88, compact = true)
                root.addView(welcome)
                val stats = LinearLayout(ctx).apply { isBaselineAligned = false }
                val first = OperatorVisuals.stat(ctx, "Generation capacity", 1240.5, " kWh", "520 kWh available", OperatorVisuals.green, R.drawable.ic_solar_panel)
                val second = OperatorVisuals.stat(ctx, "Battery storage", 640.0, " kWh", "Of 800 kWh · 80%", OperatorVisuals.cyan, R.drawable.ic_nav_health)
                stats.addView(first, LinearLayout.LayoutParams(0, -1, 1f))
                stats.addView(second, LinearLayout.LayoutParams(0, -1, 1f))
                root.addView(stats)
                val chart = OperatorChartView(ctx, true, listOf(OperatorSeries("Active", 3.0, OperatorVisuals.green), OperatorSeries("Offline", 1.0, OperatorVisuals.red)))
                root.addView(chart, LinearLayout.LayoutParams(-1, ctx.dp(176)))
                root.addView(OperatorChartView(ctx, false, listOf(OperatorSeries("Available", 520.0, OperatorVisuals.green), OperatorSeries("Reserved", 320.0, OperatorVisuals.amber), OperatorSeries("Used", 400.5, OperatorVisuals.purple), OperatorSeries("Total", 1240.5, OperatorVisuals.cyan))), LinearLayout.LayoutParams(-1, ctx.dp(176)))
                root.addView(OperatorMeterView(ctx, 80, OperatorVisuals.cyan), LinearLayout.LayoutParams(-1, ctx.dp(7)))
                val adapter = OperatorCardAdapter()
                adapter.submit(listOf(OperatorCard("Colombo Solar Generation Hub", "TX-1029", "EnergyTransferInProgress",
                    listOf("25 kWh · LKR 625.00"), actions = listOf(OperatorAction("Confirm transfer", true) {}), imageRes = R.drawable.img_solar_farm)))
                val record = adapter.createViewHolder(root, 0)
                adapter.bindViewHolder(record, 0)
                root.addView(record.card)
                val width = ctx.dp(320)
                root.measure(View.MeasureSpec.makeMeasureSpec(width, View.MeasureSpec.EXACTLY), View.MeasureSpec.makeMeasureSpec(0, View.MeasureSpec.UNSPECIFIED))
                root.layout(0, 0, width, root.measuredHeight)
                assertTrue("The full-size image must not expand the welcome banner", welcome.height <= ctx.dp(160))
                assertTrue("The banner must keep space for its text", welcome.height >= ctx.dp(88))
                val banner = welcome.getChildAt(0) as android.widget.FrameLayout
                val photo = banner.getChildAt(0) as android.widget.ImageView
                assertEquals("The photo must fill the banner width", banner.width, photo.width)
                assertEquals("The photo must fill the banner height", banner.height, photo.height)
                val crop = android.graphics.RectF(0f, 0f, photo.drawable.intrinsicWidth.toFloat(), photo.drawable.intrinsicHeight.toFloat())
                photo.imageMatrix.mapRect(crop)
                assertTrue("The crop must cover both sides without gaps", crop.left <= 1f && crop.right >= photo.width - 1f)
                assertTrue("The crop must cover the top and bottom", crop.top <= 1f && crop.bottom >= photo.height - 1f)
                assertTrue("Photo and cards must have measurable content", root.measuredHeight > ctx.dp(600))
                assertEquals("Both statistic cards stay inside the mobile width", width, first.width + second.width)
                assertTrue(first.height > ctx.dp(100))
                assertEquals(first.height, second.height)
                val heading = record.body.getChildAt(0) as LinearLayout
                assertTrue("Long status labels must leave a readable name column", heading.getChildAt(1).width >= ctx.dp(180))
                assertTrue(chart.contentDescription.contains("Active: 3"))
                val bitmap = Bitmap.createBitmap(width, root.measuredHeight, Bitmap.Config.ARGB_8888)
                root.draw(Canvas(bitmap))
                bitmap.recycle()
            }
        }
    }

    @Test fun transferWorkflowLayoutsInflateWithTheirImagesAndControls() {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        instrumentation.runOnMainSync {
            val context = ContextThemeWrapper(instrumentation.targetContext, R.style.Theme_SmartMicrogrid)
            for ((layout, action) in listOf(
                R.layout.activity_transaction_details to R.id.chip_status,
                R.layout.activity_energy_transfer_confirmation to R.id.tv_transaction_id,
                R.layout.activity_transaction_completion to R.id.btn_view_transaction,
                R.layout.activity_transaction_qr to R.id.qrCard)) {
                val root = LayoutInflater.from(context).inflate(layout, null)
                assertNotNull("Workflow control missing from layout $layout", root.findViewById<View>(action))
            }
        }
    }
}
