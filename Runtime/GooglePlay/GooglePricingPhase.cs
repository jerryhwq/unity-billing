using UnityEngine;

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

        public GooglePricingPhase(AndroidJavaObject javaPricingPhase)
        {
            BillingCycleCount = javaPricingPhase.Call<int>("getBillingCycleCount");
            RecurrenceMode = javaPricingPhase.Call<int>("getRecurrenceMode");
            PriceAmountMicros = javaPricingPhase.Call<long>("getPriceAmountMicros");
            BillingPeriod = javaPricingPhase.Call<string>("getBillingPeriod");
            FormattedPrice = javaPricingPhase.Call<string>("getFormattedPrice");
            PriceCurrencyCode = javaPricingPhase.Call<string>("getPriceCurrencyCode");
        }
    }
}