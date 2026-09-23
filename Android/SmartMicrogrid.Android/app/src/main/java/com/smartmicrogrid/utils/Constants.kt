package com.smartmicrogrid.utils

object Constants {
    // Configurable API Base URL for LAN, localhost, or physical devices
    // For Android emulator: "http://10.0.2.2:5000/api/"
    // For physical device over Wi-Fi: "http://192.168.1.50:5000/api/"
    const val API_BASE_URL = "http://10.0.2.2:5000/api/"

    const val PREF_NAME = "smart_microgrid_prefs"
    const val KEY_JWT_TOKEN = "jwt_token"
    const val KEY_USER_ROLE = "user_role"
    const val KEY_USER_NAME = "user_name"
    const val KEY_USER_EMAIL = "user_email"
    const val KEY_USER_JSON = "user_json"
}
