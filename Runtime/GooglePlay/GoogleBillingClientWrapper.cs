#if UNITY_ANDROID && !UNITY_EDITOR && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

using System;
using System.Collections.Generic;
#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
using UnityEngine.Scripting;
#endif

namespace Enbug.Billing.GooglePlay
{
    internal class GoogleBillingClientWrapper
    {
#if ENABLE_GOOGLE_PLAY_BILLING
        private readonly AndroidJavaObject _nativeBillingClient;
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
            _nativeBillingClient = new AndroidJavaObject("io.enbug.billing.google.BillingClient",
                new PurchasesUpdatedListener(onPurchasesUpdated));
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

            var listener = new ProductDetailsResponseListener(callback);
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
                new BillingResultListener(callback));
#endif
        }

        public void BuySubsProduct(string productId, PurchaseOptions options, Action<GoogleBillingResult> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            using var javaOptions = ConvertOptions(options);
            _nativeBillingClient.Call("buySubsProduct", unityActivity, productId, javaOptions,
                new BillingResultListener(callback));
#endif
        }

        public void Consume(string purchaseToken, Action<GoogleBillingResult, string> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            var consumeResponseListener = new ConsumeResponseListener(callback);
            _nativeBillingClient.Call("consume", purchaseToken, consumeResponseListener);
#endif
        }

        public void Acknowledge(string purchaseToken, Action<GoogleBillingResult> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            var acknowledgeResponseListener = new AcknowledgePurchaseResponseListener(callback);
            _nativeBillingClient.Call("acknowledge", purchaseToken, acknowledgeResponseListener);
#endif
        }

        public void QueryPurchases(string productType, Action<GoogleBillingResult, List<GooglePurchase>> callback)
        {
#if ENABLE_GOOGLE_PLAY_BILLING
            var purchasesResponseListener = new PurchasesResponseListener(callback);
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
            private readonly Action<GoogleBillingResult> _callback;

            public BillingResultListener(Action<GoogleBillingResult> callback) : base(
                "androidx.core.util.Consumer")
            {
                _callback = callback;
            }

            [Preserve]
            public void accept(AndroidJavaObject nativeBillingResult)
            {
                var billingResult = new GoogleBillingResult(nativeBillingResult);
                _callback.Invoke(billingResult);
            }
        }

        private class AcknowledgePurchaseResponseListener : AndroidJavaProxy
        {
            private readonly Action<GoogleBillingResult> _callback;

            public AcknowledgePurchaseResponseListener(Action<GoogleBillingResult> callback) : base(
                "com.android.billingclient.api.AcknowledgePurchaseResponseListener")
            {
                _callback = callback;
            }

            [Preserve]
            public void onAcknowledgePurchaseResponse(AndroidJavaObject nativeBillingResult)
            {
                var billingResult = new GoogleBillingResult(nativeBillingResult);
                _callback.Invoke(billingResult);
            }
        }

        private class SkuDetailsResponseListener : AndroidJavaProxy
        {
            private readonly Action<GoogleBillingResult, List<GoogleSkuDetails>> _callback;

            public SkuDetailsResponseListener(Action<GoogleBillingResult, List<GoogleSkuDetails>> callback) :
                base("com.android.billingclient.api.SkuDetailsResponseListener")
            {
                _callback = callback;
            }

            [Preserve]
            public void onSkuDetailsResponse(AndroidJavaObject nativeBillingResult, AndroidJavaObject nativeSkuDetails)
            {
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
            private readonly Action<GoogleBillingResult, List<GoogleProductDetails>> _callback;

            public ProductDetailsResponseListener(Action<GoogleBillingResult, List<GoogleProductDetails>> callback) :
                base("com.android.billingclient.api.ProductDetailsResponseListener")
            {
                _callback = callback;
            }

            [Preserve]
            public void onProductDetailsResponse(AndroidJavaObject nativeBillingResult,
                AndroidJavaObject nativeProductDetails)
            {
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
            private readonly Action<GoogleBillingResult, string> _callback;

            public ConsumeResponseListener(Action<GoogleBillingResult, string> callback) : base(
                "com.android.billingclient.api.ConsumeResponseListener")
            {
                _callback = callback;
            }

            [Preserve]
            public void onConsumeResponse(AndroidJavaObject nativeBillingResult, string purchaseToken)
            {
                var billingResult = new GoogleBillingResult(nativeBillingResult);
                _callback.Invoke(billingResult, purchaseToken);
            }
        }

        private class PurchasesResponseListener : AndroidJavaProxy
        {
            private readonly Action<GoogleBillingResult, List<GooglePurchase>> _callback;

            public PurchasesResponseListener(Action<GoogleBillingResult, List<GooglePurchase>> callback) : base(
                "com.android.billingclient.api.PurchasesResponseListener")
            {
                _callback = callback;
            }

            [Preserve]
            public void onQueryPurchasesResponse(AndroidJavaObject nativeBillingResult,
                AndroidJavaObject nativePurchases)
            {
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