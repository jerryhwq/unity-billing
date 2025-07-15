package io.enbug.billing.google

import android.app.Activity
import com.android.billingclient.api.AcknowledgePurchaseResponseListener
import com.android.billingclient.api.BillingClient.ProductType
import com.android.billingclient.api.BillingResult
import com.android.billingclient.api.ConsumeResponseListener
import com.android.billingclient.api.ProductDetails
import com.android.billingclient.api.ProductDetailsResponseListener
import com.android.billingclient.api.Purchase
import com.android.billingclient.api.PurchasesResponseListener
import com.android.billingclient.api.PurchasesUpdatedListener
import com.android.billingclient.api.QueryProductDetailsResult
import com.android.billingclient.api.UnfetchedProduct
import io.enbug.billing.google.internal.GoogleBillingClientWrapper
import io.enbug.billing.google.internal.Helper
import kotlinx.coroutines.launch

@Suppress("unused")
class BillingClient(private val purchasesUpdatedListener: PurchasesUpdatedListener) :
    PurchasesUpdatedListener {
    private val billingClientWrapper = GoogleBillingClientWrapper(Helper.applicationContext, this)

    val productDetailsMap: Map<String, ProductDetails>
        get() = billingClientWrapper.inAppProductDetailsMap + billingClientWrapper.subsProductDetailsMap

    val isSubscriptionsSupported: Boolean
        get() = billingClientWrapper.isSubscriptionsSupported

    val isSubscriptionsUpdateSupported: Boolean
        get() = billingClientWrapper.isSubscriptionsUpdateSupported

    val isProductDetailsSupported: Boolean
        get() = billingClientWrapper.isProductDetailsSupported

    fun queryPurchases(@ProductType productType: String, callback: PurchasesResponseListener) {
        Helper.backgroundScope.launch {
            val purchaseResult = billingClientWrapper.queryPurchases(productType)
            val billingResult = purchaseResult.billingResult
            val purchaseList = purchaseResult.purchasesList
            callback.onQueryPurchasesResponse(billingResult, purchaseList)
        }
    }

    fun queryProductDetails(
        @ProductType productType: String,
        skus: List<String>,
        callback: ProductDetailsResponseListener,
    ) {
        Helper.backgroundScope.launch {
            val result = billingClientWrapper.queryProductDetails(productType, skus)
            val billingResult = result.billingResult
            val productDetails = result.productDetailsList ?: listOf()
            val ret = QueryProductDetailsResult.create(productDetails, emptyList<UnfetchedProduct>())
            callback.onProductDetailsResponse(billingResult, ret)
        }
    }

    fun buyInAppProduct(
        activity: Activity,
        productId: String,
        options: PurchaseOptions,
        listener: BillingResultListener?,
    ) {
        Helper.backgroundScope.launch {
            val result = billingClientWrapper.buyInAppProduct(
                activity,
                productId,
                options,
            )
            listener?.invoke(result)
        }
    }

    fun buySubsProduct(
        activity: Activity,
        productId: String,
        options: PurchaseOptions,
        listener: BillingResultListener?,
    ) {
        Helper.backgroundScope.launch {
            val result = billingClientWrapper.buySubsProduct(
                activity,
                productId,
                options,
            )
            listener?.invoke(result)
        }
    }

    fun upgradeSubsProduct(
        activity: Activity,
        productId: String,
        oldPurchaseToken: String,
        subscriptionReplacementMode: Int,
        options: PurchaseOptions,
        listener: BillingResultListener?,
    ) {
        Helper.backgroundScope.launch {
            val result = billingClientWrapper.upgradeSubsProduct(
                activity,
                productId,
                oldPurchaseToken,
                subscriptionReplacementMode,
                options,
            )
            listener?.invoke(result)
        }
    }

    fun consume(
        purchaseToken: String,
        callback: ConsumeResponseListener,
    ) {
        Helper.backgroundScope.launch {
            val result = billingClientWrapper.consume(purchaseToken)
            val billingResult = result.billingResult
            callback.onConsumeResponse(billingResult, purchaseToken)
        }
    }

    fun acknowledge(
        purchaseToken: String,
        callback: AcknowledgePurchaseResponseListener,
    ) {
        Helper.backgroundScope.launch {
            val billingResult = billingClientWrapper.acknowledge(purchaseToken)
            callback.onAcknowledgePurchaseResponse(billingResult)
        }
    }

    fun endConnection() {
        Helper.backgroundScope.launch {
            billingClientWrapper.endConnection()
        }
    }

    override fun onPurchasesUpdated(
        billingResult: BillingResult,
        purchaseList: List<Purchase>?,
    ) {
        purchasesUpdatedListener.onPurchasesUpdated(billingResult, purchaseList)
    }
}