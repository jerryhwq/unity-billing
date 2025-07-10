using System;
using System.Collections.Concurrent;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using AOT;
using Newtonsoft.Json;
using UnityEngine;

namespace Enbug.Billing.AppleAppStore.StoreKit1
{
    public class StoreKit1Wrapper
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string LibName = "__Internal";
#endif
        public static event Action<SKPaymentTransaction[]> TransactionUpdatedCallback;
        private static readonly ConcurrentDictionary<int, Action<SKProductsResponse>> QueryProductsCallbacks = new();
        private static readonly ConcurrentDictionary<int, Action<bool, int>> AddPaymentCallbacks = new();
        private static string _receipt;
        private static SKPaymentTransaction[] _transactions;

        private static readonly object Lock = new();
        private static int _maxId;

        static StoreKit1Wrapper()
        {
#if UNITY_IOS && !UNITY_EDITOR
            enbug_iap_storekit1_start_listener(OnTransactionUpdated);
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
#if UNITY_IOS && !UNITY_EDITOR
                    enbug_iap_storekit1_get_receipt(OnGetReceipt);
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
#if UNITY_IOS && !UNITY_EDITOR
                    enbug_iap_storekit1_get_transactions(OnGetTransaction);
#endif
                    transactions = _transactions;
                    _transactions = null;
                }

                return transactions ?? Array.Empty<SKPaymentTransaction>();
            }
        }

        public static void RequestProducts(string[] productIdentifiers, Action<SKProductsResponse> callback)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var requestId = GetNextRequestId();
            QueryProductsCallbacks[requestId] = callback;
            enbug_iap_storekit1_request_products(requestId, JsonConvert.SerializeObject(productIdentifiers),
                OnQueryProducts);
#endif
        }

        public static void AddPayment(string productIdentifier, PaymentOption options,
            Action<bool, int> callback)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var requestId = GetNextRequestId();
            AddPaymentCallbacks[requestId] = callback;
            var optionStr = JsonConvert.SerializeObject(options);
            enbug_iap_storekit1_add_payment(requestId, productIdentifier, optionStr, OnAddPayment);
#endif
        }

        public static bool FinishTransaction(string transactionIdentifier)
        {
#if UNITY_IOS && !UNITY_EDITOR
            return enbug_iap_storekit1_finish_transaction(transactionIdentifier) == 1;
#else
            return false;
#endif
        }

        [MonoPInvokeCallback(typeof(QueryProductsDelegate))]
        private static void OnQueryProducts(int requestId, string data)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<SKProductsResponse>(data);
                if (QueryProductsCallbacks.TryRemove(requestId, out var callback))
                    callback?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(TransactionUpdatedDelegate))]
        private static void OnTransactionUpdated(string json)
        {
            try
            {
                var transactions = JsonConvert.DeserializeObject<SKPaymentTransaction[]>(json);
                TransactionUpdatedCallback?.Invoke(transactions);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(AddPaymentDelegate))]
        private static void OnAddPayment(int requestId, int success, int code)
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
        private static void OnGetReceipt(string receipt)
        {
            _receipt = receipt;
        }

        [MonoPInvokeCallback(typeof(GetTransactionsDelegate))]
        private static void OnGetTransaction(string transactionsJson)
        {
            _transactions = JsonConvert.DeserializeObject<SKPaymentTransaction[]>(transactionsJson);
        }

        private delegate void TransactionUpdatedDelegate(string json);

        private delegate void QueryProductsDelegate(int requestId, string data);

        private delegate void AddPaymentDelegate(int requestId, int success, int code);

        private delegate void GetReceiptDelegate(string receipt);

        private delegate void GetTransactionsDelegate(string transactionsJson);

#if UNITY_IOS && !UNITY_EDITOR
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