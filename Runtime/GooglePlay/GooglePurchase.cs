#if UNITY_ANDROID && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

using System.Collections.Generic;
#if ENABLE_UNITY_ANDROID_JNI
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    internal class GooglePurchase
    {
        public int PurchaseState { get; }
        public int Quantity { get; }
        public long PurchaseTime { get; }
        public string ObfuscatedAccountId { get; }
        public string ObfuscatedProfileId { get; }
        public string OrderId { get; }
        public string OriginalJson { get; }
        public string PackageName { get; }
        public string PurchaseToken { get; }
        public string Signature { get; }
        public List<string> Products { get; }

        private GooglePurchase()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GooglePurchase(AndroidJavaObject javaObject)
        {
            PurchaseState = javaObject.Call<int>("getPurchaseState");
            Quantity = javaObject.Call<int>("getQuantity");
            PurchaseTime = javaObject.Call<long>("getPurchaseTime");
            using var accountIdentifiers = javaObject.Call<AndroidJavaObject>("getAccountIdentifiers");
            ObfuscatedAccountId = accountIdentifiers.Call<string>("getObfuscatedAccountId");
            ObfuscatedProfileId = accountIdentifiers.Call<string>("getObfuscatedProfileId");
            OrderId = javaObject.Call<string>("getOrderId");
            OriginalJson = javaObject.Call<string>("getOriginalJson");
            PackageName = javaObject.Call<string>("getPackageName");
            PurchaseToken = javaObject.Call<string>("getPurchaseToken");
            Signature = javaObject.Call<string>("getSignature");
            using var skus = javaObject.Call<AndroidJavaObject>("getSkus");
            using var products = javaObject.Call<AndroidJavaObject>("getProducts");
            Products = new List<string>();
            if (products != null)
            {
                var size = products.Call<int>("size");
                for (var i = 0; i < size; i++)
                {
                    Products.Add(products.Call<string>("get", i));
                }
            }
            else if (skus != null)
            {
                var size = skus.Call<int>("size");
                for (var i = 0; i < size; i++)
                {
                    Products.Add(skus.Call<string>("get", i));
                }
            }
        }
#endif
    }
}