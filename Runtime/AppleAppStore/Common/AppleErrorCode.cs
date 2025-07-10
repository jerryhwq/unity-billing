namespace Enbug.Billing.AppleAppStore
{
    public enum AppleErrorCode
    {
        SKErrorUnknown = 0,
        SKErrorClientInvalid = 1, // client is not allowed to issue the request, etc.
        SKErrorPaymentCancelled = 2, // user cancelled the request, etc.
        SKErrorPaymentInvalid = 3, // purchase identifier was invalid, etc.
        SKErrorPaymentNotAllowed = 4, // this device is not allowed to make the payment
        SKErrorStoreProductNotAvailable = 5, // Product is not available in the current storefront
        SKErrorCloudServicePermissionDenied = 6, // user has not allowed access to cloud service information
        SKErrorCloudServiceNetworkConnectionFailed = 7, // the device could not connect to the nework
        SKErrorCloudServiceRevoked = 8, // user has revoked permission to use this cloud service
        SKErrorPrivacyAcknowledgementRequired = 9, // The user needs to acknowledge Apple's privacy policy
        SKErrorUnauthorizedRequestData = 10, // The app is attempting to use SKPayment's requestData property, but does not have the appropriate entitlement
        SKErrorInvalidOfferIdentifier = 11, // The specified subscription offer identifier is not valid
        SKErrorInvalidSignature = 12, // The cryptographic signature provided is not valid
        SKErrorMissingOfferParams = 13, // One or more parameters from SKPaymentDiscount is missing
        SKErrorInvalidOfferPrice = 14, // The price of the selected offer is not valid (e.g. lower than the current base subscription price)
        SKErrorOverlayCancelled = 15,
        SKErrorOverlayInvalidConfiguration = 16,
        SKErrorOverlayTimeout = 17,
        SKErrorIneligibleForOffer = 18, // User is not eligible for the subscription offer
        SKErrorUnsupportedPlatform = 19,
        SKErrorOverlayPresentedInBackgroundScene = 20, // Client tried to present an SKOverlay in UIWindowScene not in the foreground
    }
}