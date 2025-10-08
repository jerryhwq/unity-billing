#if UNITY_ANDROID && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GoogleBillingResult
    {
        public int ResponseCode { get; }
        public string DebugMessage { get; }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GoogleBillingResult(AndroidJavaObject javaObject)
        {
            ResponseCode = javaObject.Call<int>("getResponseCode");
            DebugMessage = javaObject.Call<string>("getDebugMessage");
        }

        public override string ToString()
        {
            return $"BillingResult{{ResponseCode={ResponseCode}, DebugMessage={DebugMessage}}}";
        }
#else
        private GoogleBillingResult()
        {
        }
#endif
    }
}