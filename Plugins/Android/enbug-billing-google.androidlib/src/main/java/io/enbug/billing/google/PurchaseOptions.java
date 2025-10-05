package io.enbug.billing.google;

import androidx.annotation.Nullable;

public class PurchaseOptions {
    @Nullable
    private String obfuscatedAccountId;
    @Nullable
    private String obfuscatedProfileId;

    @Nullable
    public String getObfuscatedAccountId() {
        return obfuscatedAccountId;
    }

    public void setObfuscatedAccountId(@Nullable String obfuscatedAccountId) {
        this.obfuscatedAccountId = obfuscatedAccountId;
    }

    @Nullable
    public String getObfuscatedProfileId() {
        return obfuscatedProfileId;
    }

    public void setObfuscatedProfileId(@Nullable String obfuscatedProfileId) {
        this.obfuscatedProfileId = obfuscatedProfileId;
    }
}
