package com.example.dev_dyanast

import CartItem
import CartResponse
import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
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


class CartActivity : AppCompatActivity() {

    private lateinit var recyclerView: RecyclerView
    private lateinit var totalPriceTextView: TextView
    private lateinit var checkoutButton: Button
    private lateinit var adapter: CartAdapter

    private val apiService: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl("http://192.168.43.36:5000/")
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }


    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_cart)

        val toolbar: androidx.appcompat.widget.Toolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)


        recyclerView = findViewById(R.id.recyclerView)
        totalPriceTextView = findViewById(R.id.totalPrice)
        checkoutButton = findViewById(R.id.checkoutButton)

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
                else -> false
            }
        }

        fetchCartItems()

        checkoutButton.setOnClickListener {
            // Handle checkout action
            completeCheckout()
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
                    val builder = AlertDialog.Builder(this@CartActivity)
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


    private fun completeCheckout() {
        val userId = getSharedPreferences("AppPrefs", MODE_PRIVATE).getString("userId", null)
        if (userId != null) {
            val checkoutRequest = CheckoutRequest(userId)

            apiService.completeCheckout(checkoutRequest).enqueue(object : Callback<CheckoutResponse> {
                override fun onResponse(call: Call<CheckoutResponse>, response: Response<CheckoutResponse>) {
                    if (response.isSuccessful) {
                        // Log the raw response
                        Log.d("CheckoutResponse", "Raw response: ${response.body()}")

                        response.body()?.let { responseBody ->
                            // Log the parsed response
                            Log.d("CheckoutResponse", "Parsed response: orderNumber=${responseBody.orderNumber}, userName=${responseBody.userName}")

                            val orderNumber = responseBody.orderNumber
                            val userName = responseBody.userName

                            // Navigate to PaymentSuccessActivity with actual values
                            val intent = Intent(this@CartActivity, PaymentSuccessActivity::class.java).apply {
                                putExtra("ORDER_NUMBER", orderNumber)
                                putExtra("USER_NAME", userName)
                            }
                            startActivity(intent)

                            // Finish the CartActivity so the user can't go back to it
                            finish()
                        }
                    } else {
                        Toast.makeText(this@CartActivity, "Checkout failed", Toast.LENGTH_SHORT).show()
                    }
                }

                override fun onFailure(call: Call<CheckoutResponse>, t: Throwable) {
                    Toast.makeText(this@CartActivity, "Error: ${t.message}", Toast.LENGTH_SHORT).show()
                }
            })
        } else {
            Toast.makeText(this, "User not logged in", Toast.LENGTH_SHORT).show()
        }
    }





    fun fetchCartItems() {
        val userId = getSharedPreferences("AppPrefs", MODE_PRIVATE).getString("userId", null)

        if (userId != null) {
            RetrofitClient.instance.getCartItems(userId).enqueue(object : Callback<CartResponse> {
                override fun onResponse(call: Call<CartResponse>, response: Response<CartResponse>) {
                    if (response.isSuccessful) {
                        val cartResponse = response.body()
                        adapter = CartAdapter(cartResponse?.items ?: listOf())
                        recyclerView.adapter = adapter
                        totalPriceTextView.text = "Total: R${cartResponse?.totalPrice ?: 0.0}"
                    } else {
                        Toast.makeText(this@CartActivity, "Failed to fetch cart items: ${response.message()}", Toast.LENGTH_SHORT).show()
                    }
                }



                override fun onFailure(call: Call<CartResponse>, t: Throwable) {
                    Toast.makeText(this@CartActivity, "Error: ${t.message}", Toast.LENGTH_SHORT).show()
                }
            })
        } else {
            Toast.makeText(this, "User not logged in", Toast.LENGTH_SHORT).show()
        }
    }
}

class CartAdapter(private val items: List<CartItem>) : RecyclerView.Adapter<CartAdapter.ViewHolder>() {

    class ViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        val productName: TextView = itemView.findViewById(R.id.productName)
        val quantity: TextView = itemView.findViewById(R.id.quantity)
        val price: TextView = itemView.findViewById(R.id.price)
        val productImage: ImageView = itemView.findViewById(R.id.productImage)
        val removeButton: Button = itemView.findViewById(R.id.removeButton)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_cart, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        holder.productName.text = item.name
        holder.quantity.text = "Quantity: ${item.quantity}"
        holder.price.text = "R${item.price * item.quantity}"

        // Load image using Glide
        Glide.with(holder.itemView.context)
            .load(item.imageUrl)
            .placeholder(R.drawable.ic_launcher_background) // Placeholder while image loads
            .error(R.drawable.ic_launcher_background) // Error image
            .into(holder.productImage)

        holder.removeButton.setOnClickListener {
            // Handle remove item

            RetrofitClient.instance.removeFromCart(item.id).enqueue(object : Callback<ApiResponse> {
                override fun onResponse(call: Call<ApiResponse>, response: Response<ApiResponse>) {
                    if (response.isSuccessful) {
                        Toast.makeText(holder.itemView.context, "Item removed from cart", Toast.LENGTH_SHORT).show()
                        // Refresh the cart items
                        (holder.itemView.context as CartActivity).fetchCartItems()
                    } else {
                        Toast.makeText(holder.itemView.context, "Failed to remove item: ${response.message()}", Toast.LENGTH_SHORT).show()
                    }
                }

                override fun onFailure(call: Call<ApiResponse>, t: Throwable) {
                    Toast.makeText(holder.itemView.context, "Error: ${t.message}", Toast.LENGTH_SHORT).show()
                }
            })
        }
    }

    override fun getItemCount(): Int = items.size
}
