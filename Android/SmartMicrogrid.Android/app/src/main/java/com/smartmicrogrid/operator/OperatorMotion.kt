package com.smartmicrogrid.operator

import android.animation.ValueAnimator
import android.content.Context
import android.provider.Settings
import android.view.View
import android.view.animation.DecelerateInterpolator

internal object OperatorMotion {
    fun enabled(context: Context): Boolean =
        Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) > 0f &&
            Settings.Global.getFloat(context.contentResolver, Settings.Global.TRANSITION_ANIMATION_SCALE, 1f) > 0f

    fun enter(view: View, delay: Long = 0L) {
        view.animate().cancel()
        if (!enabled(view.context)) { view.alpha = 1f; view.translationY = 0f; return }
        view.alpha = 0f; view.translationY = view.context.dp(12).toFloat()
        view.animate().alpha(1f).translationY(0f).setStartDelay(delay).setDuration(260L).setInterpolator(DecelerateInterpolator()).start()
    }

    fun count(view: android.widget.TextView, target: Double, suffix: String = "") {
        view.contentDescription = "${OperatorOverview.format(target)}$suffix"
        if (!enabled(view.context)) { view.text = "${OperatorOverview.format(target)}$suffix"; return }
        val animator = ValueAnimator.ofFloat(0f, 1f).apply {
            duration = 650L; interpolator = DecelerateInterpolator()
            addUpdateListener { view.text = "${OperatorOverview.format(target * (it.animatedValue as Float))}$suffix" }
        }
        view.addOnAttachStateChangeListener(object : View.OnAttachStateChangeListener {
            override fun onViewAttachedToWindow(v: View) { animator.start() }
            override fun onViewDetachedFromWindow(v: View) { animator.cancel(); view.text = "${OperatorOverview.format(target)}$suffix"; v.removeOnAttachStateChangeListener(this) }
        })
        view.text = "${OperatorOverview.format(target)}$suffix"
        if (view.isAttachedToWindow) animator.start()
    }
}
