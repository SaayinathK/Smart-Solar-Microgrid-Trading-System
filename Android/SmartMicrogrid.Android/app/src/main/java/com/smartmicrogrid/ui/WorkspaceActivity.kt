package com.smartmicrogrid.ui

import android.view.View
import android.view.ViewGroup
import android.widget.FrameLayout
import androidx.annotation.LayoutRes
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.appbar.MaterialToolbar
import com.smartmicrogrid.R

/** Shared brand header and content shell for app-owned screens.
 * Screen content and view-binding IDs stay intact inside the shell. */
abstract class WorkspaceActivity : AppCompatActivity() {
    override fun setContentView(@LayoutRes layoutResID: Int) {
        val shell = layoutInflater.inflate(R.layout.workspace_shell, null)
        val container = shell.findViewById<FrameLayout>(R.id.workspace_content)
        attachWorkspace(shell, layoutInflater.inflate(layoutResID, container, false))
    }

    override fun setContentView(view: View) {
        attachWorkspace(layoutInflater.inflate(R.layout.workspace_shell, null), view)
    }

    override fun setContentView(view: View, params: ViewGroup.LayoutParams) {
        setContentView(view)
    }

    private fun attachWorkspace(shell: View, content: View) {
        shell.findViewById<FrameLayout>(R.id.workspace_content).addView(
            content, FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
        )
        val rootScreen = javaClass.simpleName in setOf(
            "LoginActivity", "MainActivity", "ProsumerMainActivity",
            "MicrogridOperatorMainActivity", "AdminMainActivity"
        )
        shell.findViewById<View>(R.id.workspace_back).apply {
            visibility = if (rootScreen) View.GONE else View.VISIBLE
            setOnClickListener { onBackPressedDispatcher.onBackPressed() }
        }
        shell.findViewById<View>(R.id.workspace_logo).visibility = if (rootScreen) View.VISIBLE else View.GONE
        // These screens already have a content heading; replace their redundant
        // local navigation bar with the shared back control above.
        content.findViewById<MaterialToolbar>(R.id.toolbar)?.visibility = View.GONE
        super.setContentView(shell)
    }
}
