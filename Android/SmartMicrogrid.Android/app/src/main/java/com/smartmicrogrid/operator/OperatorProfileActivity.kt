package com.smartmicrogrid.operator

import android.content.Intent
import android.os.Bundle
import android.widget.FrameLayout
import com.smartmicrogrid.R
import com.smartmicrogrid.auth.LoginActivity
import com.smartmicrogrid.ui.ProfileFragment
import com.smartmicrogrid.ui.WorkspaceActivity
import com.smartmicrogrid.utils.SessionManager

class OperatorProfileActivity : WorkspaceActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!SessionManager.isLoggedIn() || !OperatorRules.canManage(SessionManager.getUserRole())) {
            startActivity(Intent(this, LoginActivity::class.java)); finish(); return
        }
        setContentView(FrameLayout(this).apply { id = R.id.fragment_container })
        if (savedInstanceState == null) supportFragmentManager.beginTransaction().replace(R.id.fragment_container, ProfileFragment()).commit()
    }
}
