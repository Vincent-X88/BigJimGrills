package com.example.dev_dyanast

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.view.Menu
import android.view.MenuItem
import android.widget.Button
import android.widget.EditText
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
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

class ProductDetailActivity : AppCompatActivity() {

    private lateinit var productImage: ImageView
    private lateinit var productName: TextView
    private lateinit var productDescription: TextView
    private lateinit var quantity: EditText
    private lateinit var addToCartButton: Button

    private val apiService: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl("http://192.168.43.36:5000/")
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }


    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_product_detail)

        val toolbar: androidx.appcompat.widget.Toolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        quantity = findViewById(R.id.quantity)
        quantity.setText("1")  // Set initial value to 1


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

        productImage = findViewById(R.id.productImage)
        productName = findViewById(R.id.productName)
        productDescription = findViewById(R.id.productDescription)
        quantity = findViewById(R.id.quantity)
        addToCartButton = findViewById(R.id.addToCartButton)

        // Get data from Intent
        val imageUrl = intent.getStringExtra("IMAGE_URL")
        val name = intent.getStringExtra("NAME")
        val description = intent.getStringExtra("DESCRIPTION")


        // Set data to views
        Glide.with(this).load(imageUrl).into(productImage)
        productName.text = name
        productDescription.text = description



        // Quantity controls
        findViewById<Button>(R.id.decreaseQuantity).setOnClickListener {
            val currentQuantity = quantity.text.toString().toIntOrNull() ?: 0
            if (currentQuantity > 1) {
                quantity.setText((currentQuantity - 1).toString())
            }
        }

        findViewById<Button>(R.id.increaseQuantity).setOnClickListener {
            val currentQuantity = quantity.text.toString().toIntOrNull() ?: 0
            quantity.setText((currentQuantity + 1).toString())
        }


        // Assuming product details are passed via Intent or fetched from somewhere
        val productId = intent.getStringExtra("PRODUCT_ID") ?: "defaultProductId"
        val productPrice = intent.getStringExtra("PRICE") ?: "0.0"

        addToCartButton.setOnClickListener {
            // Handle add to cart action
            val sharedPreferences = getSharedPreferences("AppPrefs", MODE_PRIVATE)
            val userId = sharedPreferences.getString("userId", null)

            if (userId != null) {

                val quantityValue = quantity.text.toString().toIntOrNull() ?: 0
                val prices = productPrice.toDouble()

                // Log request data
                Log.d("AddToCartRequest", "userId: $userId, productId: $productId, quantity: $quantityValue, price: $prices")

                val request = AddToCartRequest(
                    userId = userId ?: "",
                    productId = productId,
                    quantity = quantityValue,
                    price = prices
                )

                RetrofitClient.instance.addToCart(request).enqueue(object : Callback<ApiResponse> {
                    override fun onResponse(call: Call<ApiResponse>, response: Response<ApiResponse>) {
                        if (response.isSuccessful) {
                            Toast.makeText(this@ProductDetailActivity, "Added to cart!", Toast.LENGTH_SHORT).show()
                        } else {
                            // Log response for debugging
                            Log.e("AddToCartError", "Failed to add to cart: ${response.errorBody()?.string()}")
                            Toast.makeText(this@ProductDetailActivity, "Failed to add to cart: ${response.message()}", Toast.LENGTH_SHORT).show()
                        }
                    }

                    override fun onFailure(call: Call<ApiResponse>, t: Throwable) {
                        Log.e("AddToCartFailure", "Error: ${t.message}", t)
                        Toast.makeText(this@ProductDetailActivity, "Error: ${t.message}", Toast.LENGTH_SHORT).show()
                    }
                })
            } else {
                Toast.makeText(this@ProductDetailActivity, "User not logged in", Toast.LENGTH_SHORT).show()
            }
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
                    val builder = AlertDialog.Builder(this@ProductDetailActivity)
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
                finish()
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
            else -> super.onOptionsItemSelected(item)
        }
    }
}
