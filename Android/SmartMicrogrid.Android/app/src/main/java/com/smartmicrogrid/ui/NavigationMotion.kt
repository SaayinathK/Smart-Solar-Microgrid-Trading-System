package com.smartmicrogrid.ui

import android.content.Context
import android.provider.Settings
import androidx.fragment.app.FragmentTransaction
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.smartmicrogrid.R

/** A single motion policy for the three role workspaces. */
object NavigationMotion {
    fun configure(navigation: BottomNavigationView) {
        navigation.isItemHorizontalTranslationEnabled = false
        // Tapping the selected tab should not recreate the screen or repeat requests.
        navigation.setOnItemReselectedListener { }
    }

    fun transition(transaction: FragmentTransaction, context: Context): FragmentTransaction {
        val animationScale = Settings.Global.getFloat(
            context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f
        )
        val transitionScale = Settings.Global.getFloat(
            context.contentResolver, Settings.Global.TRANSITION_ANIMATION_SCALE, 1f
        )
        if (animationScale > 0f && transitionScale > 0f) {
            transaction.setCustomAnimations(R.anim.surface_enter, R.anim.fade_out)
        }
        return transaction
    }
}
