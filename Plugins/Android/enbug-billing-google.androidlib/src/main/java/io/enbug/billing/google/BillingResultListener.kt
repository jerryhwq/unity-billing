package io.enbug.billing.google

import com.android.billingclient.api.BillingResult

fun interface BillingResultListener {
    fun invoke(billingResult: BillingResult)
}