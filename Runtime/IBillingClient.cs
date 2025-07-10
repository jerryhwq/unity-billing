using System;
using System.Collections.Generic;

namespace Enbug.Billing
{
    internal interface IBillingClient
    {
        public bool IsBillingSupported { get; }

        public bool IsSubscriptionSupported { get; }

        public bool IsSubscriptionsUpdateSupported { get; }

        /**
         * 查询消耗型支付项
         */
        public void QueryInAppProducts(string[] skus, Action<BillingResult, List<Product>> callback);

        /**
         * 查询订阅
         */
        public void QuerySubsProducts(string[] skus, Action<BillingResult, List<Product>> callback);

        /**
         * 购买消耗型支付项
         */
        public void BuyInAppProduct(string sku, PurchaseOptions options);

        /**
         * 购买订阅
         */
        public void BuySubsProduct(string sku, PurchaseOptions options);

        /**
         * 消耗购买
         */
        public void Consume(string purchaseToken, Action<BillingResult> callback);

        /**
         * 确认订阅
         */
        public void Acknowledge(string purchaseToken, Action<BillingResult> callback);

        /**
         * 查询未消耗的购买
         */
        public void QueryPurchases(Action<BillingResult, List<Purchase>> callback);
    }
}