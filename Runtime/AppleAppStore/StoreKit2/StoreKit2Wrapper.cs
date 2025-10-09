#if UNITY_IOS && !UNITY_EDITOR
#define USE_APPLE_STOREKIT
#endif
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AOT;
using Newtonsoft.Json;
using UnityEngine;

namespace Enbug.Billing.AppleAppStore.StoreKit2
{
    internal static class StoreKit2Wrapper
    {
#if USE_APPLE_STOREKIT
        private const string LibName = "__Internal";
#endif
        private static readonly object Lock = new();
        private static int _maxId;
        private static string _priceStr;

        public static event Action<Transaction> TransactionUpdatedCallback;
        private static readonly ConcurrentDictionary<int, Action<Product[]>> QueryProductsCallbackDict = new();

        private static readonly ConcurrentDictionary<int, Action<AppleErrorCode?, AppleError, Transaction?>>
            PurchaseCallback = new();

        private static readonly ConcurrentDictionary<int, Action<bool>> FinishTransactionCallbackDict = new();

        private static readonly ConcurrentDictionary<int, Action<Transaction[]>>
            QueryPurchasesCallbackDict = new();

        private static QueryProductsDelegate _queryProductsDelegate;
        private static PurchaseDelegate _purchaseDelegate;
        private static FinishTransactionDelegate _finishTransactionDelegate;
        private static QueryPurchasesDelegate _queryPurchasesDelegate;
        private static FormatPriceDelegate _formatPriceDelegate;
        private static TransactionUpdatedDelegate _transactionUpdatedDelegate;

        public static bool IsSupported
        {
            get
            {
#if USE_APPLE_STOREKIT
                return enbug_iap_is_storekit2_supported() == 1;
#else
                return false;
#endif
            }
        }

        static StoreKit2Wrapper()
        {
            _queryProductsDelegate = OnQueryProducts;
            _purchaseDelegate = OnPurchase;
            _finishTransactionDelegate = OnFinishTransaction;
            _queryPurchasesDelegate = OnQueryPurchases;
            _formatPriceDelegate = OnFormatPrice;
            _transactionUpdatedDelegate = OnTransactionUpdated;
#if USE_APPLE_STOREKIT
            enbug_iap_storekit2_set_transaction_updated_callback(_transactionUpdatedDelegate);
#endif
        }

        private static int GetNextRequestId()
        {
            lock (Lock)
            {
                return _maxId++;
            }
        }

        public static void StartLoop()
        {
#if USE_APPLE_STOREKIT
            enbug_iap_storekit2_start_loop();
#endif
        }

        public static void RequestProducts(string[] productIdentifiers, Action<Product[]> callback)
        {
#if USE_APPLE_STOREKIT
            var requestId = GetNextRequestId();
            QueryProductsCallbackDict[requestId] = callback;
            var data = JsonConvert.SerializeObject(productIdentifiers);
            enbug_iap_storekit2_request_products(requestId, data, _queryProductsDelegate);
#endif
        }

        public static void Purchase(string productIdentifier, Product.PurchaseOption option,
            Action<AppleErrorCode?, AppleError, Transaction?> callback)
        {
#if USE_APPLE_STOREKIT
            var requestId = GetNextRequestId();
            PurchaseCallback[requestId] = callback;
            var optionStr = JsonConvert.SerializeObject(option);
            enbug_iap_storekit2_purchase(requestId, productIdentifier, optionStr, _purchaseDelegate);
#endif
        }

        public static void FinishTransaction(ulong id, Action<bool> callback)
        {
#if USE_APPLE_STOREKIT
            var requestId = GetNextRequestId();
            FinishTransactionCallbackDict[requestId] = callback;
            enbug_iap_storekit2_finish_transaction(requestId, id, _finishTransactionDelegate);
#endif
        }

        public static void GetTransactions(Action<Transaction[]> callback)
        {
#if USE_APPLE_STOREKIT
            var requestId = GetNextRequestId();
            QueryPurchasesCallbackDict[requestId] = callback;
            enbug_iap_storekit2_get_transactions(requestId, _queryPurchasesDelegate);
#endif
        }

        [MonoPInvokeCallback(typeof(QueryProductsDelegate))]
        private static void OnQueryProducts(int requestId, IntPtr data)
        {
            try
            {
                var products = JsonConvert.DeserializeObject<Product[]>(Marshal.PtrToStringUTF8(data));
                if (QueryProductsCallbackDict.TryRemove(requestId, out var callback))
                    callback?.Invoke(products);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(PurchaseDelegate))]
        private static void OnPurchase(int requestId, IntPtr result)
        {
            try
            {
                var transaction = JsonConvert.DeserializeObject<PurchaseCallbackData>(Marshal.PtrToStringUTF8(result));
                if (PurchaseCallback.TryRemove(requestId, out var callback))
                    callback?.Invoke(transaction.code, transaction.error, transaction.transaction);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(TransactionUpdatedDelegate))]
        private static void OnTransactionUpdated(IntPtr transactionJson)
        {
            try
            {
                var transaction = JsonConvert.DeserializeObject<Transaction>(Marshal.PtrToStringUTF8(transactionJson));
                TransactionUpdatedCallback?.Invoke(transaction);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(FinishTransactionDelegate))]
        private static void OnFinishTransaction(int requestId, int code)
        {
            try
            {
                if (FinishTransactionCallbackDict.TryRemove(requestId, out var callback))
                    callback?.Invoke(code == 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(QueryPurchasesDelegate))]
        private static void OnQueryPurchases(int requestId, IntPtr transactionsJson)
        {
            try
            {
                var transactions =
                    JsonConvert.DeserializeObject<Transaction[]>(Marshal.PtrToStringUTF8(transactionsJson));
                if (QueryPurchasesCallbackDict.TryRemove(requestId, out var callback))
                    callback?.Invoke(transactions);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(FormatPriceDelegate))]
        private static void OnFormatPrice(IntPtr priceStr)
        {
            try
            {
                _priceStr = Marshal.PtrToStringUTF8(priceStr);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private delegate void PurchaseDelegate(int requestId, IntPtr result);

        private delegate void TransactionUpdatedDelegate(IntPtr transactionJson);

        private delegate void QueryProductsDelegate(int requestId, IntPtr data);

        private delegate void FinishTransactionDelegate(int requestId, int code);

        private delegate void QueryPurchasesDelegate(int requestId, IntPtr transactionsJson);

        private delegate void FormatPriceDelegate(IntPtr priceStr);

#if USE_APPLE_STOREKIT
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit2_start_loop();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void
            enbug_iap_storekit2_set_transaction_updated_callback(TransactionUpdatedDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void
            enbug_iap_storekit2_request_products(int requestId, string data, QueryProductsDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit2_purchase(int requestId, string productIdentifier, string option,
            PurchaseDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void
            enbug_iap_storekit2_finish_transaction(int requestId, ulong id, FinishTransactionDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit2_get_transactions(int requestId, QueryPurchasesDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int enbug_iap_is_storekit2_supported();
#endif
    }
}