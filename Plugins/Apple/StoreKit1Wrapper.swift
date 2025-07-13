import Foundation
import StoreKit

@available(iOS, introduced: 3.0, deprecated: 15.0, message: "Use StoreKit 2")
private enum PaymentResult {
    case code(SKError.Code)
    case transations([SKPaymentTransaction])
}

@available(iOS, introduced: 3.0, deprecated: 15.0, message: "Use StoreKit 2")
private class ProductRequestCallback: NSObject, SKProductsRequestDelegate {
    private let callback: (SKProductsResponse) -> Void

    init(_ completionHandler:@escaping (SKProductsResponse) -> Void) {
        self.callback = completionHandler
        super.init()
    }

    func productsRequest(_ request: SKProductsRequest, didReceive response: SKProductsResponse) {
        self.callback(response)
    }
}

@available(iOS, introduced: 3.0, deprecated: 15.0, message: "Use StoreKit 2")
private class IAPManager: NSObject, SKPaymentTransactionObserver, @unchecked Sendable {
    private let completionHandler: ([SKPaymentTransaction]) -> Void

    init(_ completionHandler: @escaping ([SKPaymentTransaction]) -> Void) {
        self.completionHandler = completionHandler
        super.init()
        SKPaymentQueue.default().add(self)
    }

    deinit {
        SKPaymentQueue.default().remove(self)
    }

    func paymentQueue(_ queue: SKPaymentQueue, updatedTransactions transactions: [SKPaymentTransaction]) {
        completionHandler(transactions)
    }
}

nonisolated(unsafe) private var maxRequestProductsId: Int = 0
nonisolated(unsafe) private var callbacks: [Int: ProductRequestCallback] = [:]
nonisolated(unsafe) private var manager: IAPManager? = nil

private func convert(period: SKProductSubscriptionPeriod) -> [String: Any] {
    return [
        "numberOfUnits": period.numberOfUnits,
        "unit": period.unit.rawValue,
    ]
}

private func convert(price: NSDecimalNumber, locale: Locale) -> String {
    let formatter = NumberFormatter()
    formatter.numberStyle = .currency
    formatter.locale = locale
    formatter.formatterBehavior = .behavior10_4
    return formatter.string(from: price) ?? ""
}

private func convert(transaction: SKPaymentTransaction) -> [String: Any] {
    let payment = transaction.payment
    var paymentDict: [String: Any] = [
        "productIdentifier": payment.productIdentifier,
        "quantity": payment.quantity,
        "simulatesAskToBuyInSandbox": payment.simulatesAskToBuyInSandbox,
    ]

    if let applicationUsername = payment.applicationUsername {
        paymentDict["applicationUsername"] = applicationUsername
    }

    var transactionDict: [String: Any] = [
        "transactionState": transaction.transactionState.rawValue,
        "payment": paymentDict,
    ]

    if let transactionIdentifier = transaction.transactionIdentifier {
        transactionDict["transactionIdentifier"] = transactionIdentifier
    }

    if let error = transaction.error {
        transactionDict["error"] = [
            "code": error._code,
            "localizedDescription": error.localizedDescription,
        ]
    }

    return transactionDict
}

private func convert(discount: SKProductDiscount) -> [String: Any] {
    var discountDict: [String: Any] = [
        "price": discount.price,
        "displayPrice": convert(price: discount.price, locale: discount.priceLocale),
        "subscriptionPeriod": convert(period: discount.subscriptionPeriod),
        "numberOfPeriods": discount.numberOfPeriods,
        "paymentMode": discount.paymentMode.rawValue,
    ]

    if let currencyCode = discount.priceLocale.currencyCode {
        discountDict["currencyCode"] = currencyCode
    }

    if let currencySymbol = discount.priceLocale.currencySymbol {
        discountDict["currencySymbol"] = currencySymbol
    }

    if #available(iOS 12.2, *) {
        if let identifier = discount.identifier {
            discountDict["identifier"] = identifier
        }

        discountDict["type"] = discount.type.rawValue
    }

    return discountDict
}

private func convert(product: SKProduct) -> [String: Any] {
    var productDict: [String: Any] = [
        "localizedDescription": product.localizedDescription,
        "localizedTitle": product.localizedTitle,
        "price": product.price,
        "displayPrice": convert(price: product.price, locale: product.priceLocale),
        "productIdentifier": product.productIdentifier,
        "contentVersion": product.contentVersion,
    ]

    if let currencyCode = product.priceLocale.currencyCode {
        productDict["currencyCode"] = currencyCode
    }

    if let currencySymbol = product.priceLocale.currencySymbol {
        productDict["currencySymbol"] = currencySymbol
    }

    if #available(iOS 12.2, *) {
        productDict["discount"] = product.discounts.map { convert(discount: $0) }
    }

    if #available(iOS 14.0, *) {
        productDict["isFamilyShareable"] = product.isFamilyShareable
    }

    if let subscriptionPeriod = product.subscriptionPeriod {
        productDict["subscriptionPeriod"] = convert(period: subscriptionPeriod)
    }

    if let introductoryPrice = product.introductoryPrice {
        productDict["introductoryPrice"] = convert(discount: introductoryPrice)
    }

    if let subscriptionGroupIdentifier = product.subscriptionGroupIdentifier {
        productDict["subscriptionGroupIdentifier"] = subscriptionGroupIdentifier
    }

    return productDict
}

private func request(products: Set<String>, completionHandler: @escaping (SKProductsResponse) -> Void) {
    maxRequestProductsId += 1
    let id = maxRequestProductsId
    let delegate = ProductRequestCallback { response in
        callbacks.removeValue(forKey: id)
        completionHandler(response)
    }
    callbacks[id] = delegate

    let request = SKProductsRequest(productIdentifiers: products)
    request.delegate = delegate
    request.start()
}

private func addPayment(_ productIdentifier: String, applicationUsername: UUID? = nil, completionHandler: @escaping (SKError.Code?) -> Void) {
    if !SKPaymentQueue.canMakePayments() {
        completionHandler(.paymentNotAllowed)
        return
    }

    request(products: [productIdentifier]) { response in
        let products = response.products
        if (products.isEmpty) {
            completionHandler(.storeProductNotAvailable)
            return
        }

        let product = products[0]
        let payment = SKMutablePayment(product: product)
        payment.quantity = 1
        if let applicationUsername = applicationUsername {
            payment.applicationUsername = applicationUsername.uuidString
        }
        completionHandler(nil)
        SKPaymentQueue.default().add(payment)
    }
}

@_cdecl("enbug_iap_storekit1_start_listener")
func storeKit1StartListener(callback: @Sendable @convention(c) (UnsafePointer<CChar>) -> Void) {
    DispatchQueue.main.async {
        if manager != nil {
            return
        }

        manager = IAPManager { transations in
            let transationList = transations.map { convert(transaction: $0) }

            do {
                let jsonData = try JSONSerialization.data(withJSONObject: transationList)
                let jsonStr = String(data: jsonData, encoding: .utf8)!
                jsonStr.withCString { cString in
                    callback(cString)
                }
            } catch {
                print("[Enbug][Billing] error:\(error)")
            }
        }
    }
}

@_cdecl("enbug_iap_storekit1_request_products")
func storeKit1RequestProducts(requestId: Int, productIdentifiers: UnsafePointer<CChar>, callback: @Sendable @convention(c) (Int, UnsafePointer<CChar>?) -> Void) -> Int {
    do {
        let productIdentifierStr = String(cString: productIdentifiers)
        let productIdentifierData = productIdentifierStr.data(using: .utf8)!
        let productIdentifierArr = try JSONSerialization.jsonObject(with: productIdentifierData) as! [String]
        DispatchQueue.main.async {
            request(products: Set(productIdentifierArr)) { response in
                let productList: [[String: Any]] = response.products.map { convert(product: $0) }

                let dict: [String: Any] = [
                   "products": productList,
                   "invalidProductIdentifiers": response.invalidProductIdentifiers,
                ]

                do {
                    let jsonData = try JSONSerialization.data(withJSONObject: dict)
                    let jsonStr = String(data: jsonData, encoding: .utf8)!
                    jsonStr.withCString { cString in
                        callback(requestId, cString)
                    }
                } catch {
                   print("[Enbug][Billing] error:\(error)")
                }
            }

        }
    } catch {
        print("[Enbug][Billing] error:\(error)")
        return 0
    }

    return 1
}

@_cdecl("enbug_iap_storekit1_add_payment")
func storeKit1AddPayment(requestId: Int, productIdentifier: UnsafePointer<CChar>, options: UnsafePointer<CChar>, callback: @Sendable @convention(c) (Int, Int, Int) -> Void) {
    let str = String(cString: productIdentifier)

    var applicationUsername: UUID? = nil
    if let data = String(cString: options).data(using: .utf8) {
        if let jsonDict = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            if let applicationUsernameStr = jsonDict["applicationUsername"] as? String {
                applicationUsername = UUID(uuidString: applicationUsernameStr)
            }
        }
    }

    DispatchQueue.main.async {
        addPayment(str, applicationUsername: applicationUsername) { code in
            if let code = code {
                callback(requestId, 0, code.rawValue)
            } else {
                callback(requestId, 1, 0)
            }
        }
    }
}

@_cdecl("enbug_iap_storekit1_finish_transaction")
func storeKit1FinishTransaction(identifier: UnsafePointer<CChar>) -> Int {
    let identifierStr = String(cString: identifier)

    let transactions = SKPaymentQueue.default().transactions
    for transaction in transactions {
        if transaction.transactionIdentifier == identifierStr {
            SKPaymentQueue.default().finishTransaction(transaction)
            return 1
        }
    }

    return 0
}

@_cdecl("enbug_iap_storekit1_get_receipt")
func storeKit1GetReceipt(callback: @Sendable @convention(c) (UnsafePointer<CChar>?) -> Void) {
    let receiptURL = Bundle.main.appStoreReceiptURL
    var receiptStr: String? = nil
    if let receiptURL = receiptURL {
        let receiptData = try? Data(contentsOf: receiptURL)
        receiptStr = receiptData?.base64EncodedString()
    }

    if let receiptStr = receiptStr {
        receiptStr.withCString { cString in
            callback(cString)
        }
    } else {
        callback(nil)
    }
}

@_cdecl("enbug_iap_storekit1_get_transactions")
func storeKit1GetTransactions(callback: @Sendable @convention(c) (UnsafePointer<CChar>?) -> Void) {
    let transactions = SKPaymentQueue.default().transactions
    let transactionList: [[String: Any]] = transactions.map { convert(transaction: $0) }

    do {
        let jsonData = try JSONSerialization.data(withJSONObject: transactionList)
        let jsonStr = String(data: jsonData, encoding: .utf8)!
        jsonStr.withCString { cString in
            callback(cString)
        }
    } catch {
        print("[Enbug][Billing] error:\(error)")
    }
}
