package com.smartmicrogrid.data.remote

import com.smartmicrogrid.utils.Constants
import com.smartmicrogrid.utils.SessionManager
import kotlinx.coroutines.runBlocking
import java.io.IOException
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitClient {

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }

    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(loggingInterceptor)
        .addInterceptor { chain ->
            // OkHttp runs this on its worker thread, including after process recreation.
            try { runBlocking { SessionManager.awaitReady() } }
            catch (error: Exception) { throw IOException("Unable to restore saved login", error) }
            val original = chain.request()
            val requestBuilder = original.newBuilder()
            SessionManager.getToken()?.let { token ->
                if (token.isNotEmpty()) {
                    requestBuilder.header("Authorization", "Bearer $token")
                }
            }
            chain.proceed(requestBuilder.build())
        }
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .build()

    val apiService: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl(Constants.API_BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}
