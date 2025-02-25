package com.example.dev_dyanast

import CartResponse
import retrofit2.Call
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Headers
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

data class User(val first_name: String, val last_name: String, val email: String, val password: String, val phone_number: String)

interface ApiService {
    @POST("/register")
    fun registerUser(@Body user: User): Call<Map<String, String>>

    @POST("/login")
    fun loginUser(@Body credentials: Map<String, String>): Call<Map<String, String>>

    @GET("featured-items")
    suspend fun getFeaturedItems(): List<FeaturedItem>

    @POST("/add_to_cart")
    fun addToCart(@Body request: AddToCartRequest): Call<ApiResponse>

    @GET("/cart")
    fun getCartItems(@Query("userId") userId: String): Call<CartResponse>

    @DELETE("/cart/{item_id}")
    fun removeFromCart(@Path("item_id") itemId: Int): Call<ApiResponse>

    @GET("/categories")
    fun getCategories(): Call<List<Category>>

    @GET("/menu_items")
    fun getMenuItems(@Query("category") category: String): Call<List<MenuItem>>

    @PUT("/update_profile")
    fun updateProfile(@Body profileUpdate: ProfileUpdate): Call<ApiResponse>

    @GET("/get_profile/{userId}")
    fun getUserProfile(@Path("userId") userId: Int): Call<UserProfileResponse>

    @GET("/get_receipt/{receiptId}")
    fun getReceipt(@Path("receiptId") receiptId: String): Call<ReceiptResponse>

    @POST("/complete_checkout")
    @Headers("Content-Type: application/json")
    fun completeCheckout(@Body request: CheckoutRequest): Call<CheckoutResponse>


        @GET("/notifications")
        suspend fun getNotifications(@Query("userId") userId: String): List<Notification>





}
