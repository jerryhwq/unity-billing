namespace Enbug.Billing.AppleAppStore
{
    public class AppleError
    {
        public AppleErrorCode code;
        public string localizedDescription;

        public override string ToString()
        {
            return $"AppleError {{code={code}, localizedDescription={localizedDescription}}}";
        }
    }
}