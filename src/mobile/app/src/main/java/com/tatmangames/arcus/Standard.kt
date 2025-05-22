package com.tatmangames.arcus

import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material3.Icon
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.res.vectorResource
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.tatmangames.arcus.ui.theme.ArcusMobileTheme

// The standard activity is the main screen users will see after the splash screen.
// it contains all of the access to functionality and files Arcus Mobile has to offer.
class StandardActivity: ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            ArcusMobileTheme {
                AppContent(
                    onSettingsClick = {
                        startActivity(Intent(this, SettingsActivity::class.java))
                    }
                )
            }
        }
    }

    @Composable
    fun AppContent(onSettingsClick: () -> Unit = {}) {
        val context = this
        Scaffold(
            modifier = Modifier.fillMaxSize(),
            topBar = {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 30.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = stringResource(id = R.string.app_name),
                        fontSize = 28.sp,
                        modifier = Modifier.weight(1f)
                    )
                    Icon(
                        imageVector = Icons.Default.Menu,
                        contentDescription = "Settings",
                        modifier = Modifier
                            .size(32.dp)
                            .clickable { onSettingsClick() }
                    )
                }
            }
        ) { innerPadding ->
            // Replace the placeholder text with a grid of 4 boxes
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxSize(),
                    verticalArrangement = Arrangement.SpaceBetween
                ) {
                    // Main content (grid)
                    GridButtons(
                        onListFilesClick = {
                            Toast.makeText(context, "List Files Clicked!", Toast.LENGTH_SHORT).show()
                        }
                    )
                    // Copyright at the bottom
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(bottom = 16.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = stringResource(id = R.string.copyright),
                            fontSize = 12.sp,
                            color = Color.Gray
                        )
                    }
                }
            }
        }
    }

    @Composable
    fun GridButtons(onListFilesClick: () -> Unit = {}) {
        val buttonModifier = Modifier
            .padding(12.dp)
            .fillMaxWidth()
            .aspectRatio(1f)
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                GridButton(
                    imageRes = R.drawable.ic_list_files, // You need to add this drawable
                    text = stringResource(id = R.string.list_files),
                    onClick = onListFilesClick
                )
                GridButton(
                    imageRes = R.drawable.ic_placeholder, // You need to add this drawable
                    text = stringResource(id = R.string.placeholder)
                )
            }
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                GridButton(
                    imageRes = R.drawable.ic_placeholder,
                    text = stringResource(id = R.string.placeholder)
                )
                GridButton(
                    imageRes = R.drawable.ic_placeholder,
                    text = stringResource(id = R.string.placeholder)
                )
            }
        }
    }

    @Composable
    fun GridButton(imageRes: Int, text: String, onClick: (() -> Unit)? = null) {
        Surface(
            modifier = Modifier
                .padding(8.dp)
                .size(140.dp)
                .clickable { onClick?.invoke() },
            shape = RoundedCornerShape(16.dp),
            color = Color(0xFFE0E0E0),
            shadowElevation = 6.dp
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                verticalArrangement = Arrangement.Center,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Image(
                    painter = painterResource(id = imageRes),
                    contentDescription = null,
                    modifier = Modifier.size(48.dp)
                )
                Spacer(modifier = Modifier.height(12.dp))
                Text(text = text, fontSize = 16.sp)
            }
        }
    }

    @Preview(showBackground = true)
    @Composable
    fun Preview() {
        ArcusMobileTheme {
            AppContent()
        }
    }
}
