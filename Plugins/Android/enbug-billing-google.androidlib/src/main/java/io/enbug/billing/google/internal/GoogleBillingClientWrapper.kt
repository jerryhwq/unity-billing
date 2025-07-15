package io.enbug.billing.google.internal

import android.app.Activity
import android.content.Context
import com.android.billingclient.api.AcknowledgePurchaseParams
import com.android.billingclient.api.BillingClient
import com.android.billingclient.api.BillingClient.BillingResponseCode
import com.android.billingclient.api.BillingClient.FeatureType
import com.android.billingclient.api.BillingClient.ProductType
import com.android.billingclient.api.BillingClientStateListener
import com.android.billingclient.api.BillingFlowParams
import com.android.billingclient.api.BillingFlowParams.ProductDetailsParams
import com.android.billingclient.api.BillingResult
import com.android.billingclient.api.ConsumeParams
import com.android.billingclient.api.ConsumeResult
import com.android.billingclient.api.PendingPurchasesParams
import com.android.billingclient.api.ProductDetails
import com.android.billingclient.api.ProductDetailsResult
import com.android.billingclient.api.Purchase
import com.android.billingclient.api.PurchasesResult
import com.android.billingclient.api.PurchasesUpdatedListener
import com.android.billingclient.api.QueryProductDetailsParams
import com.android.billingclient.api.QueryPurchasesParams
import com.android.billingclient.api.acknowledgePurchase
import com.android.billingclient.api.consumePurchase
import com.android.billingclient.api.queryProductDetails
import com.android.billingclient.api.queryPurchasesAsync
import io.enbug.billing.google.PurchaseOptions
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlin.coroutines.Continuation
import kotlin.coroutines.resume
import kotlin.coroutines.suspendCoroutine

internal class GoogleBillingClientWrapper(
    context: Context,
    private val purchasesUpdatedListener: PurchasesUpdatedListener,
) : PurchasesUpdatedListener {
    private val billingClient: BillingClient =
        BillingClient.newBuilder(context.applicationContext ?: context)
            .enablePendingPurchases(
                PendingPurchasesParams.newBuilder()
                    .enableOneTimeProducts()
                    .build()
            )
            .enableAutoServiceReconnection()
            .setListener(this)
            .build()

    private var connected = false
    private var connecting = false

    private val connectionCoroutines: MutableList<Continuation<BillingResult>> = mutableListOf()

    val inAppProductDetailsMap: Map<String, ProductDetails>
        get() = inAppProductDetailsMapInternal.toMap()

    private val inAppProductDetailsMapInternal = mutableMapOf<String, ProductDetails>()

    val subsProductDetailsMap: Map<String, ProductDetails>
        get() = subsProductDetailsMapInternal.toMap()

    private val subsProductDetailsMapInternal = mutableMapOf<String, ProductDetails>()

    val isSubscriptionsUpdateSupported: Boolean
        get() {
            val billingResult = billingClient.isFeatureSupported(FeatureType.SUBSCRIPTIONS_UPDATE)
            return billingResult.responseCode != BillingResponseCode.FEATURE_NOT_SUPPORTED
        }

    val isSubscriptionsSupported: Boolean
        get() {
            val billingResult = billingClient.isFeatureSupported(FeatureType.SUBSCRIPTIONS)
            return billingResult.responseCode != BillingResponseCode.FEATURE_NOT_SUPPORTED
        }

    val isProductDetailsSupported: Boolean
        get() {
            val billingResult = billingClient.isFeatureSupported(FeatureType.PRODUCT_DETAILS)
            return billingResult.responseCode != BillingResponseCode.FEATURE_NOT_SUPPORTED
        }

    suspend fun buyInAppProduct(
        activity: Activity,
        productId: String,
        options: PurchaseOptions,
    ): BillingResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return connectionBillingResult
        }

        if (inAppProductDetailsMapInternal.containsKey(productId)) {
            val productDetails = inAppProductDetailsMapInternal[productId]!!
            return buyInAppProductInternal(
                activity,
                productDetails,
                options,
            )
        }

        val productDetailsResult = queryProductDetailsInternal(listOf(productId), ProductType.INAPP)
        val billingResult = productDetailsResult.billingResult
        val details = productDetailsResult.productDetailsList

        if (billingResult.responseCode != BillingResponseCode.OK) {
            return billingResult
        }

        if (details.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        return buyInAppProductInternal(
            activity,
            details[0],
            options,
        )
    }

    suspend fun buySubsProduct(
        activity: Activity,
        productId: String,
        options: PurchaseOptions,
    ): BillingResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return connectionBillingResult
        }

        if (subsProductDetailsMapInternal.containsKey(productId)) {
            val productDetails = subsProductDetailsMapInternal[productId]!!
            return buySubsProductInternal(
                activity,
                productDetails,
                options,
            )
        }

        val productDetailsResult = queryProductDetailsInternal(listOf(productId), ProductType.SUBS)
        val billingResult = productDetailsResult.billingResult
        val details = productDetailsResult.productDetailsList
        if (billingResult.responseCode != BillingResponseCode.OK) {
            return billingResult
        }

        if (details.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        return buySubsProductInternal(
            activity,
            details[0],
            options,
        )
    }

    suspend fun upgradeSubsProduct(
        activity: Activity,
        productId: String,
        oldPurchaseToken: String,
        subscriptionReplacementMode: Int,
        options: PurchaseOptions,
    ): BillingResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return connectionBillingResult
        }

        if (subsProductDetailsMapInternal.containsKey(productId)) {
            val productDetails = subsProductDetailsMapInternal[productId]!!
            return upgradeSubsProductInternal(
                activity,
                productDetails,
                oldPurchaseToken,
                subscriptionReplacementMode,
                options,
            )
        }

        val productDetailsResult = queryProductDetailsInternal(listOf(productId), ProductType.SUBS)
        val billingResult = productDetailsResult.billingResult
        val details = productDetailsResult.productDetailsList
        if (billingResult.responseCode != BillingResponseCode.OK) {
            return billingResult
        }

        if (details.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        if (details.isEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        return upgradeSubsProductInternal(
            activity,
            details[0],
            oldPurchaseToken,
            subscriptionReplacementMode,
            options,
        )
    }

    private suspend fun upgradeSubsProductInternal(
        activity: Activity,
        productDetails: ProductDetails,
        oldPurchaseToken: String,
        subscriptionReplacementMode: Int,
        options: PurchaseOptions,
    ): BillingResult {
        val subscriptionOfferDetails = productDetails.subscriptionOfferDetails
        if (subscriptionOfferDetails.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        val subscriptionUpdateParams = BillingFlowParams.SubscriptionUpdateParams.newBuilder()
            .setOldPurchaseToken(oldPurchaseToken)
            .setSubscriptionReplacementMode(subscriptionReplacementMode)
            .build()

        val productDetailsParams = ProductDetailsParams.newBuilder()
            .setProductDetails(productDetails)
            .setOfferToken(subscriptionOfferDetails[0].offerToken)
            .build()

        val billingFlowParamsBuilder = BillingFlowParams.newBuilder()
            .setSubscriptionUpdateParams(subscriptionUpdateParams)
            .setProductDetailsParamsList(listOf(productDetailsParams))

        val obfuscatedAccountId = options.obfuscatedAccountId
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId)
        }

        val obfuscatedProfileId = options.obfuscatedProfileId
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId)
        }

        return launchBillingFlow(activity, billingFlowParamsBuilder.build())
    }

    suspend fun consume(purchaseToken: String): ConsumeResult {
        val billingResult = ensureConnection()
        if (billingResult.responseCode != BillingResponseCode.OK) {
            return ConsumeResult(billingResult, purchaseToken)
        }

        val consumeParams = ConsumeParams.newBuilder()
            .setPurchaseToken(purchaseToken)
            .build()
        return withContext(Dispatchers.IO) {
            billingClient.consumePurchase(consumeParams)
        }
    }

    suspend fun acknowledge(
        purchaseToken: String
    ): BillingResult {
        val billingResult = ensureConnection()
        if (billingResult.responseCode != BillingResponseCode.OK) {
            return billingResult
        }

        val acknowledgeParams = AcknowledgePurchaseParams.newBuilder()
            .setPurchaseToken(purchaseToken)
            .build()
        return withContext(Dispatchers.IO) {
            billingClient.acknowledgePurchase(acknowledgeParams)
        }
    }

    suspend fun queryProductDetails(
        @ProductType productType: String,
        productIds: List<String>,
    ): ProductDetailsResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return ProductDetailsResult(
                connectionBillingResult,
                null,
            )
        }

        return queryProductDetailsInternal(productIds, productType)
    }

    private suspend fun queryProductDetailsInternal(
        productIds: List<String>,
        @ProductType productType: String,
    ): ProductDetailsResult {
        val productsToQuery = productIds.map { productId ->
            return@map QueryProductDetailsParams.Product.newBuilder()
                .setProductId(productId)
                .setProductType(productType)
                .build()
        }

        val params = QueryProductDetailsParams.newBuilder()
            .setProductList(productsToQuery)
            .build()

        val productDetailsResult = withContext(Dispatchers.IO) {
            billingClient.queryProductDetails(params)
        }

        val billingResult = productDetailsResult.billingResult
        val productDetailsList = productDetailsResult.productDetailsList

        if (billingResult.responseCode != BillingResponseCode.OK) {
            return ProductDetailsResult(
                billingResult,
                listOf(),
            )
        }

        productDetailsList?.forEach { productDetails ->
            val productId = productDetails.productId
            val productType = productDetails.productType
            if (productType == ProductType.SUBS) {
                subsProductDetailsMapInternal[productId] = productDetails
            } else if (productType == ProductType.INAPP) {
                inAppProductDetailsMapInternal[productId] = productDetails
            }
        }

        return productDetailsResult
    }

    suspend fun queryPurchases(productType: String): PurchasesResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return PurchasesResult(connectionBillingResult, listOf())
        }

        val queryPurchaseParam = QueryPurchasesParams.newBuilder()
            .setProductType(productType)
            .build()
        return withContext(Dispatchers.IO) {
            billingClient.queryPurchasesAsync(queryPurchaseParam)
        }
    }

    private suspend fun buyInAppProductInternal(
        activity: Activity,
        productDetails: ProductDetails,
        options: PurchaseOptions,
    ): BillingResult {
        val productDetailsParams = listOf(
            ProductDetailsParams.newBuilder()
                .setProductDetails(productDetails)
                .build()
        )

        val billingFlowParamsBuilder = BillingFlowParams.newBuilder()
            .setProductDetailsParamsList(productDetailsParams)

        val obfuscatedAccountId = options.obfuscatedAccountId
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId)
        }

        val obfuscatedProfileId = options.obfuscatedProfileId
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId)
        }

        return launchBillingFlow(activity, billingFlowParamsBuilder.build())
    }

    private suspend fun buySubsProductInternal(
        activity: Activity,
        productDetails: ProductDetails,
        options: PurchaseOptions,
    ): BillingResult {
        val subscriptionOfferDetails = productDetails.subscriptionOfferDetails
        if (subscriptionOfferDetails.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        val productDetailsParamsList = listOf(
            ProductDetailsParams.newBuilder()
                .setProductDetails(productDetails)
                .setOfferToken(subscriptionOfferDetails[0].offerToken)
                .build()
        )

        val billingFlowParamsBuilder = BillingFlowParams.newBuilder()
            .setProductDetailsParamsList(productDetailsParamsList)

        val obfuscatedAccountId = options.obfuscatedAccountId
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId)
        }

        val obfuscatedProfileId = options.obfuscatedProfileId
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId)
        }

        return launchBillingFlow(activity, billingFlowParamsBuilder.build())
    }

    private suspend fun launchBillingFlow(activity: Activity, params: BillingFlowParams) : BillingResult {
        return withContext(Dispatchers.Main) {
            billingClient.launchBillingFlow(activity, params)
        }
    }

    private suspend fun ensureConnection(): BillingResult {
        if (connected) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.OK)
                .build()

            return billingResult
        }

        return suspendCoroutine { continuation ->
            connectionCoroutines.add(continuation)
            if (connecting) {
                return@suspendCoroutine
            }

            connecting = true
            billingClient.startConnection(object : BillingClientStateListener {
                override fun onBillingServiceDisconnected() {
                    Helper.backgroundScope.launch {
                        connected = false
                    }
                }

                override fun onBillingSetupFinished(billingResult: BillingResult) {
                    Helper.backgroundScope.launch {
                        connecting = false
                        connected = billingResult.responseCode == BillingResponseCode.OK
                        connectionCoroutines.forEach {
                            it.resume(billingResult)
                        }
                        connectionCoroutines.clear()
                    }
                }
            })
        }
    }

    override fun onPurchasesUpdated(billingResult: BillingResult, purchaseList: List<Purchase>?) {
        purchasesUpdatedListener.onPurchasesUpdated(
            billingResult,
            purchaseList,
        )
    }

    suspend fun endConnection() {
        if (billingClient.isReady) {
            billingClient.endConnection()
        }
    }
}