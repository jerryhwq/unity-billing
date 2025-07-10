namespace Enbug.Billing.AppleAppStore.StoreKit2
{
    public struct Transaction
    {
        public string jsonRepresentation;
        public ulong id;
        public ulong originalID;
        public string webOrderLineItemID;
        public string productID;
        public string subscriptionGroupID;
        public string appBundleID;
        public ulong purchaseDate;
        public ulong originalPurchaseDate;
        public ulong? expirationDate;
        public int purchasedQuantity;
        public bool isUpgraded;
        public Offer? offer;
        public OfferType? offerType;
        public string offerID;
        public string offerPaymentModeStringRepresentation;
        public int? revocationDate;
        public RevocationReason? revocationReason;
        public ProductType productType;
        public string appAccountToken;
        public Environment? environment;
        public Reason? reason;
        public string storefrontCountryCode;
        public decimal price;
        public string currencyCode;
        public string currencySymbol;
        public string deviceVerification;
        public string deviceVerificationNonce;
        public OwnershipType ownershipType;
        public ulong signedDate;

        public enum OwnershipType
        {
            unknown = -1,
            purchased,
            familyShared,
        }

        public enum Reason
        {
            unknown = -1,
            purchase,
            renewal,
        }

        public enum RevocationReason
        {
            unknown = -1,
            developerIssue,
            other,
        }

        public enum OfferType
        {
            unknown = -1,
            introductory,
            promotional,
            code,
            winBack,
        }

        public struct Offer
        {
            public int id;
            public int type;
            public string paymentMode;
        }
    }
}