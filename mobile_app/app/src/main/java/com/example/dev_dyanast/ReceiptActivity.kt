package com.example.dev_dyanast

import android.content.Intent
import android.os.Bundle
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity

class ReceiptActivity : AppCompatActivity() {

    private lateinit var receiptIdTextView: TextView
    private lateinit var totalPriceTextView: TextView
    private lateinit var itemsTextView: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_receipt)

        receiptIdTextView = findViewById(R.id.receiptId)
        totalPriceTextView = findViewById(R.id.totalPrice)
        itemsTextView = findViewById(R.id.items)

        val receiptId = intent.getStringExtra("receiptId") ?: "N/A"
        val totalPrice = intent.getDoubleExtra("totalPrice", 0.0)
        val items = intent.getStringExtra("items") ?: "No items"

        receiptIdTextView.text = "Receipt ID: $receiptId"
        totalPriceTextView.text = "Total Price: R$totalPrice"
        itemsTextView.text = "Items: $items"
    }
}
