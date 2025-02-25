package com.example.dev_dyanast

import android.content.Intent
import android.os.Bundle
import android.os.Handler
import androidx.appcompat.app.AppCompatActivity

class SplashActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_splash)


        Handler().postDelayed({
            // Start LoginActivity
            startActivity(Intent(this, LoginActivity::class.java))

            finish()
        }, 2000)
    }
}
