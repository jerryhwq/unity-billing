using System;
using System.Collections.Generic;

namespace Enbug.Billing.FakeAppStore
{
    public class FakeBillingClient : IBillingClient
    {
        public bool IsBillingSupported => false;
        public bool IsSubscriptionSupported => false;
        public bool IsSubscriptionsUpdateSupported => false;

        public void QueryInAppProducts(string[] skus, Action<BillingResult, List<Product>> callback)
        {
        }

        public void QuerySubsProducts(string[] skus, Action<BillingResult, List<Product>> callback)
        {
        }

        public void BuyInAppProduct(string sku, PurchaseOptions options)
        {
        }

        public void BuySubsProduct(string sku, PurchaseOptions options)
        {
        }

        public void Consume(string purchaseToken, Action<BillingResult> callback)
        {
        }

        public void Acknowledge(string purchaseToken, Action<BillingResult> callback)
        {
        }

        public void QueryPurchases(Action<BillingResult, List<Purchase>> callback)
        {
        }
    }
}