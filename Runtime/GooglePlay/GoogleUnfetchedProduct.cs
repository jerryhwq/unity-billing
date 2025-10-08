#if UNITY_ANDROID && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GoogleUnfetchedProduct
    {
        public int StatusCode { get; }
        public string ProductId { get; }
        public string ProductType { get; }
        public string SerializedDocid { get; }

        private GoogleUnfetchedProduct()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GoogleUnfetchedProduct(AndroidJavaObject nativeUnfetchedProduct)
        {
            StatusCode = nativeUnfetchedProduct.Call<int>("getStatusCode");
            ProductId = nativeUnfetchedProduct.Call<string>("getProductId");
            ProductType = nativeUnfetchedProduct.Call<string>("getProductType");
            SerializedDocid = nativeUnfetchedProduct.Call<string>("getSerializedDocid");
        }
#endif
    }
}