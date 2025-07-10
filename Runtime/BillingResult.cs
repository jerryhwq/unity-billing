namespace Enbug.Billing
{
    public class BillingResult
    {
        public const int OK = 0;
        public const int USER_CANCELED = 1;
        public const int ERROR = 2;

        public int ResponseCode { get; set; }

        public object RawObject { get; set; }
    }
}