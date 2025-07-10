namespace Enbug.Billing.AppleAppStore.StoreKit1
{
    public class SKPaymentTransaction
    {
        public SKPaymentTransactionState transactionState;
        public string transactionIdentifier;
        public SKPayment payment;
        public AppleError error;
    }
}