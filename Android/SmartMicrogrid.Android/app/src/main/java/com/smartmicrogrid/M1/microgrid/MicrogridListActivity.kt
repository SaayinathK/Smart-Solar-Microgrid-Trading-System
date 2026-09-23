package com.smartmicrogrid.M1.microgrid

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartmicrogrid.databinding.ActivityMicrogridListBinding

class MicrogridListActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMicrogridListBinding
    private val viewModel: MicrogridViewModel by viewModels()
    private lateinit var adapter: MicrogridAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMicrogridListBinding.inflate(layoutInflater)
        setContentView(binding.root)

        supportActionBar?.title = "Microgrid Nodes"
        supportActionBar?.setDisplayHomeAsUpEnabled(true)

        adapter = MicrogridAdapter(emptyList()) { microgrid ->
            val intent = Intent(this, MicrogridDetailsActivity::class.java).apply {
                putExtra("MICROGRID_ID", microgrid.id)
            }
            startActivity(intent)
        }

        binding.recyclerView.layoutManager = LinearLayoutManager(this)
        binding.recyclerView.adapter = adapter

        binding.swipeRefresh.setOnRefreshListener {
            viewModel.loadMicrogrids()
        }

        viewModel.microgrids.observe(this) { list ->
            adapter.updateData(list)
            binding.tvEmpty.visibility = if (list.isEmpty()) View.VISIBLE else View.GONE
        }

        viewModel.isLoading.observe(this) { loading ->
            binding.progressBar.visibility = if (loading && !binding.swipeRefresh.isRefreshing) View.VISIBLE else View.GONE
            if (!loading) binding.swipeRefresh.isRefreshing = false
        }

        viewModel.errorMessage.observe(this) { err ->
            err?.let { Toast.makeText(this, it, Toast.LENGTH_LONG).show() }
        }

        viewModel.loadMicrogrids()
    }

    override fun onSupportNavigateUp(): Boolean {
        finish()
        return true
    }
}
