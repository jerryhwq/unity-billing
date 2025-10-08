#if UNITY_ANDROID && !UNITY_EDITOR && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

using System;
using System.Collections.Generic;
#if ENABLE_GOOGLE_PLAY_BILLING
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Scripting;
#endif

namespace Enbug.Billing.GooglePlay
{
    internal class GoogleBillingClientWrapper
    {
#if ENABLE_GOOGLE_PLAY_BILLING
        private readonly AndroidJavaObject _nativeBillingClient;
        private readonly PurchasesUpdatedListener _purchasesUpdatedListener;
        private readonly ConcurrentDictionary<AndroidJavaProxy, bool> _proxies = new();
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
        public bool IsSubscriptionSupported => _nativeBillingClient.Call<bool>("isSubscriptionsSupported");
#else
        public bool IsSubscriptionSupported => false;
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
        public bool IsSubscriptionsUpdateSupported => _nativeBillingClient.Call<bool>("isSubscriptionsUpdateSupported");
#else
        public bool IsSubscriptionsUpdateSupported => false;
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
        public bool IsProductDetailsSupported => _nativeBillingClient.Call<bool>("isProductDetailsSupported");
#else
        public bool IsProductDetailsSupported => false;
#endif

        public GoogleBillingClientWrapper(Action<GoogleBillingResult, List<GooglePurchase>> onPurchasesUpdated)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            _purchasesUpdatedListener = new PurchasesUpdatedListener(onPurchasesUpdated);
            _nativeBillingClient =
                new AndroidJavaObject("io.enbug.billing.google.BillingClient", _purchasesUpdatedListener);
#endif
        }

        ~GoogleBillingClientWrapper()
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            _nativeBillingClient.Dispose();
#endif
        }

        public void QueryProductDetails(string productType, string[] productIds,
            Action<GoogleBillingResult, List<GoogleProductDetails>> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            using var javaProductList = new AndroidJavaObject("java.util.ArrayList");
            foreach (var productId in productIds)
                javaProductList.Call<bool>("add", productId);

            var listener = new ProductDetailsResponseListener(_proxies, callback);
            _nativeBillingClient.Call("queryProductDetails", productType, javaProductList, listener);
#endif
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        private AndroidJavaObject ConvertOptions(PurchaseOptions options)
        {
            var javaOptions = new AndroidJavaObject("io.enbug.billing.google.PurchaseOptions");
            if (options.UserIdentifier != null)
                javaOptions.Call("setObfuscatedAccountId", options.UserIdentifier);
            return javaOptions;
        }
#endif

        public void BuyInAppProduct(string productId, PurchaseOptions options, Action<GoogleBillingResult> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            using var javaOptions = ConvertOptions(options);
            _nativeBillingClient.Call("buyInAppProduct", unityActivity, productId, javaOptions,
                new BillingResultListener(_proxies, callback));
#endif
        }

        public void BuySubsProduct(string productId, PurchaseOptions options, Action<GoogleBillingResult> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            using var javaOptions = ConvertOptions(options);
            _nativeBillingClient.Call("buySubsProduct", unityActivity, productId, javaOptions,
                new BillingResultListener(_proxies, callback));
#endif
        }

        public void Consume(string purchaseToken, Action<GoogleBillingResult, string> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            var consumeResponseListener = new ConsumeResponseListener(_proxies, callback);
            _nativeBillingClient.Call("consume", purchaseToken, consumeResponseListener);
#endif
        }

        public void Acknowledge(string purchaseToken, Action<GoogleBillingResult> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            var acknowledgeResponseListener = new AcknowledgePurchaseResponseListener(_proxies, callback);
            _nativeBillingClient.Call("acknowledge", purchaseToken, acknowledgeResponseListener);
#endif
        }

        public void QueryPurchases(string productType, Action<GoogleBillingResult, List<GooglePurchase>> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            var purchasesResponseListener = new PurchasesResponseListener(_proxies, callback);
            _nativeBillingClient.Call("queryPurchases", productType, purchasesResponseListener);
#endif
        }

        internal string ConvertCurrencyCodeToSymbol(string priceCurrencyCode)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            if (string.IsNullOrEmpty(priceCurrencyCode))
                return null;

            try
            {
                using var currency = new AndroidJavaClass("java.util.Currency");
                using var currencyInstance =
                    currency.CallStatic<AndroidJavaObject>("getInstance", priceCurrencyCode);
                return currencyInstance.Call<string>("getSymbol");
            }
            catch
            {
                return null;
            }
#else
            return null;
#endif
        }

        internal decimal ConvertMicrosToDecimal(long? micros)
        {
            if (micros == null)
                return 0m;
            return micros.Value / 1_000_000m;
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        private class BillingResultListener : AndroidJavaProxy
        {
            private readonly ConcurrentDictionary<AndroidJavaProxy, bool> _proxies;
            private readonly Action<GoogleBillingResult> _callback;

            public BillingResultListener(
                ConcurrentDictionary<AndroidJavaProxy, bool> proxies,
                Action<GoogleBillingResult> callback
            ) : base("androidx.core.util.Consumer")
            {
                _proxies = proxies;
                _callback = callback;
                _proxies[this] = true;
            }

            [Preserve]
            public void accept(AndroidJavaObject nativeBillingResult)
            {
                _proxies.TryRemove(this, out _);

                var billingResult = new GoogleBillingResult(nativeBillingResult);
                _callback.Invoke(billingResult);
            }
        }

        private class AcknowledgePurchaseResponseListener : AndroidJavaProxy
        {
            private readonly ConcurrentDictionary<AndroidJavaProxy, bool> _proxies;
            private readonly Action<GoogleBillingResult> _callback;

            public AcknowledgePurchaseResponseListener(
                ConcurrentDictionary<AndroidJavaProxy, bool> proxies,
                Action<GoogleBillingResult> callback
            ) : base("com.android.billingclient.api.AcknowledgePurchaseResponseListener")
            {
                _proxies = proxies;
                _callback = callback;
                _proxies[this] = true;
            }

            [Preserve]
            public void onAcknowledgePurchaseResponse(AndroidJavaObject nativeBillingResult)
            {
                _proxies.TryRemove(this, out _);

                var billingResult = new GoogleBillingResult(nativeBillingResult);
                _callback.Invoke(billingResult);
            }
        }

        private class SkuDetailsResponseListener : AndroidJavaProxy
        {
            private readonly ConcurrentDictionary<AndroidJavaProxy, bool> _proxies;
            private readonly Action<GoogleBillingResult, List<GoogleSkuDetails>> _callback;

            public SkuDetailsResponseListener(
                ConcurrentDictionary<AndroidJavaProxy, bool> proxies,
                Action<GoogleBillingResult, List<GoogleSkuDetails>> callback
            ) : base("com.android.billingclient.api.SkuDetailsResponseListener")
            {
                _proxies = proxies;
                _callback = callback;
                _proxies[this] = true;
            }

            [Preserve]
            public void onSkuDetailsResponse(AndroidJavaObject nativeBillingResult, AndroidJavaObject nativeSkuDetails)
            {
                _proxies.TryRemove(this, out _);

                var billingResult = new GoogleBillingResult(nativeBillingResult);

                List<GoogleSkuDetails> skuDetails = null;
                if (nativeSkuDetails != null)
                {
                    skuDetails = new List<GoogleSkuDetails>();
                    var size = nativeSkuDetails.Call<int>("size");
                    for (var i = 0; i < size; i++)
                    {
                        using var javaSkuDetails = nativeSkuDetails.Call<AndroidJavaObject>("get", i);
                        var skuDetail = new GoogleSkuDetails(javaSkuDetails);
                        skuDetails.Add(skuDetail);
                    }
                }

                _callback.Invoke(billingResult, skuDetails);
            }
        }

        private class ProductDetailsResponseListener : AndroidJavaProxy
        {
            private readonly ConcurrentDictionary<AndroidJavaProxy, bool> _proxies;
            private readonly Action<GoogleBillingResult, List<GoogleProductDetails>> _callback;

            public ProductDetailsResponseListener(
                ConcurrentDictionary<AndroidJavaProxy, bool> proxies,
                Action<GoogleBillingResult, List<GoogleProductDetails>> callback
            ) : base("com.android.billingclient.api.ProductDetailsResponseListener")
            {
                _proxies = proxies;
                _callback = callback;
                _proxies[this] = true;
            }

            [Preserve]
            public void onProductDetailsResponse(AndroidJavaObject nativeBillingResult,
                AndroidJavaObject nativeProductDetails)
            {
                _proxies.TryRemove(this, out _);

                var billingResult = new GoogleBillingResult(nativeBillingResult);
                List<GoogleProductDetails> productDetails = null;
                if (nativeProductDetails != null)
                {
                    productDetails = new List<GoogleProductDetails>();
                    var size = nativeProductDetails.Call<int>("size");
                    for (var i = 0; i < size; i++)
                    {
                        using var javaProductDetails = nativeProductDetails.Call<AndroidJavaObject>("get", i);
                        var productDetail = new GoogleProductDetails(javaProductDetails);
                        productDetails.Add(productDetail);
                    }
                }

                _callback.Invoke(billingResult, productDetails);
            }
        }

        private class ConsumeResponseListener : AndroidJavaProxy
        {
            private readonly ConcurrentDictionary<AndroidJavaProxy, bool> _proxies;
            private readonly Action<GoogleBillingResult, string> _callback;

            public ConsumeResponseListener(
                ConcurrentDictionary<AndroidJavaProxy, bool> proxies,
                Action<GoogleBillingResult, string> callback
            ) : base("com.android.billingclient.api.ConsumeResponseListener")
            {
                _proxies = proxies;
                _callback = callback;
                _proxies[this] = true;
            }

            [Preserve]
            public void onConsumeResponse(AndroidJavaObject nativeBillingResult, string purchaseToken)
            {
                _proxies.TryRemove(this, out _);

                var billingResult = new GoogleBillingResult(nativeBillingResult);
                _callback.Invoke(billingResult, purchaseToken);
            }
        }

        private class PurchasesResponseListener : AndroidJavaProxy
        {
            private readonly ConcurrentDictionary<AndroidJavaProxy, bool> _proxies;
            private readonly Action<GoogleBillingResult, List<GooglePurchase>> _callback;

            public PurchasesResponseListener(
                ConcurrentDictionary<AndroidJavaProxy, bool> proxies,
                Action<GoogleBillingResult, List<GooglePurchase>> callback
            ) : base("com.android.billingclient.api.PurchasesResponseListener")
            {
                _proxies = proxies;
                _callback = callback;
                _proxies[this] = true;
            }

            [Preserve]
            public void onQueryPurchasesResponse(AndroidJavaObject nativeBillingResult,
                AndroidJavaObject nativePurchases)
            {
                _proxies.TryRemove(this, out _);

                var billingResult = new GoogleBillingResult(nativeBillingResult);
                var purchases = new List<GooglePurchase>();
                if (nativePurchases != null)
                {
                    var size = nativePurchases.Call<int>("size");
                    for (var i = 0; i < size; i++)
                    {
                        using var javaGooglePurchase = nativePurchases.Call<AndroidJavaObject>("get", i);
                        var purchase = new GooglePurchase(javaGooglePurchase);
                        purchases.Add(purchase);
                    }
                }

                _callback?.Invoke(billingResult, purchases);
            }
        }

        private class PurchasesUpdatedListener : AndroidJavaProxy
        {
            private readonly Action<GoogleBillingResult, List<GooglePurchase>> _onPurchasesUpdated;

            public PurchasesUpdatedListener(Action<GoogleBillingResult, List<GooglePurchase>> onPurchasesUpdated) :
                base("com.android.billingclient.api.PurchasesUpdatedListener")
            {
                _onPurchasesUpdated = onPurchasesUpdated;
            }

            [Preserve]
            public void onPurchasesUpdated(AndroidJavaObject nativeBillingResult, AndroidJavaObject nativePurchases)
            {
                var billingResult = new GoogleBillingResult(nativeBillingResult);
                var purchases = new List<GooglePurchase>();
                if (nativePurchases != null)
                {
                    var size = nativePurchases.Call<int>("size");
                    for (var i = 0; i < size; i++)
                    {
                        using var javaPurchase = nativePurchases.Call<AndroidJavaObject>("get", i);
                        var purchase = new GooglePurchase(javaPurchase);
                        purchases.Add(purchase);
                    }
                }

                _onPurchasesUpdated(billingResult, purchases);
            }
        }
#endif
    }
}