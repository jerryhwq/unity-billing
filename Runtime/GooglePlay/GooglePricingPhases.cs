#if UNITY_ANDROID && !UNITY_EDITOR && ENABLE_UNITY_ANDROID_JNI
#define ENABLE_GOOGLE_PLAY_BILLING
#endif

using System.Collections.Generic;
#if ENABLE_GOOGLE_PLAY_BILLING
using UnityEngine;
#endif

namespace Enbug.Billing.GooglePlay
{
    public class GooglePricingPhases
    {
        public List<GooglePricingPhase> PricingPhaseList { get; }

        private GooglePricingPhases()
        {
        }

#if ENABLE_GOOGLE_PLAY_BILLING
        public GooglePricingPhases(AndroidJavaObject javaPricingPhases)
        {
            using var javaPricingPhaseList = javaPricingPhases.Call<AndroidJavaObject>("getPricingPhaseList");

            PricingPhaseList = new List<GooglePricingPhase>();
            var pricingPhaseListSize = javaPricingPhaseList.Call<int>("size");
            for (var i = 0; i < pricingPhaseListSize; i++)
            {
                using var javaPricingPhase = javaPricingPhaseList.Call<AndroidJavaObject>("get", i);
                PricingPhaseList.Add(new GooglePricingPhase(javaPricingPhase));
            }
        }
#endif
    }
}