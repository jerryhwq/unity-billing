using System;

namespace Enbug.Billing.GooglePlay
{
    public class GoogleBillingResponseCode
    {
        [Obsolete] public const int SERVICE_TIMEOUT = -3;
        public const int FEATURE_NOT_SUPPORTED = -2;
        public const int SERVICE_DISCONNECTED = -1;
        public const int OK = 0;
        public const int USER_CANCELED = 1;
        public const int SERVICE_UNAVAILABLE = 2;
        public const int BILLING_UNAVAILABLE = 3;
        public const int ITEM_UNAVAILABLE = 4;
        public const int DEVELOPER_ERROR = 5;
        public const int ERROR = 6;
        public const int ITEM_ALREADY_OWNED = 7;
        public const int ITEM_NOT_OWNED = 8;
        public const int NETWORK_ERROR = 12;
    }
}