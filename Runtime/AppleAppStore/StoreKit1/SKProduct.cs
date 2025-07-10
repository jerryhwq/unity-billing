namespace Enbug.Billing.AppleAppStore.StoreKit1
{
    public class SKProduct
    {
        public string localizedDescription;
        public string localizedTitle;
        public string displayPrice;
        public string currencyCode;
        public string currencySymbol;
        public decimal price;
        public string productIdentifier;
        public bool? isFamilyShareable;
        public string contentVersion;
        public SKProductSubscriptionPeriod subscriptionPeriod;
        public SKProductDiscount introductoryPrice;
        public string subscriptionGroupIdentifier;
        public SKProductDiscount[] discounts;

        public enum PeriodUnit : uint
        {
            Day = 0,
            Week = 1,
            Month = 2,
            Year = 3,
        }
    }
}