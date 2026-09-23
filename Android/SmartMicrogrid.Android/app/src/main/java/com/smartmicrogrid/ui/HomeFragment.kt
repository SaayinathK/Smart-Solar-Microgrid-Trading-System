package com.smartmicrogrid.ui

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.fragment.app.Fragment
import com.smartmicrogrid.R
import com.smartmicrogrid.utils.SessionManager

class HomeFragment : Fragment() {

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        val view = inflater.inflate(R.layout.fragment_home, container, false)
        
        // Personalize the greeting
        val user = SessionManager.getUser()
        val nameView = view.findViewById<TextView>(R.id.tv_greeting_name)
        if (user != null) {
            nameView.text = "${user.firstName}!"
        }
        
        return view
    }
}
