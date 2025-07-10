namespace Enbug.Billing.AppleAppStore.StoreKit1
{
    public class SKProductDiscount
    {
        public enum PaymentMode
        {
            PayAsYouGo = 0,
            PayUpFront = 1,
            FreeTrial = 2,
        }

        public enum Type
        {
            Introductory = 0,
            Subscription = 1,
        }

        public string displayPrice;
        public string currencyCode;
        public string currencySymbol;
        public decimal price;
        public SKProductSubscriptionPeriod subscriptionPeriod;
        public int numberOfPeriods;
        public PaymentMode paymentMode;

        public string identifer;
        public Type type;
    }
}