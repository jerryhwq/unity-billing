namespace Enbug.Billing.AppleAppStore.StoreKit2
{
    public struct Product
    {
        public string jsonRepresentation;
        public string id;
        public ProductType type;
        public string displayName;
        public string description;
        public string displayPrice;
        public string currencyCode;
        public string currencySymbol;
        public decimal price;
        public bool isFamilyShareable;
        public SubscriptionInfo? subscription;

        public enum ProductType
        {
            unknown = -1,
            consumable,
            nonConsumable,
            nonRenewable,
            autoRenewable,
        }

        public enum OfferType
        {
            unknown = -1,
            introductory,
            promotional,
            winBack,
        }

        public struct SubscriptionPeriod
        {
            public enum Unit
            {
                unknown = -1,
                day,
                week,
                month,
                year,
            }

            public Unit unit;
            public int value;
        }

        public struct SubscriptionOffer
        {
            public string id;
            public OfferType type;
            public decimal price;
            public string displayPrice;
            public SubscriptionPeriod period;
            public int periodCount;
            public PaymentMode paymentMode;

            public enum PaymentMode
            {
                unknown = -1,
                payAsYouGo,
                payUpFront,
                freeTrial,
            }
        }

        public struct SubscriptionInfo
        {
            public SubscriptionOffer? introductoryOffer;
            public SubscriptionOffer[] promotionalOffers;
            public SubscriptionOffer[] winBackOffers;
            public string subscriptionGroupID;
            public SubscriptionPeriod subscriptionPeriod;
            public bool isEligibleForIntroOffer;
        }

        public struct PurchaseOption
        {
            public string appAccountToken;
        }
    }
}