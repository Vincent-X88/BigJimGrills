package com.example.dev_dyanast

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.view.Menu
import android.view.MenuItem
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.bottomnavigation.BottomNavigationView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import retrofit2.Call
import retrofit2.Callback
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class MenusActivity : AppCompatActivity() {

    private lateinit var recyclerView: RecyclerView
    private lateinit var adapter: CategoryAdapter

    private val apiService: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl("http://192.168.43.36:5000/")
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_menus)

        val toolbar: androidx.appcompat.widget.Toolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true) // Show back button

        recyclerView = findViewById(R.id.recyclerView)
        recyclerView.layoutManager = LinearLayoutManager(this)

        val bottomNavigationView = findViewById<BottomNavigationView>(R.id.bottomNavigationView)
        bottomNavigationView.setOnNavigationItemSelectedListener { item ->
            when (item.itemId) {
                R.id.navigation_home -> {
                    val intent = Intent(this, FeaturedItemsActivity::class.java)
                    startActivity(intent)
                    true
                }
                R.id.navigation_featured -> {
                    val intent = Intent(this, FeaturedItemsActivity::class.java)
                    startActivity(intent)
                    true
                }
                R.id.navigation_menu -> {
                    val intent = Intent(this, MenusActivity::class.java)
                    startActivity(intent)
                    true
                }
                R.id.navigation_profile -> {
                    val intent = Intent(this, ProfileEditActivity::class.java)
                    startActivity(intent)
                    true
                }
                R.id.action_notifications -> {
                    showNotificationsDialog() // Show notifications dialog when icon is clicked
                    true
                }
                else -> false
            }
        }

        fetchCategories()
    }

    private fun showNotificationsDialog() {
        val userId = getSharedPreferences("AppPrefs", MODE_PRIVATE).getString("userId", null)

        // Ensure userId is not null
        if (userId == null) {
            Log.e("FeaturedItemsActivity", "User ID is null")
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // Fetch notifications
                val notifications = apiService.getNotifications(userId) // This is now a suspend function

                withContext(Dispatchers.Main) {
                    val builder = AlertDialog.Builder(this@MenusActivity)
                    builder.setTitle("Notifications")

                    // Prepare the message string
                    val notificationMessages = if (notifications.isNotEmpty()) {
                        notifications.joinToString(separator = "\n") { it.message }
                    } else {
                        "No notifications"
                    }

                    builder.setMessage(notificationMessages) // Set the message
                    builder.setPositiveButton("OK") { dialog, _ -> dialog.dismiss() } // Dismiss button
                    builder.show() // Show the dialog

                }
            } catch (e: Exception) {
                Log.e("FeaturedItemsActivity", "Error fetching notifications", e)
            }
        }
    }


    override fun onCreateOptionsMenu(menu: Menu): Boolean {
        menuInflater.inflate(R.menu.top_nav_menu, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            android.R.id.home -> {
                finish() // Handle back button
                true
            }
            R.id.action_cart -> {
                val intent = Intent(this, CartActivity::class.java)
                startActivity(intent)
                true
            }
            R.id.action_logout -> {
                val intent = Intent(this, LoginActivity::class.java)
                startActivity(intent)
                true
            }
            R.id.navigation_profile -> {
                val intent = Intent(this, ProfileEditActivity::class.java)
                startActivity(intent)
                true
            }
            R.id.action_notifications -> {
                showNotificationsDialog() // Show notifications dialog when icon is clicked
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }

    private fun fetchCategories() {
        RetrofitClient.instance.getCategories().enqueue(object : Callback<List<Category>> {
            override fun onResponse(call: Call<List<Category>>, response: Response<List<Category>>) {
                if (response.isSuccessful) {
                    val categories = response.body()
                    adapter = CategoryAdapter(categories ?: listOf()) { category ->
                        // Handle category click
                        val intent = Intent(this@MenusActivity, MenuItemsActivity::class.java)
                        intent.putExtra("CATEGORY", category.name) // Pass the category name
                        startActivity(intent)
                    }
                    recyclerView.adapter = adapter
                } else {
                    Toast.makeText(this@MenusActivity, "Failed to fetch categories", Toast.LENGTH_SHORT).show()
                }
            }

            override fun onFailure(call: Call<List<Category>>, t: Throwable) {
                Toast.makeText(this@MenusActivity, "Error: ${t.message}", Toast.LENGTH_SHORT).show()
            }
        })
    }
}
