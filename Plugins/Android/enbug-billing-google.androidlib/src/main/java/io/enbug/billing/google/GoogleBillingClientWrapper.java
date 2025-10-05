package io.enbug.billing.google;

import android.app.Activity;
import android.content.Context;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.util.Consumer;

import com.android.billingclient.api.AcknowledgePurchaseParams;
import com.android.billingclient.api.AcknowledgePurchaseResponseListener;
import com.android.billingclient.api.BillingClient;
import com.android.billingclient.api.BillingClientStateListener;
import com.android.billingclient.api.BillingFlowParams;
import com.android.billingclient.api.BillingResult;
import com.android.billingclient.api.ConsumeParams;
import com.android.billingclient.api.ConsumeResponseListener;
import com.android.billingclient.api.PendingPurchasesParams;
import com.android.billingclient.api.ProductDetails;
import com.android.billingclient.api.ProductDetailsResponseListener;
import com.android.billingclient.api.Purchase;
import com.android.billingclient.api.PurchasesResponseListener;
import com.android.billingclient.api.PurchasesUpdatedListener;
import com.android.billingclient.api.QueryProductDetailsParams;
import com.android.billingclient.api.QueryProductDetailsResult;
import com.android.billingclient.api.QueryPurchasesParams;

import java.util.ArrayList;
import java.util.List;

class GoogleBillingClientWrapper implements PurchasesUpdatedListener {
    @NonNull
    private final PurchasesUpdatedListener purchasesUpdatedListener;
    @NonNull
    private final BillingClient billingClient;
    private final List<Consumer<BillingResult>> connectionListeners = new ArrayList<>();
    private boolean connected;
    private boolean connecting;

    private final BillingClientStateListener clientStateListener = new BillingClientStateListener() {
        @Override
        public void onBillingServiceDisconnected() {
            Helper.getBackgroundHandler().post(() -> connected = false);
        }

        @Override
        public void onBillingSetupFinished(@NonNull BillingResult billingResult) {
            Helper.getBackgroundHandler().post(() -> {
                connecting = false;
                connected = billingResult.getResponseCode() == BillingClient.BillingResponseCode.OK;
                for (Consumer<BillingResult> listener : connectionListeners) {
                    listener.accept(billingResult);
                }
                connectionListeners.clear();
            });
        }
    };

    public boolean isProductDetailsSupported() {
        BillingResult billingResult = billingClient.isFeatureSupported(BillingClient.FeatureType.PRODUCT_DETAILS);
        return billingResult.getResponseCode() != BillingClient.BillingResponseCode.FEATURE_NOT_SUPPORTED;
    }

    public boolean isSubscriptionsSupported() {
        BillingResult billingResult = billingClient.isFeatureSupported(BillingClient.FeatureType.SUBSCRIPTIONS);
        return billingResult.getResponseCode() != BillingClient.BillingResponseCode.FEATURE_NOT_SUPPORTED;
    }

    public boolean isSubscriptionsUpdateSupported() {
        BillingResult billingResult = billingClient.isFeatureSupported(BillingClient.FeatureType.SUBSCRIPTIONS_UPDATE);
        return billingResult.getResponseCode() != BillingClient.BillingResponseCode.FEATURE_NOT_SUPPORTED;
    }

    public GoogleBillingClientWrapper(@NonNull Context context, @NonNull PurchasesUpdatedListener purchasesUpdatedListener) {
        this.purchasesUpdatedListener = purchasesUpdatedListener;
        billingClient = BillingClient.newBuilder(context)
                .enablePendingPurchases(PendingPurchasesParams.newBuilder()
                        .enableOneTimeProducts()
                        .build())
                .enableAutoServiceReconnection()
                .setListener(this)
                .build();
    }

    public void queryProductDetails(@NonNull String productType, @NonNull List<String> productIds, @NonNull ProductDetailsResponseListener callback) {
        ensureConnection(result -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                callback.onProductDetailsResponse(result, QueryProductDetailsResult.create(new ArrayList<>(), new ArrayList<>()));
                return;
            }

            queryProductDetailsInternal(productIds, productType, callback);
        });
    }

    public void buyInAppProducts(@NonNull Activity activity, @NonNull String productId, @NonNull PurchaseOptions options, @NonNull Consumer<BillingResult> callback) {
        ensureConnection(result -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                callback.accept(result);
                return;
            }

            List<String> productIds = new ArrayList<>();
            productIds.add(productId);
            queryProductDetailsInternal(productIds, BillingClient.ProductType.INAPP, (queryBillingResult, queryProductDetailsResult) -> {
                if (queryBillingResult.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                    callback.accept(queryBillingResult);
                    return;
                }

                List<ProductDetails> productDetailsList = queryProductDetailsResult.getProductDetailsList();
                if (productDetailsList.isEmpty()) {
                    callback.accept(BillingResult.newBuilder()
                            .setResponseCode(BillingClient.BillingResponseCode.ITEM_UNAVAILABLE)
                            .build());
                    return;
                }

                buyInAppProductInternal(activity, productDetailsList.get(0), options, callback);
            });
        });
    }

    private void buyInAppProductInternal(@NonNull Activity activity, @NonNull ProductDetails productDetails, @NonNull PurchaseOptions options, @NonNull Consumer<BillingResult> callback) {
        List<BillingFlowParams.ProductDetailsParams> productDetailsParams = new ArrayList<>();
        productDetailsParams.add(BillingFlowParams.ProductDetailsParams.newBuilder()
                .setProductDetails(productDetails)
                .build());

        BillingFlowParams.Builder billingFlowParamsBuilder = BillingFlowParams.newBuilder()
                .setProductDetailsParamsList(productDetailsParams);

        String obfuscatedAccountId = options.getObfuscatedAccountId();
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId);
        }

        String obfuscatedProfileId = options.getObfuscatedProfileId();
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId);
        }

        launchBillingFlow(activity, billingFlowParamsBuilder.build(), callback);
    }

    public void buySubsProducts(@NonNull Activity activity, @NonNull String productId, @NonNull PurchaseOptions options, @NonNull Consumer<BillingResult> callback) {
        ensureConnection(result -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                callback.accept(result);
                return;
            }

            List<String> productIds = new ArrayList<>();
            productIds.add(productId);
            queryProductDetailsInternal(productIds, BillingClient.ProductType.SUBS, (queryBillingResult, queryProductDetailsResult) -> {
                if (queryBillingResult.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                    callback.accept(queryBillingResult);
                    return;
                }

                List<ProductDetails> productDetailsList = queryProductDetailsResult.getProductDetailsList();
                if (productDetailsList.isEmpty()) {
                    callback.accept(BillingResult.newBuilder()
                            .setResponseCode(BillingClient.BillingResponseCode.ITEM_UNAVAILABLE)
                            .build());
                    return;
                }

                buySubsProductInternal(activity, productDetailsList.get(0), options, callback);
            });
        });
    }

    private void buySubsProductInternal(@NonNull Activity activity, @NonNull ProductDetails productDetails, @NonNull PurchaseOptions options, @NonNull Consumer<BillingResult> callback) {
        List<ProductDetails.SubscriptionOfferDetails> subscriptionOfferDetails = productDetails.getSubscriptionOfferDetails();
        if (subscriptionOfferDetails == null || subscriptionOfferDetails.isEmpty()) {
            callback.accept(BillingResult.newBuilder()
                    .setResponseCode(BillingClient.BillingResponseCode.ITEM_UNAVAILABLE)
                    .build());
            return;
        }

        List<BillingFlowParams.ProductDetailsParams> productDetailsParams = new ArrayList<>();
        productDetailsParams.add(BillingFlowParams.ProductDetailsParams.newBuilder()
                .setProductDetails(productDetails)
                .setOfferToken(subscriptionOfferDetails.get(0).getOfferToken())
                .build());

        BillingFlowParams.Builder billingFlowParamsBuilder = BillingFlowParams.newBuilder()
                .setProductDetailsParamsList(productDetailsParams);

        String obfuscatedAccountId = options.getObfuscatedAccountId();
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId);
        }

        String obfuscatedProfileId = options.getObfuscatedProfileId();
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId);
        }

        launchBillingFlow(activity, billingFlowParamsBuilder.build(), callback);
    }

    public void upgradeSubsProduct(@NonNull Activity activity, @NonNull String productId, @NonNull String oldPurchaseToken, int subscriptionReplacementMode, @NonNull PurchaseOptions options, @NonNull Consumer<BillingResult> callback) {
        ensureConnection(result -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                callback.accept(result);
                return;
            }

            List<String> productIds = new ArrayList<>();
            productIds.add(productId);
            queryProductDetailsInternal(productIds, BillingClient.ProductType.SUBS, (queryBillingResult, queryProductDetailsResult) -> {
                if (queryBillingResult.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                    callback.accept(queryBillingResult);
                    return;
                }

                List<ProductDetails> productDetailsList = queryProductDetailsResult.getProductDetailsList();
                if (productDetailsList.isEmpty()) {
                    callback.accept(BillingResult.newBuilder()
                            .setResponseCode(BillingClient.BillingResponseCode.ITEM_UNAVAILABLE)
                            .build());
                    return;
                }

                upgradeSubsProductInternal(activity, productDetailsList.get(0), oldPurchaseToken, subscriptionReplacementMode, options, callback);
            });
        });
    }

    private void upgradeSubsProductInternal(@NonNull Activity activity, @NonNull ProductDetails productDetails, @NonNull String oldPurchaseToken, int subscriptionReplacementMode, @NonNull PurchaseOptions options, @NonNull Consumer<BillingResult> callback) {
        List<ProductDetails.SubscriptionOfferDetails> subscriptionOfferDetails = productDetails.getSubscriptionOfferDetails();
        if (subscriptionOfferDetails == null || subscriptionOfferDetails.isEmpty()) {
            callback.accept(BillingResult.newBuilder()
                    .setResponseCode(BillingClient.BillingResponseCode.ITEM_UNAVAILABLE)
                    .build());
            return;
        }

        List<BillingFlowParams.ProductDetailsParams> productDetailsParams = new ArrayList<>();
        productDetailsParams.add(BillingFlowParams.ProductDetailsParams.newBuilder()
                .setProductDetails(productDetails)
                .setOfferToken(subscriptionOfferDetails.get(0).getOfferToken())
                .build());

        BillingFlowParams.SubscriptionUpdateParams subscriptionUpdateParams = BillingFlowParams.SubscriptionUpdateParams.newBuilder()
                .setOldPurchaseToken(oldPurchaseToken)
                .setSubscriptionReplacementMode(subscriptionReplacementMode)
                .build();

        BillingFlowParams.Builder billingFlowParamsBuilder = BillingFlowParams.newBuilder()
                .setSubscriptionUpdateParams(subscriptionUpdateParams)
                .setProductDetailsParamsList(productDetailsParams);

        String obfuscatedAccountId = options.getObfuscatedAccountId();
        if (obfuscatedAccountId != null) {
            billingFlowParamsBuilder.setObfuscatedAccountId(obfuscatedAccountId);
        }

        String obfuscatedProfileId = options.getObfuscatedProfileId();
        if (obfuscatedProfileId != null) {
            billingFlowParamsBuilder.setObfuscatedProfileId(obfuscatedProfileId);
        }

        launchBillingFlow(activity, billingFlowParamsBuilder.build(), callback);
    }

    public void queryPurchases(@NonNull String productType, @NonNull PurchasesResponseListener callback) {
        ensureConnection(result -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                callback.onQueryPurchasesResponse(result, new ArrayList<>());
                return;
            }

            QueryPurchasesParams queryPurchaseParam = QueryPurchasesParams.newBuilder()
                    .setProductType(productType)
                    .build();

            billingClient.queryPurchasesAsync(queryPurchaseParam, callback);
        });
    }

    public void consume(@NonNull String purchaseToken, @NonNull ConsumeResponseListener callback) {
        ensureConnection((result -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                callback.onConsumeResponse(result, purchaseToken);
                return;
            }

            ConsumeParams consumeParams = ConsumeParams.newBuilder()
                    .setPurchaseToken(purchaseToken)
                    .build();
            billingClient.consumeAsync(consumeParams, callback);
        }));
    }

    public void acknowledge(@NonNull String purchaseToken, @NonNull AcknowledgePurchaseResponseListener callback) {
        ensureConnection((result -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) {
                callback.onAcknowledgePurchaseResponse(result);
                return;
            }

            AcknowledgePurchaseParams acknowledgeParams = AcknowledgePurchaseParams.newBuilder()
                    .setPurchaseToken(purchaseToken)
                    .build();
            billingClient.acknowledgePurchase(acknowledgeParams, callback);
        }));
    }

    private void launchBillingFlow(@NonNull Activity activity, @NonNull BillingFlowParams params, @NonNull Consumer<BillingResult> callback) {
        Helper.getUiHandler().post(() -> {
            BillingResult result = billingClient.launchBillingFlow(activity, params);
            callback.accept(result);
        });
    }

    private void queryProductDetailsInternal(@NonNull List<String> productIds, @NonNull String productType, @NonNull ProductDetailsResponseListener callback) {
        List<QueryProductDetailsParams.Product> productsToQuery = new ArrayList<>();
        for (String productId : productIds) {
            productsToQuery.add(QueryProductDetailsParams.Product.newBuilder()
                    .setProductId(productId)
                    .setProductType(productType)
                    .build());
        }

        QueryProductDetailsParams params = QueryProductDetailsParams.newBuilder()
                .setProductList(productsToQuery)
                .build();

        billingClient.queryProductDetailsAsync(params, callback);
    }

    private void ensureConnection(@NonNull Consumer<BillingResult> callback) {
        if (connected) {
            callback.accept(BillingResult.newBuilder()
                    .setResponseCode(BillingClient.BillingResponseCode.OK)
                    .build());
            return;
        }

        connectionListeners.add(callback);
        if (connecting) {
            return;
        }

        connecting = true;
        billingClient.startConnection(clientStateListener);
    }

    @Override
    public void onPurchasesUpdated(@NonNull BillingResult billingResult, @Nullable List<Purchase> list) {
        purchasesUpdatedListener.onPurchasesUpdated(
                billingResult,
                list
        );
    }

    public void endConnection() {
        if (billingClient.isReady()) {
            billingClient.endConnection();
        }
    }
}
