using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Enbug.Billing.Editor
{
    public class BillingBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var config = EnbugBillingEditor.CurrentPlatformInfo;
            EnbugBillingEditor.SetAppStore(config.appStore);
        }
    }
}