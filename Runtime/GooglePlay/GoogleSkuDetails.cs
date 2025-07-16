#if UNITY_ANDROID && !UNITY_EDITOR && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GoogleSkuDetails
    {
        public int IntroductoryPriceCycles { get; }
        public long IntroductoryPriceAmountMicros { get; }
        public long OriginalPriceAmountMicros { get; }
        public long PriceAmountMicros { get; }
        public string Description { get; }
        public string FreeTrialPeriod { get; }
        public string IconUrl { get; }
        public string IntroductoryPrice { get; }
        public string IntroductoryPricePeriod { get; }
        public string OriginalJson { get; }
        public string OriginalPrice { get; }
        public string Price { get; }
        public string PriceCurrencyCode { get; }
        public string Sku { get; }
        public string SubscriptionPeriod { get; }
        public string Title { get; }
        public string Type { get; }

        private GoogleSkuDetails()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GoogleSkuDetails(AndroidJavaObject javaSkuDetails)
        {
            IntroductoryPriceCycles = javaSkuDetails.Call<int>("getIntroductoryPriceCycles");
            IntroductoryPriceAmountMicros = javaSkuDetails.Call<long>("getIntroductoryPriceAmountMicros");
            OriginalPriceAmountMicros = javaSkuDetails.Call<long>("getOriginalPriceAmountMicros");
            PriceAmountMicros = javaSkuDetails.Call<long>("getPriceAmountMicros");
            Description = javaSkuDetails.Call<string>("getDescription");
            FreeTrialPeriod = javaSkuDetails.Call<string>("getFreeTrialPeriod");
            IconUrl = javaSkuDetails.Call<string>("getIconUrl");
            IntroductoryPrice = javaSkuDetails.Call<string>("getIntroductoryPrice");
            IntroductoryPricePeriod = javaSkuDetails.Call<string>("getIntroductoryPricePeriod");
            OriginalJson = javaSkuDetails.Call<string>("getOriginalJson");
            OriginalPrice = javaSkuDetails.Call<string>("getOriginalPrice");
            Price = javaSkuDetails.Call<string>("getPrice");
            PriceCurrencyCode = javaSkuDetails.Call<string>("getPriceCurrencyCode");
            Sku = javaSkuDetails.Call<string>("getSku");
            SubscriptionPeriod = javaSkuDetails.Call<string>("getSubscriptionPeriod");
            Title = javaSkuDetails.Call<string>("getTitle");
            Type = javaSkuDetails.Call<string>("getType");
        }
#endif
    }
}