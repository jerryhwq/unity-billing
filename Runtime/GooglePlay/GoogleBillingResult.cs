using UnityEngine;

namespace Enbug.Billing.GooglePlay
{
    public class GoogleBillingResult
    {
        public int ResponseCode { get; }
        public string DebugMessage { get; }

        public GoogleBillingResult(AndroidJavaObject javaObject)
        {
            ResponseCode = javaObject.Call<int>("getResponseCode");
            DebugMessage = javaObject.Call<string>("getDebugMessage");
        }

        public override string ToString()
        {
            return $"BillingResult{{ResponseCode={ResponseCode}, DebugMessage={DebugMessage}}}";
        }
    }
}