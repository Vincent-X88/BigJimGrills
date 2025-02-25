package com.example.dev_dyanast

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.PopupMenu
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.example.dev_dyanast.R
import com.google.android.material.bottomnavigation.BottomNavigationView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

data class Notification(
    val id: Int,
    val message: String,  // This must be a non-nullable String if you are confident it will always have a value
    val createdAt: String // Adjust this according to your API response
)

data class FeaturedItem(
    val productId: Int,
    val name: String,
    val description: String,
    val imageUrl: String,
    val price: Double
)


class FeaturedItemsActivity : AppCompatActivity() {

    private lateinit var recyclerView: RecyclerView
    private lateinit var adapter: FeaturedItemsAdapter
    private val apiService: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl("http://192.168.43.36:5000/")
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_featured_items)

        val toolbar: androidx.appcompat.widget.Toolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true) // Show back button

        recyclerView = findViewById(R.id.recyclerView)
        recyclerView.layoutManager = LinearLayoutManager(this)

        val bottomNavigationView = findViewById<BottomNavigationView>(R.id.bottomNavigationView)
        bottomNavigationView.setOnNavigationItemSelectedListener { item: MenuItem ->
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
                else -> false
            }
        }

        fetchFeaturedItems()
    }

    override fun onCreateOptionsMenu(menu: Menu): Boolean {
        menuInflater.inflate(R.menu.top_nav_menu, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
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
            R.id.action_notifications -> {
                showNotificationsDialog() // Show notifications dialog when icon is clicked
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
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
                    val builder = AlertDialog.Builder(this@FeaturedItemsActivity)
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




    private fun fetchFeaturedItems() {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val items = apiService.getFeaturedItems()
                withContext(Dispatchers.Main) {
                    adapter = FeaturedItemsAdapter(items)
                    recyclerView.adapter = adapter
                }
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }
}

class FeaturedItemsAdapter(private val items: List<FeaturedItem>) : RecyclerView.Adapter<FeaturedItemsAdapter.ViewHolder>() {

    class ViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        val name: TextView = itemView.findViewById(R.id.itemName)
        val price: TextView = itemView.findViewById(R.id.itemPrice)
        val image: ImageView = itemView.findViewById(R.id.itemImage)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_featured, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        // Calculate the "was" price and format it to two decimal places
        val wasPrice = String.format("%.2f", item.price + 5.0)

        // Display the actual price and the "was" price
        holder.name.text = item.name
        holder.price.text = "R${item.price} (was R${wasPrice})"

        Glide.with(holder.itemView.context)
            .load(item.imageUrl)
            .centerCrop()
            .into(holder.image)

        holder.itemView.setOnClickListener {
            val intent = Intent(holder.itemView.context, ProductDetailActivity::class.java).apply {
                putExtra("PRODUCT_ID", item.productId.toString())
                putExtra("IMAGE_URL", item.imageUrl)
                putExtra("NAME", item.name)
                putExtra("DESCRIPTION", item.description)
                putExtra("PRICE", item.price.toString())
            }
            holder.itemView.context.startActivity(intent)
        }
    }

    override fun getItemCount(): Int = items.size
}
