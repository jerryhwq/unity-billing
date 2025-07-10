using System.Collections.Generic;
using UnityEngine;

namespace Enbug.Billing.GooglePlay
{
    public class GoogleSubscriptionOfferDetails
    {
        public GooglePricingPhases PricingPhases { get; }
        public string BasePlanId { get; }
        public string OfferId { get; }
        public string OfferToken { get; }
        public List<string> OfferTags { get; }

        public GoogleSubscriptionOfferDetails(AndroidJavaObject javaSubscriptionOfferDetails)
        {
            using var javaPricingPhases = javaSubscriptionOfferDetails.Call<AndroidJavaObject>("getPricingPhases");
            if (javaPricingPhases != null)
                PricingPhases = new GooglePricingPhases(javaPricingPhases);

            BasePlanId = javaSubscriptionOfferDetails.Call<string>("getBasePlanId");
            OfferId = javaSubscriptionOfferDetails.Call<string>("getOfferId");
            OfferToken = javaSubscriptionOfferDetails.Call<string>("getOfferToken");

            using var javaOfferTags = javaSubscriptionOfferDetails.Call<AndroidJavaObject>("getOfferTags");
            if (javaOfferTags != null)
            {
                OfferTags = new List<string>();
                var offerTagsSize = javaOfferTags.Call<int>("size");
                for (var i = 0; i < offerTagsSize; i++)
                {
                    OfferTags.Add(javaOfferTags.Call<string>("get", i));
                }
            }
        }
    }
}