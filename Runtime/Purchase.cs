using System.Text;

namespace Enbug.Billing
{
    public class Purchase
    {
        public string ConsumeId;

        public string OrderId;
        public string ProductId;

        // Google Play
        public string PurchaseToken;
        public string OriginalJson;
        public string Signature;

        // iOS App Store (StoreKit 2)
        public Environment Environment;

        // iOS App Store (StoreKit 1), server can work without this
        public string Receipt;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Purchase {{orderId={OrderId}, productId={ProductId}");

            if (PurchaseToken != null)
            {
                sb.Append($", purchaseToken={PurchaseToken}");
            }

            if (OriginalJson != null)
            {
                sb.Append($", originalJson={OriginalJson}");
            }

            if (Signature != null)
            {
                sb.Append($", signature={Signature}");
            }

            if (Receipt != null)
            {
                sb.Append($", receipt={Receipt}");
            }

            sb.Append("}");

            return sb.ToString();
        }
    }
}