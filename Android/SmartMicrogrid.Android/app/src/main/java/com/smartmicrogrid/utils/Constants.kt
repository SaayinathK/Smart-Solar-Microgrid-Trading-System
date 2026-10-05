package com.smartmicrogrid.utils

object Constants {
    const val DEFAULT_LAN_URL = "http://192.168.1.6:5050/api/"
    const val DEFAULT_EMULATOR_URL = "http://10.0.2.2:5050/api/"
    const val DEFAULT_IIS_URL = "http://192.168.1.6:80/api/"

    // Dynamically chooses emulator loopback (10.0.2.2) or LAN Wi-Fi as default fallback
    val API_BASE_URL: String
        get() = if (ServerDiscovery.isEmulator()) DEFAULT_EMULATOR_URL else DEFAULT_LAN_URL

    const val PREF_NAME = "smart_microgrid_prefs"
    const val KEY_CUSTOM_API_BASE_URL = "custom_api_base_url"
    const val KEY_JWT_TOKEN = "jwt_token"
    const val KEY_USER_ROLE = "user_role"
    const val KEY_USER_NAME = "user_name"
    const val KEY_USER_EMAIL = "user_email"
    const val KEY_USER_JSON = "user_json"
}
