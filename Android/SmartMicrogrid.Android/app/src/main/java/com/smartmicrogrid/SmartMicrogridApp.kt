package com.smartmicrogrid

import android.app.Application
import com.smartmicrogrid.utils.ThemeManager

class SmartMicrogridApp : Application() {
    override fun onCreate() {
        super.onCreate()
        com.smartmicrogrid.utils.SessionManager.init(this)
        // Initialize and apply the saved theme
        ThemeManager.init(this)
        ThemeManager.applyCurrentTheme()
    }
}
