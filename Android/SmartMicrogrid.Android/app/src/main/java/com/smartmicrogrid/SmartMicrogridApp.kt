package com.smartmicrogrid

import android.app.Application
import com.smartmicrogrid.utils.ThemeManager
import com.smartmicrogrid.utils.SessionManager

class SmartMicrogridApp : Application() {
    override fun onCreate() {
        super.onCreate()
        // Initialize and apply the saved theme
        ThemeManager.init(this)
        ThemeManager.applyCurrentTheme()
        // Restore role, credentials and server before any restored operator screen runs.
        SessionManager.init(this)
    }
}
