using UnityEngine;

namespace Enbug.Billing.GooglePlay
{
    public class GoogleOneTimePurchaseOfferDetails
    {
        public long PriceAmountMicros { get; }
        public string FormattedPrice { get; }
        public string PriceCurrencyCode { get; }

        public GoogleOneTimePurchaseOfferDetails(AndroidJavaObject javaGoogleOneTimePurchaseOfferDetails)
        {
            PriceAmountMicros = javaGoogleOneTimePurchaseOfferDetails.Call<long>("getPriceAmountMicros");
            FormattedPrice = javaGoogleOneTimePurchaseOfferDetails.Call<string>("getFormattedPrice");
            PriceCurrencyCode = javaGoogleOneTimePurchaseOfferDetails.Call<string>("getPriceCurrencyCode");
        }
    }
}