package com.tatmangames.arcus

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.foundation.layout.Column
import androidx.compose.ui.Modifier
import androidx.compose.ui.Alignment
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.ui.tooling.preview.Preview
import com.tatmangames.arcus.ui.theme.ArcusMobileTheme
import androidx.compose.ui.res.stringResource
import androidx.compose.foundation.Image
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import android.content.Intent
import android.os.Handler
import android.os.Looper

class SplashActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            ArcusMobileTheme {
                AppContent()
            }
        }
        Handler(Looper.getMainLooper()).postDelayed({
            startActivity(Intent(this, StandardActivity::class.java))
            finish()
        }, 5000)
    }
}

@Composable
fun AppTitle(modifier: Modifier = Modifier) {
    Text(
        text = stringResource(id = R.string.app_name),
        modifier = modifier.padding(vertical = 32.dp), // More padding for title
        fontSize = 28.sp // Optional: make title larger
    )
}

@Composable
fun Copyright(modifier: Modifier = Modifier) {
    Text(
        text = stringResource(id = R.string.copyright),
        modifier = modifier.padding(vertical = 4.dp),
        fontSize = 12.sp // Smaller text
    )
}

@Composable
fun Version(modifier: Modifier = Modifier) {
    Text(
        text = stringResource(id = R.string.version),
        modifier = modifier.padding(vertical = 4.dp),
        fontSize = 12.sp // Smaller text
    )
}

@Composable
fun AppContent() {
    Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center // Center all content vertically
        ) {
            // App icon/image at the top
            Image(
                painter = painterResource(id = R.drawable.ic_launcher_foreground), // Replace with your image resource
                contentDescription = "App Icon",
                modifier = Modifier
                    .padding(bottom = 24.dp)
                    .align(Alignment.CenterHorizontally)
            )
            AppTitle()
            Version()
            Copyright()
        }
    }
}

@Preview(showBackground = true)
@Composable
fun SplashPreview() {
    ArcusMobileTheme {
        AppContent()
    }
}
