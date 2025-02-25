package com.example.dev_dyanast

import android.widget.EditText

data class AddToCartRequest(
    val userId: String,
    val productId: String,
    val quantity: Int,
    val price: Double
)

data class ApiResponse(
    val message: String
)
