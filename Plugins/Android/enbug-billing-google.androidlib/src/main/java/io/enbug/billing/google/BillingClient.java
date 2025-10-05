package io.enbug.billing.google;

import android.app.Activity;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.android.billingclient.api.AcknowledgePurchaseResponseListener;
import com.android.billingclient.api.BillingResult;
import com.android.billingclient.api.ConsumeResponseListener;
import com.android.billingclient.api.ProductDetailsResponseListener;
import com.android.billingclient.api.Purchase;
import com.android.billingclient.api.PurchasesResponseListener;
import com.android.billingclient.api.PurchasesUpdatedListener;

import java.util.List;

public class BillingClient implements PurchasesUpdatedListener {
    @NonNull
    private final PurchasesUpdatedListener purchasesUpdatedListener;
    @NonNull
    private final GoogleBillingClientWrapper billingClientWrapper;

    public BillingClient(@NonNull PurchasesUpdatedListener purchasesUpdatedListener) {
        this.purchasesUpdatedListener = purchasesUpdatedListener;
        billingClientWrapper = new GoogleBillingClientWrapper(Helper.getContext(), this);
    }

    public boolean isProductDetailsSupported() {
        return billingClientWrapper.isProductDetailsSupported();
    }

    public boolean isSubscriptionsSupported() {
        return billingClientWrapper.isSubscriptionsSupported();
    }

    public boolean isSubscriptionsUpdateSupported() {
        return billingClientWrapper.isSubscriptionsUpdateSupported();
    }

    public void queryProductDetails(@NonNull String productType, @NonNull List<String> skus, ProductDetailsResponseListener callback) {
        Helper.getBackgroundHandler().post(() -> {
            billingClientWrapper.queryProductDetails(productType, skus, callback);
        });
    }

    public void buyInAppProduct(@NonNull Activity activity, @NonNull String productId, @NonNull PurchaseOptions options, @NonNull BillingResultListener callback) {
        Helper.getBackgroundHandler().post(() -> {
            billingClientWrapper.buyInAppProducts(activity, productId, options, callback);
        });
    }

    public void buySubsProduct(@NonNull Activity activity, @NonNull String productId, @NonNull PurchaseOptions options, @NonNull BillingResultListener callback) {
        Helper.getBackgroundHandler().post(() -> {
            billingClientWrapper.buySubsProducts(activity, productId, options, callback);
        });
    }

    public void upgradeSubsProduct(@NonNull Activity activity, @NonNull String productId, @NonNull String oldPurchaseToken, int subscriptionReplacementMode, @NonNull PurchaseOptions options, BillingResultListener callback) {
        Helper.getBackgroundHandler().post(() -> {
            billingClientWrapper.upgradeSubsProduct(activity, productId, oldPurchaseToken, subscriptionReplacementMode, options, callback);
        });
    }

    public void consume(@NonNull String purchaseToken, @NonNull ConsumeResponseListener callback) {
        Helper.getBackgroundHandler().post(() -> {
            billingClientWrapper.consume(purchaseToken, callback);
        });
    }

    public void acknowledge(@NonNull String purchaseToken, @NonNull AcknowledgePurchaseResponseListener callback) {
        Helper.getBackgroundHandler().post(() -> {
            billingClientWrapper.acknowledge(purchaseToken, callback);
        });
    }

    public void queryPurchases(@NonNull String productType, @NonNull PurchasesResponseListener callback) {
        Helper.getBackgroundHandler().post(() -> {
            billingClientWrapper.queryPurchases(productType, callback);
        });
    }

    @Override
    public void onPurchasesUpdated(@NonNull BillingResult billingResult, @Nullable List<Purchase> list) {
        purchasesUpdatedListener.onPurchasesUpdated(billingResult, list);
    }

    public void endConnection() {
        Helper.getBackgroundHandler().post(billingClientWrapper::endConnection);
    }
}
