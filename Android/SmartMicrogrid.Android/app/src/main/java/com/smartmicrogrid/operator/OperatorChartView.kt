package com.smartmicrogrid.operator

import android.animation.ValueAnimator
import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.Rect
import android.graphics.RectF
import android.view.View
import android.view.ViewTreeObserver
import android.view.animation.DecelerateInterpolator
import com.smartmicrogrid.R
import kotlin.math.min

/** Animates when scrolled into view; static accessible values accompany every chart. */
internal open class OperatorAnimatedView(context: Context) : View(context), ViewTreeObserver.OnScrollChangedListener {
    protected var reveal = 1f
    private var played = false
    private var animator: ValueAnimator? = null
    protected fun changed() { played = false; reveal = if (OperatorMotion.enabled(context)) 0f else 1f; invalidate(); post { playIfVisible() } }
    override fun onAttachedToWindow() { super.onAttachedToWindow(); viewTreeObserver.addOnScrollChangedListener(this); post { playIfVisible() } }
    override fun onScrollChanged() { playIfVisible() }
    private fun playIfVisible() {
        if (played || !isAttachedToWindow || !isShown || width == 0 || height == 0) return
        val visible = Rect()
        if (!getGlobalVisibleRect(visible) || visible.height() < height / 3) return
        played = true
        if (!OperatorMotion.enabled(context)) { reveal = 1f; invalidate(); return }
        animator?.cancel()
        animator = ValueAnimator.ofFloat(0f, 1f).apply {
            duration = 700L; interpolator = DecelerateInterpolator()
            addUpdateListener { reveal = it.animatedValue as Float; invalidate() }
            start()
        }
    }
    override fun onDetachedFromWindow() {
        if (viewTreeObserver.isAlive) viewTreeObserver.removeOnScrollChangedListener(this)
        animator?.cancel(); animator = null; reveal = 1f
        super.onDetachedFromWindow()
    }
}

internal data class OperatorSeries(val label: String, val value: Double, val color: Int)

internal class OperatorChartView(context: Context, private val donut: Boolean, private val series: List<OperatorSeries>, private val countLabel: String = "microgrids") : OperatorAnimatedView(context) {
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG)
    private val textColor get() = context.getColor(R.color.m3_text_primary)
    private val mutedColor get() = context.getColor(R.color.m3_text_secondary)
    private val scale = resources.displayMetrics.density
    private val font = resources.displayMetrics.scaledDensity
    init {
        contentDescription = series.joinToString("; ") { "${it.label}: ${OperatorOverview.format(it.value)}${if (donut) " $countLabel" else " kWh"}" }
        importantForAccessibility = IMPORTANT_FOR_ACCESSIBILITY_YES
        isFocusable = true
        changed()
    }
    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        if (donut) drawDonut(canvas) else drawBars(canvas)
    }
    private fun drawDonut(canvas: Canvas) {
        val total = series.sumOf { it.value.coerceAtLeast(0.0) }
        val diameter = min(width.toFloat() - 36 * scale, height.toFloat() - 28 * scale)
        val left = (width - diameter) / 2
        val top = (height - diameter) / 2
        val inset = 14 * scale
        val rect = RectF(left + inset, top + inset, left + diameter - inset, top + diameter - inset)
        paint.style = Paint.Style.STROKE; paint.strokeWidth = 22 * scale; paint.strokeCap = Paint.Cap.BUTT
        paint.color = context.getColor(R.color.surface_soft); canvas.drawArc(rect, 0f, 360f, false, paint)
        var start = -90f
        if (total > 0) series.forEach { part ->
            val sweep = (part.value.coerceAtLeast(0.0) / total * 360).toFloat()
            paint.color = part.color
            if (sweep > 0) canvas.drawArc(rect, start, (sweep - 2f).coerceAtLeast(0f) * reveal, false, paint)
            start += sweep
        }
        paint.style = Paint.Style.FILL; paint.textAlign = Paint.Align.CENTER
        paint.color = textColor; paint.typeface = android.graphics.Typeface.DEFAULT_BOLD; paint.textSize = 32 * font
        canvas.drawText(OperatorOverview.format(total * reveal), width / 2f, height / 2f + 5 * scale, paint)
        paint.color = mutedColor; paint.typeface = android.graphics.Typeface.DEFAULT; paint.textSize = 12 * font
        canvas.drawText(countLabel, width / 2f, height / 2f + 27 * scale, paint)
    }
    private fun drawBars(canvas: Canvas) {
        val max = series.maxOfOrNull { it.value }?.coerceAtLeast(1.0) ?: 1.0
        val left = 36 * scale; val right = width - 8 * scale
        val top = 26 * scale; val bottom = height - 32 * scale
        paint.typeface = android.graphics.Typeface.DEFAULT; paint.textSize = 10 * font
        repeat(4) { step ->
            val y = bottom - (bottom - top) * step / 3f
            paint.color = context.getColor(R.color.surface_outline); paint.strokeWidth = scale
            canvas.drawLine(left, y, right, y, paint)
            paint.color = mutedColor; paint.textAlign = Paint.Align.RIGHT
            canvas.drawText(compact(max * step / 3), left - 5 * scale, y + 3 * scale, paint)
        }
        val cell = (right - left) / series.size.coerceAtLeast(1)
        series.forEachIndexed { index, part ->
            val center = left + cell * (index + .5f)
            val value = part.value.coerceAtLeast(0.0)
            val barTop = bottom - (value / max * (bottom - top) * reveal).toFloat()
            paint.color = part.color; paint.style = Paint.Style.FILL
            canvas.drawRoundRect(RectF(center - cell * .26f, barTop, center + cell * .26f, bottom), 6 * scale, 6 * scale, paint)
            paint.color = textColor; paint.textAlign = Paint.Align.CENTER; paint.textSize = 10 * font
            canvas.drawText(compact(value * reveal), center, barTop - 6 * scale, paint)
            paint.color = mutedColor; paint.textSize = 10 * font
            canvas.drawText(part.label, center, bottom + 19 * scale, paint)
        }
    }
    private fun compact(value: Double): String = if (value >= 10000) "${OperatorOverview.format(value / 1000)}k" else OperatorOverview.format(value)
}

internal class OperatorMeterView(context: Context, value: Int, private val color: Int) : OperatorAnimatedView(context) {
    private val fraction = value.coerceIn(0, 100) / 100f
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG)
    init { importantForAccessibility = IMPORTANT_FOR_ACCESSIBILITY_NO; changed() }
    override fun onDraw(canvas: Canvas) {
        paint.color = context.getColor(R.color.surface_soft)
        val rect = RectF(0f, 0f, width.toFloat(), height.toFloat())
        canvas.drawRoundRect(rect, height / 2f, height / 2f, paint)
        paint.color = color; rect.right = width * fraction * reveal
        canvas.drawRoundRect(rect, height / 2f, height / 2f, paint)
    }
}
