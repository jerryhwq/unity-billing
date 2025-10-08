#if UNITY_ANDROID && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GoogleOneTimePurchaseOfferDetails
    {
        public long PriceAmountMicros { get; }
        public string FormattedPrice { get; }
        public string PriceCurrencyCode { get; }

        private GoogleOneTimePurchaseOfferDetails()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GoogleOneTimePurchaseOfferDetails(AndroidJavaObject javaGoogleOneTimePurchaseOfferDetails)
        {
            PriceAmountMicros = javaGoogleOneTimePurchaseOfferDetails.Call<long>("getPriceAmountMicros");
            FormattedPrice = javaGoogleOneTimePurchaseOfferDetails.Call<string>("getFormattedPrice");
            PriceCurrencyCode = javaGoogleOneTimePurchaseOfferDetails.Call<string>("getPriceCurrencyCode");
        }
#endif
    }
}