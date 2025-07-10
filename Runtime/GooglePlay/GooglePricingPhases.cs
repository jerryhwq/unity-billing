using System.Collections.Generic;
using UnityEngine;

namespace Enbug.Billing.GooglePlay
{
    public class GooglePricingPhases
    {
        public List<GooglePricingPhase> PricingPhaseList { get; }

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
    }
}