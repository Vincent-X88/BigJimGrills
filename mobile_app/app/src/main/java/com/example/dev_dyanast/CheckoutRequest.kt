package com.example.dev_dyanast

data class CheckoutRequest(
    val userId: String
)

data class CheckoutResponse(
    val id: String,
    val message: String,
    val totalPrice: Double,
    val items: List<Item>, // Change to List<Item> if items is an array of objects
    val orderNumber: String,
    val userName: String,
)

data class Item(
    val product_id: Int,
    val quantity: Int
)

