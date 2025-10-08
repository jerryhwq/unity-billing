#if UNITY_ANDROID && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

using System.Collections.Generic;
#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GoogleProductDetails
    {
        public GoogleOneTimePurchaseOfferDetails OneTimePurchaseOfferDetails { get; }
        public string Description { get; }
        public string Name { get; }
        public string ProductId { get; }
        public string ProductType { get; }
        public string Title { get; }
        public List<GoogleSubscriptionOfferDetails> SubscriptionOfferDetails { get; }

        private GoogleProductDetails()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GoogleProductDetails(AndroidJavaObject javaProductDetails)
        {
            using var javaOneTimePurchaseOfferDetails =
                javaProductDetails.Call<AndroidJavaObject>("getOneTimePurchaseOfferDetails");
            if (javaOneTimePurchaseOfferDetails != null)
                OneTimePurchaseOfferDetails = new GoogleOneTimePurchaseOfferDetails(javaOneTimePurchaseOfferDetails);

            Description = javaProductDetails.Call<string>("getDescription");
            Name = javaProductDetails.Call<string>("getName");
            ProductId = javaProductDetails.Call<string>("getProductId");
            ProductType = javaProductDetails.Call<string>("getProductType");
            Title = javaProductDetails.Call<string>("getTitle");

            using var javaSubscriptionOfferDetailsList =
                javaProductDetails.Call<AndroidJavaObject>("getSubscriptionOfferDetails");
            if (javaSubscriptionOfferDetailsList != null)
            {
                SubscriptionOfferDetails = new List<GoogleSubscriptionOfferDetails>();
                var subscriptionOfferDetailsListSize = javaSubscriptionOfferDetailsList.Call<int>("size");
                for (var i = 0; i < subscriptionOfferDetailsListSize; i++)
                {
                    using var javaSubscriptionOfferDetails =
                        javaSubscriptionOfferDetailsList.Call<AndroidJavaObject>("get", i);
                    SubscriptionOfferDetails.Add(new GoogleSubscriptionOfferDetails(javaSubscriptionOfferDetails));
                }
            }
        }
#endif
    }
}