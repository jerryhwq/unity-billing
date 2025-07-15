using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Enbug.Billing.AppleAppStore;
using Enbug.Billing.FakeAppStore;
using Enbug.Billing.GooglePlay;
using UnityEngine;

namespace Enbug.Billing
{
    public class BillingManager
    {
        private readonly IBillingClient _billingClient;

        public readonly AppStore AppStore;

        private readonly Action<BillingResult, Purchase> _onPurchaseComplete;

        public bool IsBillingSupported => _billingClient.IsBillingSupported;

        private readonly ConcurrentQueue<Action> _actionList = new();

        public BillingManager(Action<BillingResult, Purchase> onPurchaseComplete)
        {
            var platformInfo = Resources.Load<PlatformInfo>("billing_config");
            AppStore = platformInfo.appStore;

            if (Application.isEditor)
            {
                AppStore = AppStore.Unknown;
            }

            _onPurchaseComplete = onPurchaseComplete;
            _billingClient = AppStore switch
            {
                AppStore.GooglePlay => new GoogleBillingClient(OnPurchaseComplete),
                AppStore.AppleAppStore => new AppleAppStoreBillingClient(OnPurchaseComplete),
                _ => new FakeBillingClient()
            };
        }

        private void OnPurchaseComplete(BillingResult billingResult, Purchase purchase)
        {
            _actionList.Enqueue(() => { _onPurchaseComplete?.Invoke(billingResult, purchase); });
        }

        public void QueryInAppProducts(string[] productIds, Action<BillingResult, List<Product>> callback)
        {
            _billingClient.QueryInAppProducts(productIds,
                (billingResult, products) =>
                {
                    _actionList.Enqueue(() => { callback?.Invoke(billingResult, products); });
                });
        }

        public void QuerySubsProducts(string[] productIds, Action<BillingResult, List<Product>> callback)
        {
            _billingClient.QuerySubsProducts(productIds,
                (billingResult, products) =>
                {
                    _actionList.Enqueue(() => { callback?.Invoke(billingResult, products); });
                });
        }

        public void BuyInAppProduct(string productId, PurchaseOptions options)
        {
            _billingClient.BuyInAppProduct(productId, options);
        }

        public void BuySubsProduct(string productId, PurchaseOptions options)
        {
            _billingClient.BuySubsProduct(productId, options);
        }

        public void Consume(string purchaseToken, Action<BillingResult> callback)
        {
            _billingClient.Consume(purchaseToken,
                billingResult => { _actionList.Enqueue(() => { callback?.Invoke(billingResult); }); });
        }

        public void Acknowledge(string purchaseToken, Action<BillingResult> callback)
        {
            _billingClient.Acknowledge(purchaseToken,
                billingResult => { _actionList.Enqueue(() => { callback?.Invoke(billingResult); }); });
        }

        public void QueryPurchases(Action<BillingResult, List<Purchase>> callback)
        {
            _billingClient.QueryPurchases((billingResult, purchases) =>
            {
                _actionList.Enqueue(() => { callback?.Invoke(billingResult, purchases); });
            });
        }

        public void Update()
        {
            while (_actionList.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}