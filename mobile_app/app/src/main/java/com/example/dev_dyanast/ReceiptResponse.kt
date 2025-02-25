package com.example.dev_dyanast

data class ReceiptResponse(
    val receiptId: String,
    val totalPrice: Float,
    val items: List<ReceiptItem>
)

data class ReceiptItem(
    val name: String,
    val quantity: Int,
    val price: Float
)
