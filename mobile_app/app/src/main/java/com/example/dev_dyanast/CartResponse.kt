// Data class for individual cart item
data class CartItem(
    val id: Int,
    val productId: String,
    val name: String,
    val imageUrl: String,
    val quantity: Int,
    val price: Double
)

// Data class for the response from the cart API
data class CartResponse(
    val items: List<CartItem>,
    val totalPrice: Double
)
