package com.smartmicrogrid.data.remote

import com.smartmicrogrid.utils.Constants
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitClient {

    private var jwtToken: String? = null

    private var currentBaseUrl: String = Constants.API_BASE_URL
    @Volatile
    private var cachedApiService: ApiService? = null

    fun setJwtToken(token: String?) {
        jwtToken = token
    }

    fun updateBaseUrl(newUrl: String) {
        val formatted = if (!newUrl.endsWith("/")) "$newUrl/" else newUrl
        if (currentBaseUrl != formatted) {
            currentBaseUrl = formatted
            cachedApiService = null
        }
    }

    fun getBaseUrl(): String = currentBaseUrl

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }

    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(loggingInterceptor)
        .addInterceptor { chain ->
            val original = chain.request()
            val requestBuilder = original.newBuilder()
            jwtToken?.let { token ->
                if (token.isNotEmpty()) {
                    requestBuilder.header("Authorization", "Bearer $token")
                }
            }
            chain.proceed(requestBuilder.build())
        }
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .build()

    val apiService: ApiService
        get() {
            return cachedApiService ?: synchronized(this) {
                cachedApiService ?: Retrofit.Builder()
                    .baseUrl(currentBaseUrl)
                    .client(okHttpClient)
                    .addConverterFactory(GsonConverterFactory.create())
                    .build()
                    .create(ApiService::class.java)
                    .also { cachedApiService = it }
            }
        }
}
