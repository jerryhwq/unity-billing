#if UNITY_ANDROID && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GooglePricingPhase
    {
        public int BillingCycleCount { get; }
        public int RecurrenceMode { get; }
        public long PriceAmountMicros { get; }
        public string BillingPeriod { get; }
        public string FormattedPrice { get; }
        public string PriceCurrencyCode { get; }

        private GooglePricingPhase()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GooglePricingPhase(AndroidJavaObject javaPricingPhase)
        {
            BillingCycleCount = javaPricingPhase.Call<int>("getBillingCycleCount");
            RecurrenceMode = javaPricingPhase.Call<int>("getRecurrenceMode");
            PriceAmountMicros = javaPricingPhase.Call<long>("getPriceAmountMicros");
            BillingPeriod = javaPricingPhase.Call<string>("getBillingPeriod");
            FormattedPrice = javaPricingPhase.Call<string>("getFormattedPrice");
            PriceCurrencyCode = javaPricingPhase.Call<string>("getPriceCurrencyCode");
        }
#endif
    }
}