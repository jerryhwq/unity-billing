namespace Enbug.Billing.AppleAppStore.StoreKit1
{
    public enum SKPaymentTransactionState
    {
        SKPaymentTransactionStatePurchasing = 0,    // Transaction is being added to the server queue.
        SKPaymentTransactionStatePurchased = 1,     // Transaction is in queue, user has been charged.  Client should complete the transaction.
        SKPaymentTransactionStateFailed = 2,        // Transaction was cancelled or failed before being added to the server queue.
        SKPaymentTransactionStateRestored = 3,      // Transaction was restored from user's purchase history.  Client should complete the transaction.
        SKPaymentTransactionStateDeferred = 4,   // The transaction is in the queue, but its final status is pending external action.
    }
}