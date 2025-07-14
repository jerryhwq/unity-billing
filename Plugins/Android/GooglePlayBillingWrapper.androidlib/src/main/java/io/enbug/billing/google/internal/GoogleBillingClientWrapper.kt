package io.enbug.billing.google.internal

import android.app.Activity
import android.content.Context
import com.android.billingclient.api.AcknowledgePurchaseParams
import com.android.billingclient.api.BillingClient
import com.android.billingclient.api.BillingClient.BillingResponseCode
import com.android.billingclient.api.BillingClient.FeatureType
import com.android.billingclient.api.BillingClient.ProductType
import com.android.billingclient.api.BillingClient.SkuType
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
import com.android.billingclient.api.SkuDetails
import com.android.billingclient.api.SkuDetailsParams
import com.android.billingclient.api.SkuDetailsResult
import com.android.billingclient.api.acknowledgePurchase
import com.android.billingclient.api.consumePurchase
import com.android.billingclient.api.queryProductDetails
import com.android.billingclient.api.queryPurchasesAsync
import com.android.billingclient.api.querySkuDetails
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

    val inAppSkuDetailsMap: Map<String, SkuDetails>
        get() = inAppSkuDetailsMapInternal.toMap()

    private val inAppSkuDetailsMapInternal = mutableMapOf<String, SkuDetails>()

    val subsSkuDetailsMap: Map<String, SkuDetails>
        get() = subsSkuDetailsMapInternal.toMap()

    private val subsSkuDetailsMapInternal = mutableMapOf<String, SkuDetails>()

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

    suspend fun buyInAppSku(
        activity: Activity,
        sku: String,
        options: PurchaseOptions,
    ): BillingResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return connectionBillingResult
        }

        if (inAppSkuDetailsMap.containsKey(sku)) {
            val skuDetails = inAppSkuDetailsMap[sku]!!
            return buySkuInternal(
                activity,
                skuDetails,
                options,
            )
        }

        val skuDetailsResult = querySkuDetailsInternal(listOf(sku), SkuType.INAPP)
        val billingResult = skuDetailsResult.billingResult
        val details = skuDetailsResult.skuDetailsList
        if (billingResult.responseCode != BillingResponseCode.OK) {
            return billingResult
        }

        if (details.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        return buySkuInternal(
            activity,
            details[0],
            options,
        )
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

    suspend fun buySubsSku(
        activity: Activity,
        sku: String,
        options: PurchaseOptions,
    ): BillingResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return connectionBillingResult
        }

        if (subsSkuDetailsMap.containsKey(sku)) {
            val skuDetails = subsSkuDetailsMap[sku]!!
            return buySkuInternal(
                activity,
                skuDetails,
                options,
            )
        }

        val skuDetailsResult = querySkuDetailsInternal(listOf(sku), SkuType.SUBS)
        val billingResult = skuDetailsResult.billingResult
        val details = skuDetailsResult.skuDetailsList
        if (billingResult.responseCode != BillingResponseCode.OK) {
            return billingResult
        }

        if (details.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        return buySkuInternal(activity, details[0], options)
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

    suspend fun upgradeSubsSku(
        activity: Activity,
        sku: String,
        oldPurchaseToken: String,
        subscriptionReplacementMode: Int,
        options: PurchaseOptions,
    ): BillingResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return connectionBillingResult
        }

        if (subsSkuDetailsMapInternal.containsKey(sku)) {
            val skuDetails = subsSkuDetailsMapInternal[sku]!!
            return upgradeSubsSkuInternal(
                activity,
                skuDetails,
                oldPurchaseToken,
                subscriptionReplacementMode,
                options,
            )
        }

        val skuDetailsResult = querySkuDetailsInternal(listOf(sku), SkuType.SUBS)
        val billingResult = skuDetailsResult.billingResult
        val details = skuDetailsResult.skuDetailsList
        if (billingResult.responseCode != BillingResponseCode.OK) {
            return billingResult
        }

        if (details.isNullOrEmpty()) {
            val billingResult = BillingResult.newBuilder()
                .setResponseCode(BillingResponseCode.ITEM_UNAVAILABLE)
                .build()
            return billingResult
        }

        return upgradeSubsSkuInternal(
            activity,
            details[0],
            oldPurchaseToken,
            subscriptionReplacementMode,
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

    private suspend fun upgradeSubsSkuInternal(
        activity: Activity,
        skuDetails: SkuDetails,
        oldPurchaseToken: String,
        subscriptionReplacementMode: Int,
        options: PurchaseOptions,
    ): BillingResult {
        val subscriptionUpdateParams = BillingFlowParams.SubscriptionUpdateParams.newBuilder()
            .setOldPurchaseToken(oldPurchaseToken)
            .setSubscriptionReplacementMode(subscriptionReplacementMode)
            .build()

        val billingFlowParamsBuilder = BillingFlowParams.newBuilder()
            .setSubscriptionUpdateParams(subscriptionUpdateParams)
            .setSkuDetails(skuDetails)

        val obfuscatedAccountId = options.obfuscatedAccountId
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId)
        }

        val obfuscatedProfileId = options.obfuscatedProfileId
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId)
        }

        return withContext(Dispatchers.Main) {
            billingClient.launchBillingFlow(activity, billingFlowParamsBuilder.build())
        }
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

        return withContext(Dispatchers.Main) {
            billingClient.launchBillingFlow(activity, billingFlowParamsBuilder.build())
        }
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

    suspend fun querySkuDetails(
        @SkuType skuType: String,
        skus: List<String>,
    ): SkuDetailsResult {
        val connectionBillingResult = ensureConnection()
        if (connectionBillingResult.responseCode != BillingResponseCode.OK) {
            return SkuDetailsResult(
                connectionBillingResult,
                null,
            )
        }

        val skuDetailsResult = querySkuDetailsInternal(skus, skuType)
        val billingResult = skuDetailsResult.billingResult
        val skuDetails = skuDetailsResult.skuDetailsList
        return SkuDetailsResult(
            billingResult,
            skuDetails,
        )
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

        val productDetailsResult = queryProductDetailsInternal(productIds, productType)
        val billingResult = productDetailsResult.billingResult
        val productDetails = productDetailsResult.productDetailsList

        return ProductDetailsResult(
            billingResult,
            productDetails,
        )
    }

    private suspend fun querySkuDetailsInternal(
        skus: List<String>,
        @SkuType skuType: String,
    ): SkuDetailsResult {
        val skuDetailsParams = SkuDetailsParams.newBuilder()
            .setType(skuType)
            .setSkusList(skus)
            .build()

        val skuDetailsResult = withContext(Dispatchers.IO) {
            billingClient.querySkuDetails(skuDetailsParams)
        }

        val billingResult = skuDetailsResult.billingResult
        val skuDetailsList = skuDetailsResult.skuDetailsList

        if (billingResult.responseCode == BillingResponseCode.OK) {
            skuDetailsList?.forEach { skuDetails ->
                if (skuDetails.type == SkuType.INAPP) {
                    inAppSkuDetailsMapInternal[skuDetails.sku] = skuDetails
                } else if (skuDetails.type == SkuType.SUBS) {
                    subsSkuDetailsMapInternal[skuDetails.sku] = skuDetails
                }
            }
        }

        return skuDetailsResult
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

    private suspend fun buySkuInternal(
        activity: Activity,
        skuDetails: SkuDetails,
        options: PurchaseOptions,
    ): BillingResult {
        val billingFlowParamsBuilder = BillingFlowParams
            .newBuilder()
            .setSkuDetails(skuDetails)

        val obfuscatedAccountId = options.obfuscatedAccountId
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId)
        }

        val obfuscatedProfileId = options.obfuscatedProfileId
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId)
        }

        return withContext(Dispatchers.Main) {
            billingClient.launchBillingFlow(activity, billingFlowParamsBuilder.build())
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

        return withContext(Dispatchers.Main) {
            billingClient.launchBillingFlow(activity, billingFlowParamsBuilder.build())
        }
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

        return withContext(Dispatchers.Main) {
            billingClient.launchBillingFlow(activity, billingFlowParamsBuilder.build())
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