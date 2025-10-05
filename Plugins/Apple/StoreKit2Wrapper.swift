import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private enum PaymentResult {
    case code(SKError.Code)
    case error(Error)
    case transation(Transaction)
}

nonisolated(unsafe) private var initialized: Bool = false
nonisolated(unsafe) private var transactionUpudated: ((UnsafePointer<CChar>) -> Void)? = nil

private func getCurrencySymbol(currencyCode: String) -> String {
    let formatter = NumberFormatter()
    formatter.numberStyle = .currency
    formatter.currencyCode = currencyCode
    return formatter.currencySymbol
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(offerType: Transaction.OfferType) -> Int32 {
    switch offerType {
    case .introductory:
        return 0
    case .promotional:
        return 1
    case .winBack:
        return 2
    default:
        return -1
    }
}

@available(iOS 17.2, macOS 14.2, tvOS 17.2, watchOS 10.2, visionOS 1.1, *)
private func convert(offer: Transaction.Offer) -> [String: Any] {
    var offerDict: [String: Any] = [
        "type": convert(offerType: offer.type)
    ]

    if let id = offer.id {
        offerDict["id"] = id
    }

    if let paymentMode = offer.paymentMode {
        offerDict["paymentMode"] = paymentMode
    }

    return offerDict
}

@available(iOS 16.0, macOS 13.0, tvOS 16.0, watchOS 9.0, visionOS 1.0, *)
private func convert(environment: AppStore.Environment) -> Int32 {
    switch environment {
    case .production:
        return 0
    case .sandbox:
        return 1
    case .xcode:
        return 2
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(environment: String) -> Int32 {
    switch environment {
    case "Production":
        return 0
    case "Sandbox":
        return 1
    case "Xcode":
        return 2
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(ownershipType: Transaction.OwnershipType) -> Int32 {
    switch ownershipType {
    case .purchased:
        return 0
    case .familyShared:
        return 1
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(revocationReason: Transaction.RevocationReason) -> Int32 {
    switch revocationReason {
    case .developerIssue:
        return 0
    case .other:
        return 1
    default:
        return -1
    }
}

@available(iOS 17.0, macOS 14.0, tvOS 17.0, watchOS 10.0, *)
private func convert(reason: Transaction.Reason) -> Int32 {
    switch reason {
    case .purchase:
        return 0
    case .renewal:
        return 1
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(reason: String) -> Int32 {
    switch reason {
    case "PURCHASE":
        return 0
    case "RENEWAL":
        return 1
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(unit: Product.SubscriptionPeriod.Unit) -> Int32 {
    switch unit {
    case .day:
        return 0
    case .week:
        return 1
    case .month:
        return 2
    case .year:
        return 3
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(transaction: Transaction) -> [String: Any] {
    var transactionDict: [String: Any] = [
        "id": transaction.id,
        "originalID": transaction.originalID,
        "productID": transaction.productID,
        "appBundleID": transaction.appBundleID,
        "purchaseDate": transaction.purchaseDate.timeIntervalSince1970,
        "originalPurchaseDate": transaction.originalPurchaseDate.timeIntervalSince1970,
        "purchasedQuantity": transaction.purchasedQuantity,
        "isUpgraded": transaction.isUpgraded,
        "productType": convert(productType: transaction.productType),
        "deviceVerificationNonce": transaction.deviceVerificationNonce.uuidString,
        "ownershipType": convert(ownershipType: transaction.ownershipType),
        "signedDate": transaction.signedDate.timeIntervalSince1970,
    ]

    if let jsonRepresentation = String(data: transaction.jsonRepresentation, encoding: .utf8) {
        transactionDict["jsonRepresentation"] = jsonRepresentation
    }

    if let webOrderLineItemID = transaction.webOrderLineItemID {
        transactionDict["webOrderLineItemID"] = webOrderLineItemID
    }

    if let subscriptionGroupID = transaction.subscriptionGroupID {
        transactionDict["subscriptionGroupID"] = subscriptionGroupID
    }

    if let expirationDate = transaction.expirationDate {
        transactionDict["expirationDate"] = expirationDate.timeIntervalSince1970
    }

    if #available(iOS 17.2, macOS 14.2, tvOS 17.2, watchOS 10.2, visionOS 1.1, *) {
        if let offer = transaction.offer {
            transactionDict["offer"] = convert(offer: offer)
        }
    }

    if let offerType = transaction.offerType {
        transactionDict["offerType"] = convert(offerType: offerType)
    }

    if let offerID = transaction.offerID {
        transactionDict["offerID"] = offerID
    }

    if let paymentMode = transaction.offerPaymentModeStringRepresentation {
        transactionDict["offerPaymentModeStringRepresentation"] = paymentMode
    }

    if let revocationDate = transaction.revocationDate {
        transactionDict["revocationDate"] = revocationDate.timeIntervalSince1970
    }

    if let revocationReason = transaction.revocationReason {
        transactionDict["revocationReason"] = convert(revocationReason: revocationReason)
    }

    if let appAccountToken = transaction.appAccountToken {
        transactionDict["appAccountToken"] = appAccountToken.uuidString
    }

    if #available(iOS 16.0, macOS 13.0, tvOS 16.0, watchOS 9.0, visionOS 1.0, *) {
        transactionDict["environment"] = convert(environment: transaction.environment)
    } else {
        transactionDict["environment"] = convert(environment: transaction.environmentStringRepresentation)
    }

    if #available(iOS 17.0, macOS 14.0, tvOS 17.0, watchOS 10.0, *) {
        transactionDict["reason"] = convert(reason: transaction.reason)
    } else {
        transactionDict["reason"] = convert(reason: transaction.reasonStringRepresentation)
    }

    if #available(iOS 17.0, macOS 14.0, tvOS 17.0, watchOS 10.0, *) {
        transactionDict["storefrontCountryCode"] = transaction.storefront.countryCode
    } else {
        transactionDict["storefrontCountryCode"] = transaction.storefrontCountryCode
    }

    if let price = transaction.price {
        transactionDict["price"] = price
    }

    if #available(iOS 16.0, macOS 13.0, tvOS 16.0, watchOS 9.0, visionOS 1.0, *) {
        if let currency = transaction.currency {
            transactionDict["currencyCode"] = currency.identifier
        }
    } else {
        if let currencyCode = transaction.currencyCode {
            transactionDict["currencyCode"] = currencyCode
        }
    }

    if let currencyCode = transactionDict["currencyCode"] as? String {
        transactionDict["currencySymbol"] = getCurrencySymbol(currencyCode: currencyCode)
    }

    if let deviceVerification = String(data: transaction.deviceVerification, encoding: .utf8) {
        transactionDict["deviceVerification"] = deviceVerification
    }

    return transactionDict
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(subscriptionPeriod: Product.SubscriptionPeriod) -> [String: Any] {
    let subscriptionPeriodDict: [String: Any] = [
        "unit": convert(unit: subscriptionPeriod.unit),
        "value": subscriptionPeriod.value,
    ]

    return subscriptionPeriodDict
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(offerType: Product.SubscriptionOffer.OfferType) -> Int32 {
    if offerType == .introductory {
        return 0
    }

    if offerType == .promotional {
        return 1
    }

    if #available(iOS 18.0, macOS 15.0, tvOS 18.0, watchOS 11.0, visionOS 2.0, *) {
        if offerType == .winBack {
            return 2
        }
    }

    return -1
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(paymentMode: Product.SubscriptionOffer.PaymentMode) -> Int32 {
    switch paymentMode {
    case .payAsYouGo:
        return 0
    case .payUpFront:
        return 1
    case .freeTrial:
        return 2
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(subscriptionOffer: Product.SubscriptionOffer) -> [String: Any] {
    var subscriptionOfferDict: [String: Any] = [
        "type": convert(offerType: subscriptionOffer.type),
        "price": subscriptionOffer.price,
        "displayPrice": subscriptionOffer.displayPrice,
        "period": convert(subscriptionPeriod: subscriptionOffer.period),
        "periodCount": subscriptionOffer.periodCount,
        "paymentMode": convert(paymentMode: subscriptionOffer.paymentMode),
    ]

    if let id = subscriptionOffer.id {
        subscriptionOfferDict["id"] = id
    }

    return subscriptionOfferDict
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(subscriptionInfo: Product.SubscriptionInfo) async -> [String: Any] {
    var subscriptionInfoDict: [String: Any] = [
        "promotionalOffers": subscriptionInfo.promotionalOffers.map { convert(subscriptionOffer: $0) },
        "subscriptionGroupID": subscriptionInfo.subscriptionGroupID,
        "subscriptionPeriod": convert(subscriptionPeriod: subscriptionInfo.subscriptionPeriod),
        "isEligibleForIntroOffer": await subscriptionInfo.isEligibleForIntroOffer,
    ]

    if let introductoryOffer = subscriptionInfo.introductoryOffer {
        subscriptionInfoDict["introductoryOffer"] = convert(subscriptionOffer: introductoryOffer)
    }

    if #available(iOS 18.0, macOS 15.0, tvOS 18.0, watchOS 11.0, visionOS 2.0, *) {
        subscriptionInfoDict["winBackOffers"] = subscriptionInfo.winBackOffers.map { winBackOffer in
            convert(subscriptionOffer: winBackOffer)
        }
    }

    return subscriptionInfoDict
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(productType: Product.ProductType) -> Int32 {
    switch productType {
    case .consumable:
        return 0
    case .nonConsumable:
        return 1
    case .nonRenewable:
        return 2
    case .autoRenewable:
        return 3
    default:
        return -1
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
private func convert(product: Product) async -> [String: Any] {
    var productDict: [String: Any] = [
        "id": product.id,
        "type": convert(productType: product.type),
        "displayName": product.displayName,
        "description": product.description,
        "price": product.price,
        "displayPrice": product.displayPrice,
        "isFamilyShareable": product.isFamilyShareable,
        "currencyCode": product.priceFormatStyle.currencyCode,
        "currencySymbol": getCurrencySymbol(currencyCode: product.priceFormatStyle.currencyCode)
    ]

    if let jsonRepresentation = String(data: product.jsonRepresentation, encoding: .utf8) {
        productDict["jsonRepresentation"] = jsonRepresentation
    }

    if let subscription = product.subscription {
        productDict["subscription"] = await convert(subscriptionInfo: subscription)
    }

    return productDict
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
@MainActor
private func purchase(_ productIdentifier: String, appAccountToken: UUID? = nil) async -> PaymentResult {
    if !AppStore.canMakePayments {
        return .code(.paymentNotAllowed)
    }

    do {
        let products = try await Product.products(for: Set.init([productIdentifier]))
        if (products.count <= 0) {
            return .code(.storeProductNotAvailable)
        }

        let product = products[0]
        var options: Set<Product.PurchaseOption> = [
            .quantity(1),
        ]
        if let appAccountToken = appAccountToken {
            options.insert(.appAccountToken(appAccountToken))
        }
        let result = try await product.purchase(options: options)

        switch result {
        case .success(let verificationResult):
            switch verificationResult {
            case .unverified(_, let error):
                return .error(error)
            case .verified(let transaction):
                return .transation(transaction)
            }
        case .userCancelled:
            return .code(.paymentCancelled)
        case .pending:
            return .code(.unknown)
        @unknown default:
            return .code(.unknown)
        }
    } catch {
        return .error(error)
    }
}

@_cdecl("enbug_iap_is_storekit2_supported")
func isStoreKit2Supported() -> Bool {
    if #available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *) {
        return true
    }

    return false
}

@_cdecl("enbug_iap_storekit2_set_transaction_updated_callback")
func setTransactionUpdatedCallback(callback: @Sendable @convention(c) (UnsafePointer<CChar>) -> Void) {
    transactionUpudated = callback
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
@_cdecl("enbug_iap_storekit2_start_loop")
func mainLoop() {
    if initialized { return }
    initialized = true
    Task.detached {
        for await verificationResult in Transaction.updates {
            switch verificationResult {
            case .verified(let transaction):
                let transactionDict = convert(transaction: transaction)

                do {
                    let jsonData = try JSONSerialization.data(withJSONObject: transactionDict)
                    let jsonStr = String(data: jsonData, encoding: .utf8)!
                    await MainActor.run {
                        jsonStr.withCString { cString in
                            transactionUpudated?(cString)
                        }
                    }
                } catch {
                    print("[Enbug][Billing] error:\(error)")
                }
            case .unverified(_, let error):
                print("\(error)")
            }
        }
    }
}

@_cdecl("enbug_iap_storekit2_request_products")
@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
func storeKit2RequestProducts(requestId: Int32, productIdentifiers: UnsafePointer<CChar>, callback: @Sendable @convention(c) (Int32, UnsafePointer<CChar>) -> Void) -> Int32 {
    do {
        let productIdentifierStr = String(cString: productIdentifiers)
        let productIdentifierData = productIdentifierStr.data(using: .utf8)!
        let productIdentifierArr = try JSONSerialization.jsonObject(with: productIdentifierData) as! [String]
        Task.detached {
            let products = try await Product.products(for: Set(productIdentifierArr))

            var productList: [[String: Any]] = []
            for product in products {
               productList.append(await convert(product: product))
            }

            do {
                let jsonData = try JSONSerialization.data(withJSONObject: productList)
                let jsonStr = String(data: jsonData, encoding: .utf8)!
                await MainActor.run {
                    jsonStr.withCString { cString in
                        callback(requestId, cString)
                    }
                }
            } catch {
               print("[Enbug][Billing] error:\(error)")
            }
        }
    } catch {
        print("[Enbug][Billing] error:\(error)")
        return 0
    }

    return 1
}

@_cdecl("enbug_iap_storekit2_purchase")
@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
func storeKit2Purchase(requestId: Int32, productIdentifier: UnsafePointer<CChar>, options: UnsafePointer<CChar>, callback: @Sendable @convention(c) (Int32, UnsafePointer<CChar>) -> Void) {
    let str = String(cString: productIdentifier)
    var appAccountToken: UUID? = nil
    if let data = String(cString: options).data(using: .utf8) {
        if let jsonDict = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            if let appAccountTokenStr = jsonDict["appAccountToken"] as? String {
                appAccountToken = UUID(uuidString: appAccountTokenStr)
            }
        }
    }

    Task.detached {
        let result = await purchase(str, appAccountToken: appAccountToken)
        var dict: [String: Any] = [:]
        switch result {
        case .transation(let transaction):
            dict["transaction"] = convert(transaction: transaction)
        case .error(let error):
            dict["error"] = [
                "code": error._code,
                "localizedDescription": error.localizedDescription
            ]
        case .code(let code):
            dict["code"] = code.rawValue
        }

        do {
            let jsonData = try JSONSerialization.data(withJSONObject: dict)
            let jsonStr = String(data: jsonData, encoding: .utf8)!
            await MainActor.run {
                jsonStr.withCString { cString in
                    callback(requestId, cString)
                }
            }
        } catch {
            print("[Enbug][Billing] error:\(error)")
        }
    }
}

@_cdecl("enbug_iap_storekit2_finish_transaction")
@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
func storeKit2FinishTransaction(requestId: Int32, identifier: UInt64, callback: @Sendable @convention(c) (Int32, Int32) -> Void) -> Int32 {
    Task.detached {
        for await verificationResult in Transaction.unfinished {
            switch verificationResult {
            case .verified(let transation):
                if transation.id == identifier {
                    await transation.finish()
                    await MainActor.run {
                        callback(requestId, 1)
                    }
                    return
                }
            default:
                break
            }
        }
        
        await MainActor.run {
            callback(requestId, 0)
        }
    }

    return 1
}

@_cdecl("enbug_iap_storekit2_get_transactions")
@available(iOS 15.0, macOS 12.0, tvOS 15.0, watchOS 8.0, visionOS 1.0, *)
func storeKit2GetTransactions(requestId: Int32, callback: @Sendable @convention(c) (Int32, UnsafePointer<CChar>?) -> Void) {
    Task.detached {
        var transactions: [[String: Any]] = []
        for await verificationResult in Transaction.unfinished {
            switch verificationResult {
            case .verified(let transation):
                transactions.append(convert(transaction: transation))
            default:
                break
            }
        }

        do {
            let jsonData = try JSONSerialization.data(withJSONObject: transactions)
            let jsonStr = String(data: jsonData, encoding: .utf8)!
            await MainActor.run {
                jsonStr.withCString { cString in
                    callback(requestId, cString)
                }
            }
        } catch {
            print("[Enbug][Billing] error:\(error)")
        }
    }
}
