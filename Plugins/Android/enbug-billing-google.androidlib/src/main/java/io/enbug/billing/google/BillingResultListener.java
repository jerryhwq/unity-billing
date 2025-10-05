package io.enbug.billing.google;

import androidx.annotation.NonNull;

import com.android.billingclient.api.BillingResult;

public interface BillingResultListener {
    void invoke(@NonNull BillingResult billingResult);
}
