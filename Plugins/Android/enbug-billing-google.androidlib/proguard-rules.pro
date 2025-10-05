-keep public class com.android.billingclient.api.** { public *; }

-keeppackagenames io.enbug.billing.google.**

-keep interface androidx.core.util.Consumer {
    void accept(...);
}

-keep class io.enbug.billing.google.PurchaseOptions {
    public <init>();
    public void setObfuscatedAccountId(java.lang.String);
    public java.lang.String getObfuscatedAccountId();
    public void setObfuscatedProfileId(java.lang.String);
    public java.lang.String getObfuscatedProfileId();
}

-keep class io.enbug.billing.google.BillingClient {
    public <init>(com.android.billingclient.api.PurchasesUpdatedListener);
    public boolean isSubscriptionsSupported();
    public boolean isSubscriptionsUpdateSupported();
    public boolean isProductDetailsSupported();
    public void queryPurchases(java.lang.String, com.android.billingclient.api.PurchasesResponseListener);
    public void queryProductDetails(java.lang.String, java.util.List, com.android.billingclient.api.ProductDetailsResponseListener);
    public void buyInAppProduct(android.app.Activity, java.lang.String, io.enbug.billing.google.PurchaseOptions, androidx.core.util.Consumer);
    public void buySubsProduct(android.app.Activity, java.lang.String, io.enbug.billing.google.PurchaseOptions, androidx.core.util.Consumer);
    public void upgradeSubsProduct(android.app.Activity, java.lang.String, java.lang.String, int, io.enbug.billing.google.PurchaseOptions, androidx.core.util.Consumer);
    public void consume(java.lang.String, com.android.billingclient.api.ConsumeResponseListener);
    public void acknowledge(java.lang.String, com.android.billingclient.api.AcknowledgePurchaseResponseListener);
    public void endConnection();
}
