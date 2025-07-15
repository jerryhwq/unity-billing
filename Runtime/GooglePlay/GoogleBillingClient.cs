using System;
using System.Collections.Generic;
using System.Linq;

namespace Enbug.Billing.GooglePlay
{
    public class GoogleBillingClient : IBillingClient
    {
        private readonly GoogleBillingClientWrapper _billingClientWrapper;
        private readonly Action<BillingResult, Purchase> _onPurchaseComplete;

        public GoogleBillingClient(Action<BillingResult, Purchase> onPurchaseComplete)
        {
            _onPurchaseComplete = onPurchaseComplete;
            _billingClientWrapper = new GoogleBillingClientWrapper(OnPurchasesUpdated);
        }

        public bool IsBillingSupported => _billingClientWrapper.IsProductDetailsSupported;
        public bool IsSubscriptionSupported => _billingClientWrapper.IsSubscriptionSupported;
        public bool IsSubscriptionsUpdateSupported => _billingClientWrapper.IsSubscriptionsUpdateSupported;

        public void QueryInAppProducts(string[] productIds, Action<BillingResult, List<Product>> callback)
        {
            _billingClientWrapper.QueryProductDetails(GoogleProductType.INAPP, productIds,
                (billingResult, googleProductDetailsList) =>
                {
                    var result = ConvertBillingResult(billingResult);
                    if (result.ResponseCode != BillingResult.OK ||
                        googleProductDetailsList == null)
                    {
                        callback.Invoke(result, null);
                        return;
                    }

                    var products = googleProductDetailsList
                        .Select(googleProductDetails => new Product
                        {
                            DisplayPrice = googleProductDetails.OneTimePurchaseOfferDetails?.FormattedPrice,
                            CurrencyCode = googleProductDetails.OneTimePurchaseOfferDetails?.PriceCurrencyCode,
                            CurrencySymbol = _billingClientWrapper.ConvertCurrencyCodeToSymbol(googleProductDetails.OneTimePurchaseOfferDetails?.PriceCurrencyCode),
                            Price =  _billingClientWrapper.ConvertMicrosToDecimal(googleProductDetails.OneTimePurchaseOfferDetails?.PriceAmountMicros),
                            ProductId = googleProductDetails.ProductId,
                            Title = googleProductDetails.Title,
                            Description = googleProductDetails.Description,
                            RawObject = googleProductDetails,
                        })
                        .ToList();
                    callback?.Invoke(result, products);
                });
        }

        public void QuerySubsProducts(string[] productIds, Action<BillingResult, List<Product>> callback)
        {
            _billingClientWrapper.QueryProductDetails(GoogleProductType.SUBS, productIds,
                (billingResult, googleProductDetailsList) =>
                {
                    var result = ConvertBillingResult(billingResult);
                    if (result.ResponseCode != BillingResult.OK ||
                        googleProductDetailsList == null)
                    {
                        callback.Invoke(result, null);
                        return;
                    }

                    var products = googleProductDetailsList
                        .Select(googleProductDetails =>
                        {
                            var product = new Product
                            {
                                DisplayPrice = null,
                                ProductId = googleProductDetails.ProductId,
                                Title = googleProductDetails.Title,
                                Description = googleProductDetails.Description,
                                RawObject = googleProductDetails,
                            };

                            if (googleProductDetails.SubscriptionOfferDetails is { Count: > 0 })
                            {
                                var offerDetails = googleProductDetails.SubscriptionOfferDetails[0];
                                if (offerDetails.PricingPhases?.PricingPhaseList is { Count: > 0 })
                                {
                                    var pricingPhase = offerDetails.PricingPhases.PricingPhaseList[0];
                                    product.DisplayPrice = pricingPhase.FormattedPrice;
                                }
                            }

                            return product;
                        })
                        .ToList();

                    callback?.Invoke(result, products);
                });
        }

        public void BuyInAppProduct(string productId, PurchaseOptions options)
        {
            _billingClientWrapper.BuyInAppProduct(productId, options, OnPurchaseCallback);
        }

        public void BuySubsProduct(string productId, PurchaseOptions options)
        {
            _billingClientWrapper.BuySubsProduct(productId, options, OnPurchaseCallback);
        }

        public void Consume(string purchaseToken, Action<BillingResult> callback)
        {
            _billingClientWrapper.Consume(purchaseToken,
                (billingResult, _) => { callback.Invoke(ConvertBillingResult(billingResult)); });
        }

        public void Acknowledge(string purchaseToken, Action<BillingResult> callback)
        {
            _billingClientWrapper.Acknowledge(purchaseToken,
                billingResult => { callback.Invoke(ConvertBillingResult(billingResult)); });
        }

        public void QueryPurchases(Action<BillingResult, List<Purchase>> callback)
        {
            _billingClientWrapper.QueryPurchases(
                GoogleProductType.INAPP,
                (googleBillingResult, googlePurchases) =>
                {
                    var billingResult = ConvertBillingResult(googleBillingResult);
                    if (billingResult.ResponseCode != BillingResult.OK)
                    {
                        callback.Invoke(billingResult, null);
                        return;
                    }

                    var purchases = googlePurchases
                        .Where(googlePurchase => googlePurchase.PurchaseState == GooglePurchaseState.PURCHASED)
                        .Select(ConvertPurchase)
                        .ToList();

                    if (!IsSubscriptionSupported)
                    {
                        callback.Invoke(billingResult, purchases);
                        return;
                    }

                    _billingClientWrapper.QueryPurchases(
                        GoogleProductType.SUBS,
                        (googleBillingResult, googlePurchases) =>
                        {
                            var billingResult = ConvertBillingResult(googleBillingResult);
                            if (billingResult.ResponseCode != BillingResult.OK)
                            {
                                callback.Invoke(billingResult, null);
                                return;
                            }

                            var subsPurchases = googlePurchases
                                .Where(googlePurchase => googlePurchase.PurchaseState == GooglePurchaseState.PURCHASED)
                                .Select(ConvertPurchase)
                                .ToList();

                            purchases.AddRange(subsPurchases);
                            callback.Invoke(billingResult, purchases);
                        });
                });
        }

        private void OnPurchaseCallback(GoogleBillingResult billingResult)
        {
            if (billingResult.ResponseCode == GoogleBillingResponseCode.OK) return;
            _onPurchaseComplete.Invoke(ConvertBillingResult(billingResult), null);
        }

        private static BillingResult ConvertBillingResult(GoogleBillingResult billingResult)
        {
            var rawResponseCode = billingResult.ResponseCode;
            var responseCode = rawResponseCode switch
            {
                GoogleBillingResponseCode.OK => BillingResult.OK,
                GoogleBillingResponseCode.USER_CANCELED => BillingResult.USER_CANCELED,
                _ => BillingResult.ERROR
            };

            return new BillingResult
            {
                ResponseCode = responseCode,
                RawObject = billingResult,
            };
        }

        private static Purchase ConvertPurchase(GooglePurchase googlePurchase)
        {
            return new Purchase
            {
                ConsumeId = googlePurchase.PurchaseToken,
                OrderId = googlePurchase.OrderId,
                ProductId = googlePurchase.Products[0],
                PurchaseToken = googlePurchase.PurchaseToken,
                Signature = googlePurchase.Signature,
                OriginalJson = googlePurchase.OriginalJson,
            };
        }

        private void OnPurchasesUpdated(GoogleBillingResult billingResult, List<GooglePurchase> googlePurchases)
        {
            var result = ConvertBillingResult(billingResult);

            if (googlePurchases is not { Count: > 0 })
            {
                _onPurchaseComplete.Invoke(result, null);
                return;
            }

            foreach (var googlePurchase in googlePurchases)
            {
                var purchase = ConvertPurchase(googlePurchase);
                _onPurchaseComplete?.Invoke(result, purchase);
            }
        }
    }
}