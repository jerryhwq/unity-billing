using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Enbug.Billing.AppleAppStore.StoreKit1;
using Enbug.Billing.AppleAppStore.StoreKit2;

namespace Enbug.Billing.AppleAppStore
{
    public class AppleAppStoreBillingClient : IBillingClient
    {
        private readonly Action<BillingResult, Purchase> _onPurchaseComplete;
        private readonly bool _useStoreKit2;

        public bool IsBillingSupported => true;
        public bool IsSubscriptionSupported => true;
        public bool IsSubscriptionsUpdateSupported => true;

        public AppleAppStoreBillingClient(Action<BillingResult, Purchase> onPurchaseComplete,
            bool enableStoreKit2 = true)
        {
            _useStoreKit2 = enableStoreKit2 && StoreKit2Wrapper.IsSupported;
            _onPurchaseComplete = onPurchaseComplete;

            if (_useStoreKit2)
            {
                StoreKit2Wrapper.TransactionUpdatedCallback += OnStoreKit2TransactionUpdated;
                StoreKit2Wrapper.StartLoop();
            }
            else
            {
                StoreKit1Wrapper.TransactionUpdatedCallback += OnStoreKit1TransactionUpdated;
            }
        }

        private void OnStoreKit1TransactionUpdated(SKPaymentTransaction[] transactions)
        {
            var receipt = StoreKit1Wrapper.Receipt;
            var purchases = transactions
                .Select(transaction => ConvertTransactionToPurchase(transaction, receipt))
                .ToList();

            foreach (var purchase in purchases)
            {
                var result = new BillingResult
                {
                    ResponseCode = BillingResult.OK,
                };
                _onPurchaseComplete?.Invoke(result, purchase);
            }
        }

        private void OnStoreKit2TransactionUpdated(Transaction transaction)
        {
            var purchase = ConvertTransactionToPurchase(transaction);

            var result = new BillingResult
            {
                ResponseCode = BillingResult.OK,
            };
            _onPurchaseComplete?.Invoke(result, purchase);
        }

        public void QueryInAppProducts(string[] skus, Action<BillingResult, List<Product>> callback)
        {
            if (_useStoreKit2)
            {
                StoreKit2Wrapper.RequestProducts(skus, appleProducts =>
                {
                    var result = new BillingResult
                    {
                        ResponseCode = BillingResult.OK,
                    };
                    var products = appleProducts
                        .Select(ConvertProduct)
                        .ToArray();
                    callback?.Invoke(result, products.ToList());
                });
            }
            else
            {
                StoreKit1Wrapper.RequestProducts(skus, productResponse =>
                {
                    var result = new BillingResult
                    {
                        ResponseCode = BillingResult.OK,
                    };

                    var products = productResponse.products
                        .Select(ConvertProduct)
                        .ToArray();
                    callback?.Invoke(result, products.ToList());
                });
            }
        }

        public void QuerySubsProducts(string[] skus, Action<BillingResult, List<Product>> callback)
        {
            if (_useStoreKit2)
            {
                StoreKit2Wrapper.RequestProducts(skus, appleProducts =>
                {
                    var result = new BillingResult
                    {
                        ResponseCode = BillingResult.OK,
                    };
                    var products = appleProducts
                        .Select(ConvertProduct)
                        .ToArray();
                    callback?.Invoke(result, products.ToList());
                });
            }
            else
            {
                StoreKit1Wrapper.RequestProducts(skus, productResponse =>
                {
                    var result = new BillingResult
                    {
                        ResponseCode = BillingResult.OK,
                    };

                    var products = productResponse.products
                        .Select(ConvertProduct)
                        .ToArray();
                    callback?.Invoke(result, products.ToList());
                });
            }
        }

        public void BuyInAppProduct(string sku, PurchaseOptions options)
        {
            if (_useStoreKit2)
            {
                var option = new StoreKit2.Product.PurchaseOption
                {
                    appAccountToken = options.UserIdentifier,
                };
                StoreKit2Wrapper.Purchase(sku, option, (code, error, transaction) =>
                {
                    Purchase purchase = null;
                    var result = new BillingResult();
                    if (transaction != null)
                    {
                        result.ResponseCode = BillingResult.OK;
                        purchase = ConvertTransactionToPurchase(transaction.Value);
                    }
                    else if (code != null)
                    {
                        result.ResponseCode = code == AppleErrorCode.SKErrorPaymentCancelled
                            ? BillingResult.USER_CANCELED
                            : BillingResult.ERROR;
                        result.RawObject = error;
                    }
                    else
                    {
                        result.ResponseCode = BillingResult.ERROR;
                        result.RawObject = error;
                    }

                    _onPurchaseComplete?.Invoke(result, purchase);
                });
            }
            else
            {
                var option = new PaymentOption
                {
                    applicationUsername = options.UserIdentifier,
                };
                StoreKit1Wrapper.AddPayment(sku, option, (success, code) =>
                {
                    if (success)
                        return;

                    var result = new BillingResult
                    {
                        ResponseCode = BillingResult.ERROR,
                        RawObject = code,
                    };
                    _onPurchaseComplete?.Invoke(result, null);
                });
            }
        }

        public void BuySubsProduct(string sku, PurchaseOptions options)
        {
            if (_useStoreKit2)
            {
                var option = new StoreKit2.Product.PurchaseOption
                {
                    appAccountToken = options.UserIdentifier,
                };
                StoreKit2Wrapper.Purchase(sku, option, (code, error, transaction) =>
                {
                    Purchase purchase = null;
                    var result = new BillingResult();
                    if (transaction != null)
                    {
                        result.ResponseCode = BillingResult.OK;
                        purchase = ConvertTransactionToPurchase(transaction.Value);
                    }
                    else if (code != null)
                    {
                        result.ResponseCode = code == AppleErrorCode.SKErrorPaymentCancelled
                            ? BillingResult.USER_CANCELED
                            : BillingResult.ERROR;
                        result.RawObject = error;
                    }
                    else
                    {
                        result.ResponseCode = BillingResult.ERROR;
                        result.RawObject = error;
                    }

                    _onPurchaseComplete?.Invoke(result, purchase);
                });
            }
            else
            {
                var option = new PaymentOption
                {
                    applicationUsername = options.UserIdentifier,
                };
                StoreKit1Wrapper.AddPayment(sku, option, (success, code) =>
                {
                    if (success)
                        return;

                    var result = new BillingResult
                    {
                        ResponseCode = BillingResult.ERROR,
                        RawObject = code,
                    };
                    _onPurchaseComplete?.Invoke(result, null);
                });
            }
        }

        public void Consume(string purchaseToken, Action<BillingResult> callback)
        {
            if (_useStoreKit2)
            {
                StoreKit2Wrapper.FinishTransaction(ulong.Parse(purchaseToken), success =>
                {
                    callback?.Invoke(new BillingResult
                    {
                        ResponseCode = success ? BillingResult.OK : BillingResult.ERROR,
                    });
                });
            }
            else
            {
                var success = StoreKit1Wrapper.FinishTransaction(purchaseToken);
                callback?.Invoke(new BillingResult
                {
                    ResponseCode = success ? BillingResult.OK : BillingResult.ERROR,
                });
            }
        }

        public void Acknowledge(string purchaseToken, Action<BillingResult> callback)
        {
            if (_useStoreKit2)
            {
                StoreKit2Wrapper.FinishTransaction(ulong.Parse(purchaseToken), success =>
                {
                    callback?.Invoke(new BillingResult
                    {
                        ResponseCode = success ? BillingResult.OK : BillingResult.ERROR,
                    });
                });
            }
            else
            {
                var success = StoreKit1Wrapper.FinishTransaction(purchaseToken);
                callback?.Invoke(new BillingResult
                {
                    ResponseCode = success ? BillingResult.OK : BillingResult.ERROR,
                });
            }
        }

        public void QueryPurchases(Action<BillingResult, List<Purchase>> callback)
        {
            if (_useStoreKit2)
            {
                StoreKit2Wrapper.GetTransactions(transactions =>
                {
                    var result = new BillingResult
                    {
                        ResponseCode = BillingResult.OK,
                    };
                    var purchases = transactions
                        .Select(ConvertTransactionToPurchase)
                        .ToList();
                    callback?.Invoke(result, purchases);
                });
            }
            else
            {
                var transactions = StoreKit1Wrapper.Transactions;
                var receipt = StoreKit1Wrapper.Receipt;
                var purchases = transactions
                    .Select(transaction => ConvertTransactionToPurchase(transaction, receipt))
                    .ToList();

                var result = new BillingResult
                {
                    ResponseCode = BillingResult.OK,
                };
                callback?.Invoke(result, purchases);
            }
        }

        ~AppleAppStoreBillingClient()
        {
            if (_useStoreKit2)
                StoreKit2Wrapper.TransactionUpdatedCallback -= OnStoreKit2TransactionUpdated;
            else
                StoreKit1Wrapper.TransactionUpdatedCallback -= OnStoreKit1TransactionUpdated;
        }

        private Product ConvertProduct(StoreKit2.Product product)
        {
            return new Product
            {
                ProductId = product.id,
                DisplayPrice = product.displayPrice,
                CurrencyCode = product.currencyCode,
                CurrencySymbol =  product.currencySymbol,
                Price = product.price,
                Title = product.displayName,
                Description = product.description,
                RawObject = product,
            };
        }

        private Product ConvertProduct(SKProduct product)
        {
            return new Product
            {
                ProductId = product.productIdentifier,
                DisplayPrice = product.displayPrice,
                CurrencyCode = product.currencyCode,
                CurrencySymbol = product.currencySymbol,
                Price = product.price,
                Title = product.localizedTitle,
                Description = product.localizedDescription,
                RawObject = product,
            };
        }

        private Environment ConvertEnvironment(StoreKit2.Environment? environment)
        {
            switch (environment)
            {
                case StoreKit2.Environment.production:
                    return Environment.Production;
                case StoreKit2.Environment.sandbox:
                case StoreKit2.Environment.xcode:
                    return Environment.Sandbox;
                default:
                    return Environment.Unknown;
            }
        }

        private Purchase ConvertTransactionToPurchase(Transaction transaction)
        {
            return new Purchase
            {
                ConsumeId = transaction.id.ToString("D0", CultureInfo.InvariantCulture),
                OrderId = transaction.id.ToString("D0", CultureInfo.InvariantCulture),
                ProductId = transaction.productID,
                Environment = ConvertEnvironment(transaction.environment),
            };
        }

        private Purchase ConvertTransactionToPurchase(SKPaymentTransaction transaction, string receipt)
        {
            return new Purchase
            {
                ConsumeId = transaction.transactionIdentifier,
                OrderId = transaction.transactionIdentifier,
                ProductId = transaction.payment.productIdentifier,
                Receipt = receipt,
            };
        }
    }
}