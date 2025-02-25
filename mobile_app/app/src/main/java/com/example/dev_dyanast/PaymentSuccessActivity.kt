package com.example.dev_dyanast

import android.annotation.SuppressLint
import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity

class PaymentSuccessActivity : AppCompatActivity() {

    @SuppressLint("MissingInflatedId")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_payment_success)

        // Retrieve order number and user name from intent
        val orderNumber = intent.getStringExtra("ORDER_NUMBER") ?: "Unknown"
        val userName = intent.getStringExtra("USER_NAME") ?: "Customer"

        // Find the TextView and set the success message with order number and user name
        val successMessage = findViewById<TextView>(R.id.textViewOrderDetails)
        successMessage.text = "Order number: $orderNumber\nName: $userName\nThank you! Your order has been received and will be ready in 15 minutes."

        // Set up Go Back button
        val goBackButton = findViewById<Button>(R.id.buttonGoBack)
        goBackButton.setOnClickListener {
            finish() // Finish this activity
            val intent = Intent(this, CartActivity::class.java)
            startActivity(intent) // Return to CartActivity
        }
    }
}
