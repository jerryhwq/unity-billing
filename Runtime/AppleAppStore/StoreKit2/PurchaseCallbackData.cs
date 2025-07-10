using System.Collections.Generic;

namespace Enbug.Billing.AppleAppStore.StoreKit2
{
    public class PurchaseCallbackData
    {
        public AppleErrorCode? code;
        public AppleError error;
        public Transaction? transaction;
    }
}