#if UNITY_IOS && !UNITY_EDITOR
#define USE_APPLE_STOREKIT
#endif
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AOT;
using Newtonsoft.Json;
using UnityEngine;

namespace Enbug.Billing.AppleAppStore.StoreKit1
{
    public static class StoreKit1Wrapper
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string LibName = "__Internal";
#endif
        public static event Action<SKPaymentTransaction[]> TransactionUpdatedCallback;
        private static readonly ConcurrentDictionary<int, Action<SKProductsResponse>> QueryProductsCallbacks = new();
        private static readonly ConcurrentDictionary<int, Action<bool, long>> AddPaymentCallbacks = new();
        private static string _receipt;
        private static SKPaymentTransaction[] _transactions;

        private static readonly object Lock = new();
        private static int _maxId;

        private static AddPaymentDelegate _addPaymentDelegate;
        private static QueryProductsDelegate _queryProductsDelegate;
        private static TransactionUpdatedDelegate _transactionUpdatedDelegate;
        private static GetReceiptDelegate _getReceiptDelegate;
        private static GetTransactionsDelegate _getTransactionsDelegate;

        static StoreKit1Wrapper()
        {
            _addPaymentDelegate = OnAddPayment;
            _queryProductsDelegate = OnQueryProducts;
            _transactionUpdatedDelegate = OnTransactionUpdated;
            _getReceiptDelegate = OnGetReceipt;
            _getTransactionsDelegate = OnGetTransaction;
#if USE_APPLE_STOREKIT
            enbug_iap_storekit1_start_listener(_transactionUpdatedDelegate);
#endif
        }

        private static int GetNextRequestId()
        {
            lock (Lock)
            {
                return _maxId++;
            }
        }

        public static string Receipt
        {
            get
            {
                string receipt = null;
                lock (Lock)
                {
#if USE_APPLE_STOREKIT
                    enbug_iap_storekit1_get_receipt(_getReceiptDelegate);
#endif
                    receipt = _receipt;
                    _receipt = null;
                }

                return receipt;
            }
        }

        public static SKPaymentTransaction[] Transactions
        {
            get
            {
                SKPaymentTransaction[] transactions;
                lock (Lock)
                {
#if USE_APPLE_STOREKIT
                    enbug_iap_storekit1_get_transactions(_getTransactionsDelegate);
#endif
                    transactions = _transactions;
                    _transactions = null;
                }

                return transactions ?? Array.Empty<SKPaymentTransaction>();
            }
        }

        public static void RequestProducts(string[] productIdentifiers, Action<SKProductsResponse> callback)
        {
#if USE_APPLE_STOREKIT
            var requestId = GetNextRequestId();
            QueryProductsCallbacks[requestId] = callback;
            enbug_iap_storekit1_request_products(requestId, JsonConvert.SerializeObject(productIdentifiers),
                _queryProductsDelegate);
#endif
        }

        public static void AddPayment(string productIdentifier, PaymentOption options,
            Action<bool, long> callback)
        {
#if USE_APPLE_STOREKIT
            var requestId = GetNextRequestId();
            AddPaymentCallbacks[requestId] = callback;
            var optionStr = JsonConvert.SerializeObject(options);
            enbug_iap_storekit1_add_payment(requestId, productIdentifier, optionStr, _addPaymentDelegate);
#endif
        }

        public static bool FinishTransaction(string transactionIdentifier)
        {
#if USE_APPLE_STOREKIT
            return enbug_iap_storekit1_finish_transaction(transactionIdentifier) == 1;
#else
            return false;
#endif
        }

        [MonoPInvokeCallback(typeof(QueryProductsDelegate))]
        private static void OnQueryProducts(int requestId, IntPtr data)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<SKProductsResponse>(Marshal.PtrToStringUTF8(data));
                if (QueryProductsCallbacks.TryRemove(requestId, out var callback))
                    callback?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(TransactionUpdatedDelegate))]
        private static void OnTransactionUpdated(IntPtr json)
        {
            try
            {
                var transactions = JsonConvert.DeserializeObject<SKPaymentTransaction[]>(Marshal.PtrToStringUTF8(json));
                TransactionUpdatedCallback?.Invoke(transactions);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(AddPaymentDelegate))]
        private static void OnAddPayment(int requestId, int success, long code)
        {
            try
            {
                if (AddPaymentCallbacks.TryRemove(requestId, out var callback))
                    callback?.Invoke(success == 1, code);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(GetReceiptDelegate))]
        private static void OnGetReceipt(IntPtr receipt)
        {
            try
            {
                _receipt = Marshal.PtrToStringUTF8(receipt);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(GetTransactionsDelegate))]
        private static void OnGetTransaction(IntPtr transactionsJson)
        {
            try
            {
                _transactions =
                    JsonConvert.DeserializeObject<SKPaymentTransaction[]>(Marshal.PtrToStringUTF8(transactionsJson));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private delegate void TransactionUpdatedDelegate(IntPtr json);

        private delegate void QueryProductsDelegate(int requestId, IntPtr data);

        private delegate void AddPaymentDelegate(int requestId, int success, long code);

        private delegate void GetReceiptDelegate(IntPtr receipt);

        private delegate void GetTransactionsDelegate(IntPtr transactionsJson);

#if USE_APPLE_STOREKIT
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit1_start_listener(TransactionUpdatedDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit1_request_products(int requestId, string data,
            QueryProductsDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit1_add_payment(int requestId, string productIdentifier, string options,
            AddPaymentDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int enbug_iap_storekit1_finish_transaction(string transactionIdentifier);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit1_get_receipt(GetReceiptDelegate callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void enbug_iap_storekit1_get_transactions(GetTransactionsDelegate callback);
#endif
    }
}