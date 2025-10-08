#if UNITY_ANDROID && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

using System.Collections.Generic;
#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GoogleQueryProductDetailsResult
    {
        public List<GoogleProductDetails> ProductDetailsList { get; }
        public List<GoogleUnfetchedProduct> UnfetchedProductList { get; }

        private GoogleQueryProductDetailsResult()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GoogleQueryProductDetailsResult(AndroidJavaObject nativeQueryProductDetailsResult)
        {
            ProductDetailsList = new List<GoogleProductDetails>();
            UnfetchedProductList = new List<GoogleUnfetchedProduct>();

            var nativeProductDetailsList =
                nativeQueryProductDetailsResult.Call<AndroidJavaObject>("getProductDetailsList");
            var productDetailsSize = nativeProductDetailsList.Call<int>("size");
            for (var i = 0; i < productDetailsSize; i++)
            {
                using var javaProductDetails = nativeProductDetailsList.Call<AndroidJavaObject>("get", i);
                var productDetail = new GoogleProductDetails(javaProductDetails);
                ProductDetailsList.Add(productDetail);
            }

            var nativeUnfetchedProductList =
                nativeQueryProductDetailsResult.Call<AndroidJavaObject>("getUnfetchedProductList");
            var unfetchedProductSize = nativeUnfetchedProductList.Call<int>("size");
            for (var i = 0; i < unfetchedProductSize; i++)
            {
                using var javaUnfetchedProduct = nativeUnfetchedProductList.Call<AndroidJavaObject>("get", i);
                var unfetchedProduct = new GoogleUnfetchedProduct(javaUnfetchedProduct);
                UnfetchedProductList.Add(unfetchedProduct);
            }
        }
#endif
    }
}