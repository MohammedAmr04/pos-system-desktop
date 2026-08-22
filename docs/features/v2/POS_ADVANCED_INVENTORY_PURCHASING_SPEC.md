# POS — Advanced Product, Purchasing, Inventory, Returns & Financial Rules

> **Status:** Business Requirements / Implementation Specification  
> **Purpose:** This document consolidates the business decisions discussed for the next POS features so implementation can proceed without changing the core rules later.

---

## 1. Scope

This document defines the agreed business rules for:

1. Categories
2. Brands
3. Units & Product Units
4. Suppliers
5. Clients / Customers
6. Purchase Invoices
7. Sales Invoices lifecycle
8. FIFO cost layers
9. Payments & account balances
10. Sales Returns
11. Purchase Returns
12. Expenses
13. Reports foundations
14. Printer settings foundations

**Bundle / Composite Products are intentionally out of scope for this phase.**

---

# 2. Core Design Principles

## 2.1 Master Data vs Transactions

The system should clearly separate:

### Master Data

- Product
- Category
- Brand
- Unit
- Supplier
- Client

### Transactions

- Sales Invoice
- Purchase Invoice
- Sales Return
- Purchase Return
- Expense
- Payment

### Inventory / Costing

- Stock Movement
- FIFO Cost Layer

### Configuration

- Printer Settings
- Tenant Features
- Permissions

---

## 2.2 Inactive Does Not Mean Delete

`Inactive` must never mean that existing relationships are removed.

If a Product references a Category and that Category becomes inactive:

```text
Product.CategoryId = existing Category Id
Category.IsActive = false
```

The foreign-key value must remain unchanged.

The UI should treat the inactive reference as `Other`.

If the Category is reactivated later, the original Products automatically become associated with it again.

### Important Rule

```text
Deactivate != Delete != Detach
```

The same principle applies to Brands and Units.

---

## 2.3 NULL vs "Other"

The database should use `NULL` when the Product genuinely has no Category or Brand.

Do **not** create fake `Other` records just to avoid NULLs.

### Example

```text
Product.CategoryId = NULL
Product.BrandId = NULL
```

The UI displays:

```text
Category: Other
Brand: Other
```

If a Category/Brand is inactive but still referenced:

```text
Product.CategoryId = 5
Category.IsActive = false
```

The UI still displays:

```text
Category: Other
```

The relationship itself is not removed.

---

# 3. Categories

## 3.1 Purpose

Categories provide a simple classification of Products.

Examples:

- Beverages
- Snacks
- Cleaning
- Electronics
- Grocery

---

## 3.2 Structure

Categories are intentionally **single-level**.

There is no Parent Category.

### Category fields

```text
Id
Name
Description
Image
IsActive
CreatedAt
UpdatedAt
```

---

## 3.3 Business Rules

1. A Category does not have a Parent Category.
2. Category hierarchy is not supported.
3. A Product can belong to only one Category.
4. A Product may have no Category.
5. If no Category exists, the UI displays `Other`.
6. Category cannot be deleted.
7. Category can be renamed.
8. Category can be activated/deactivated.
9. Deactivating a Category does not clear `Product.CategoryId`.
10. An inactive Category must not be selectable for new assignments.
11. Existing Products referencing an inactive Category remain linked in the database.
12. The UI treats an inactive Category as `Other`.
13. Reactivating the Category restores the original relationship automatically.
14. Category grouping is not required in the POS screen.
15. POS product discovery is based primarily on server-side search.
16. Category-based reporting will be handled later as part of Reports.
17. Categories may be controlled by a Tenant Feature.
18. If the Category feature is disabled for a Tenant, the UI should not require Categories and unassigned Products appear as `Other`.

---

## 3.4 Category Feature Disablement

Disabling the Tenant Feature is different from deactivating a Category.

### Tenant Feature disabled

Do not mutate existing Product.CategoryId values.

The feature is simply unavailable in the UI/business flow.

### Category itself inactive

Do not mutate Product.CategoryId values.

The UI resolves the inactive category to `Other`.

This preserves historical relationships and allows future reactivation.

---

# 4. Brands

## 4.1 Purpose

A Brand identifies the manufacturer/brand of a Product.

Example:

```text
Product: Pepsi 330ml
Category: Soft Drinks
Brand: Pepsi
```

Category and Brand are different concepts.

### Category

Classifies what the product is.

### Brand

Identifies the company/brand associated with the product.

---

## 4.2 Structure

```text
Brand
-----
Id
Name
IsActive
CreatedAt
UpdatedAt
```

No Brand Logo is required in this phase.

---

## 4.3 Business Rules

1. A Brand can exist without Products.
2. A Product may have no Brand.
3. `Product.BrandId` is nullable.
4. If Brand is missing, the UI displays `Other`.
5. Brand does not need to appear in POS product search.
6. Brand can be renamed.
7. Brand can be deleted only when no Product references it.
8. A Brand that is referenced by Products cannot be deleted.
9. A referenced Brand can be deactivated.
10. Deactivation does not clear `Product.BrandId`.
11. The UI treats an inactive Brand as `Other`.
12. Reactivation restores the original Brand relationship.
13. Brand-based reporting will be handled later.
14. Brand may be controlled by a Tenant Feature.

---

# 5. Units

## 5.1 Goal

Units must support both normal packaging and measurable/weighted products.

The system must not be limited to:

```text
Piece
Pack
Carton
```

It should also support units such as:

```text
Gram
Kilogram
Milliliter
Liter
Meter
Roll
Dozen
Box
Bottle
Case
```

The exact unit list is configurable.

---

## 5.2 Unit Master Data

Units should be managed from a dedicated Units page.

### Unit structure

```text
Unit
----
Id
Name
IsActive
CreatedAt
UpdatedAt
```

A Unit does not contain a product-specific conversion factor.

The conversion factor belongs to the relationship between a Product and a Unit.

---

# 6. Product Units

The existing Product Unit architecture remains the foundation.

The current POS already supports:

- One Base Unit per Product
- Quantity Factors
- Decimal Quantity Factors
- Unit-specific Retail Price
- Optional Wholesale Price
- Unit-scoped Barcodes

This is consistent with the existing Product Units design. The current documentation defines one Base Unit as the stock owner and stores inventory in the Base Unit. fileciteturn1file0

---

## 6.1 ProductUnit

Recommended structure:

```text
ProductUnit
-----------
Id
ProductId
UnitId
QuantityFactor
IsBaseUnit
CreatedAt
UpdatedAt
```

Selling prices may exist on the ProductUnit according to the current pricing architecture:

```text
RetailPrice
WholesalePrice
```

The existing v1.4 design already uses unit-specific Retail and Wholesale prices and allows decimal quantity factors. fileciteturn1file2turn1file6

---

## 6.2 Base Unit

Every Product must have exactly one Base Unit.

Rules:

1. Base Unit represents the stock owner.
2. Base Unit has QuantityFactor = `1`.
3. Inventory is stored using the Base Unit.
4. Base Unit cannot be deleted.
5. Base Unit should not be changed after the Product has transactional history/stock.
6. Other selling units convert into the Base Unit.

Example:

```text
Product: Cable

Base Unit:
Meter
Factor = 1

Selling Unit:
Roll
Factor = 100
```

---

## 6.3 Packaging Example

```text
Product: Pepsi

Piece
Factor = 1

Pack
Factor = 6

Carton
Factor = 24
```

If the cashier sells:

```text
2 Cartons
```

Inventory movement is:

```text
2 × 24 = 48 Base Units
```

---

# 7. Weighted / Decimal Quantities

Decimal quantities are mandatory.

This is important for supermarkets and products sold by weight, length, volume, etc.

Examples:

```text
1.250 KG
0.500 KG
2.750 KG
```

Decimal quantities must be supported consistently in:

- Product Units
- Sales
- Purchases
- Sales Returns
- Purchase Returns
- Stock Adjustments
- Inventory calculations
- Reports

Do not restrict quantity to integers.

---

# 8. Unit Pricing During Purchase

When entering a Purchase Invoice, the user must be able to define the selling prices for the units being purchased.

Example:

```text
Product: Pepsi

Purchase Unit: Carton

Quantity:
10 Cartons

Purchase Cost:
200 EGP / Carton

Selling Prices:

Carton
Retail = 250
Wholesale = 230

Pack
Retail = 65
Wholesale = 60

Piece
Retail = 12
Wholesale = 11
```

The exact UI can decide which Product Units are editable, but the business requirement is:

> The Purchase workflow must allow the user to define/update the selling price for the relevant units while receiving the stock.

---

## 8.1 Auto Pricing

Automatic selling-price calculation is **out of scope for the current phase**.

Do not implement:

```text
Cost + Margin = Retail Price
```

or configurable automatic margins yet.

Future versions may introduce:

- Retail margin
- Wholesale margin
- Automatic price calculation
- Price rules

For now, prices are entered/updated explicitly.

---

# 9. Suppliers

A Purchase Invoice may or may not have a Supplier.

This is intentional.

A business may purchase goods:

- From a registered Supplier
- From an individual/vendor
- From a shop in the street
- Through an immediate cash purchase without maintaining a Supplier account

---

## 9.1 Supplier Structure

Recommended:

```text
Supplier
--------
Id
Name
Phone
Address
Notes
IsActive
CreatedAt
UpdatedAt
```

---

## 9.2 Supplier Rules

1. Supplier is optional for a cash Purchase Invoice.
2. Supplier is required for a credit Purchase Invoice.
3. Supplier can have multiple Purchase Invoices.
4. Supplier can have multiple Payment transactions.
5. Supplier balance is derived from Purchases and Payments.
6. Supplier returns affect the Supplier account.
7. Supplier cannot be deleted if transactional history requires preserving its identity; prefer inactive/archival behavior once referenced.

---

# 10. Clients / Customers

Clients are needed to support credit sales and customer balances.

A normal cash sale does not require a Client.

---

## 10.1 Client Structure

Recommended:

```text
Client
------
Id
Name
Phone
Address
Notes
IsActive
CreatedAt
UpdatedAt
```

---

## 10.2 Client Rules

### Cash Sale

```text
Customer = NULL
Payment = Cash
```

Allowed.

### Credit Sale

```text
Customer = Required
```

A credit sale without a Client is not allowed.

This allows the system to answer:

> Who owes us money?

---

# 11. Purchase Invoices

Purchase Invoices represent stock entering the business.

---

## 11.1 Purchase Invoice Fields

Recommended:

```text
PurchaseInvoice
---------------
Id
InvoiceNumber
SupplierId nullable
Date
Status
Subtotal
Discount
Tax
Total
CreatedBy
CreatedAt
UpdatedAt
```

Supplier is nullable because cash purchases can be made without a registered Supplier.

---

## 11.2 Purchase Invoice Number

A Purchase Invoice must have an invoice number.

This can be:

- Supplier's invoice number
- Or the POS's internal transaction number, depending on the final UX/design

The exact uniqueness policy should be finalized during implementation, especially if supplier invoice numbers are allowed to repeat across different Suppliers.

---

# 12. Purchase Invoice Status

Purchase Invoices support:

```text
Draft
Posted
Cancelled
```

---

## 12.1 Draft

A Draft Purchase Invoice:

- Can be edited.
- Does not affect stock.
- Does not create FIFO cost layers.
- Does not affect Supplier balance.
- Does not create accounting/payment effects.

---

## 12.2 Posted

A Posted Purchase Invoice:

- Adds stock.
- Creates FIFO Cost Layers.
- Updates relevant Product Unit selling prices if the user entered new prices.
- Creates Supplier balance impact when applicable.
- Can have Payment transactions.

---

## 12.3 Cancelled

Cancellation must reverse the effects of a Posted transaction safely.

Do not simply delete transactional records.

The exact reversal implementation must preserve auditability and FIFO integrity.

---

# 13. Editing Purchase Invoices

A Purchase Invoice may be edited by a user with the appropriate permission.

However, editing a Posted Purchase Invoice is not a simple database update because it may already have affected:

- Stock
- FIFO Cost Layers
- Supplier Balance
- Product Unit Prices

Therefore:

> Posted Purchase Invoice editing must be implemented as a transactional recalculation/reversal/re-application operation.

Do not directly update invoice lines without recalculating dependent inventory/cost state.

---

# 14. Purchase Cost

Purchase cost is historical transaction data.

Example:

```text
Purchase #100
100 Pieces
Cost = 8
```

Later:

```text
Purchase #120
200 Pieces
Cost = 10
```

The system must preserve both costs.

Do not overwrite the old cost on the Product.

---

# 15. FIFO Cost Layers

The project will use **FIFO (First In, First Out)** for inventory costing.

FIFO is required because Purchase Costs can change over time and Profit Reports must use the correct historical cost.

---

## 15.1 Cost Layer

A Cost Layer represents a quantity received at a specific cost.

Conceptually:

```text
CostLayer
---------
Id
ProductId
SourcePurchaseId
QuantityReceived
QuantityRemaining
UnitCost
CreatedAt
```

The exact schema may include additional audit/reference fields.

---

## 15.2 Example

Purchase #100:

```text
100 Pieces
Cost = 8
```

Creates:

```text
Layer 1
100 × 8
Remaining = 100
```

Purchase #120:

```text
200 Pieces
Cost = 10
```

Creates:

```text
Layer 2
200 × 10
Remaining = 200
```

---

## 15.3 Sale Under FIFO

If the business sells:

```text
150 Pieces
```

FIFO consumes:

```text
100 × 8
50 × 10
```

COGS:

```text
800 + 500 = 1300
```

Remaining:

```text
Layer 1 = 0
Layer 2 = 150
```

---

## 15.4 FIFO Scope

FIFO is applied at the **Product level**.

Selling Units are converted into the Product's Base Unit before FIFO consumption.

Example:

```text
Carton = 24 Pieces
Sale = 2 Cartons
```

Convert:

```text
2 × 24 = 48 Pieces
```

Then consume:

```text
48 Pieces
```

from the Product's FIFO layers.

---

# 16. Sales Invoice Historical Cost

Every posted Sales Invoice line must preserve the historical financial values used at the time of the sale.

The system must not rely on the Product's current cost when calculating historical reports.

A Sale should preserve at least:

```text
ProductId
ProductUnitId
Quantity
UnitPrice
OriginalUnitPrice
Discount
FinalLineTotal
TotalCost
```

Additional FIFO allocation records may be stored separately.

---

## 16.1 Why TotalCost Is Important

A single Sales Invoice line can consume multiple FIFO layers.

Example:

```text
Sale:
150 Pieces

FIFO:
100 × 8
50 × 10
```

The line's total cost is:

```text
1300
```

Therefore the Sales Invoice should preserve:

```text
TotalCost = 1300
```

while FIFO allocation details remain available for auditing.

---

# 17. Historical Sales Data

Once a Sale is Posted, its historical values must not change because the Product's current prices/costs change later.

Example:

```text
Aug 20

Cost = 8
Sale = 10
Profit = 2
```

Later:

```text
Aug 25

New Purchase Cost = 10
```

The Aug 20 sale must still report:

```text
Cost = 8
Sale = 10
Profit = 2
```

Do not recalculate old invoice costs from the current Product cost.

---

# 18. Selling Price Updates

When a new Purchase Invoice changes the purchase cost, the business wants the Retail/Wholesale selling prices to be updated during the purchase process.

The user can explicitly enter the new selling prices.

Example:

```text
Old Cost = 8

New Purchase Cost = 9

User enters:

Retail = 12
Wholesale = 11
```

The system stores the new current selling prices on the appropriate Product Units.

Automatic margin calculation is deferred.

---

# 19. Payments

Payments must be independent transactions.

Do not use a single mutable `PaidAmount` field as the source of truth.

---

## 19.1 Payment Concept

Recommended structure:

```text
Payment
-------
Id
Amount
PaymentMethod
Date
InvoiceId nullable
CustomerId nullable
SupplierId nullable
Reference
Notes
CreatedBy
CreatedAt
```

The exact relationships can be adjusted during database design.

---

## 19.2 Payment Examples

Purchase:

```text
Total = 10,000

Payment #1 = 3,000
Payment #2 = 2,000

Remaining = 5,000
```

Sales credit:

```text
Invoice = 5,000

Payment #1 = 2,000

Customer Balance = 3,000
```

---

# 20. Payment Status

Required statuses:

```text
Paid
Partially Paid
Unpaid
```

Prefer calculating the status from:

```text
Total
PaidAmount
RemainingAmount
```

rather than treating a mutable status field as the primary truth.

Conceptually:

```text
PaidAmount = 0
→ Unpaid

0 < PaidAmount < Total
→ Partially Paid

PaidAmount >= Total
→ Paid
```

Payment transactions remain the source of truth.

---

# 21. Cash vs Credit

The system must distinguish immediate cash transactions from credit transactions.

---

## Purchase

### Cash Purchase

```text
SupplierId = NULL or provided
Payment = Cash
Paid = Total
```

Supplier is optional.

### Credit Purchase

```text
SupplierId = Required
Payment may be partial/unpaid
```

The Supplier balance tracks what the business owes.

---

## Sale

### Cash Sale

```text
ClientId = NULL or provided
Payment = Cash
```

Client is optional.

### Credit Sale

```text
ClientId = Required
```

This is necessary to track the amount owed by the Client.

---

# 22. Sales Returns

Sales Returns must be based on an existing Sales Invoice.

A return without an original invoice is not supported in the normal flow.

---

## 22.1 Return Types

Support:

```text
Full Return
Partial Return
```

---

## 22.2 Full Return

If the customer returns the entire invoice:

```text
Original Invoice
Status = Fully Returned
```

The original invoice must remain in the system.

Do not rename/convert the original invoice into a Return Invoice.

Instead create a separate transaction:

```text
Sales Return
OriginalInvoiceId = X
```

This preserves audit history.

---

## 22.3 Partial Return

Example:

```text
Invoice #100

Pepsi × 2
Chipsy × 3
Water × 5
```

Customer returns:

```text
Pepsi × 1
Water × 2
```

The system creates a Sales Return referencing Invoice #100.

The original invoice becomes:

```text
Partially Returned
```

If all remaining quantities are later returned:

```text
Fully Returned
```

---

# 23. Sales Return Rules

1. Return requires an original Sales Invoice.
2. Return can reference an old invoice; there is no fixed return-period limitation.
3. Return can be partial.
4. Return can be full.
5. Returned quantity cannot exceed the quantity sold minus quantities already returned.
6. Returned stock is restored using the Product's Base Unit conversion.
7. Refund uses the same payment method as the original payment.
8. Cash sale → Cash refund.
9. Card sale → Card refund.
10. A return does not destroy the original Sales Invoice.
11. Return must have its own transaction identity/number.
12. Cashier can perform Returns only when the required permission is granted.

---

# 24. Sales Return + FIFO

The implementation must preserve FIFO correctness.

When stock is returned from a sale, the system must define which cost is restored.

The preferred design is to preserve the original FIFO cost allocation associated with the sold quantity so that a return can restore the appropriate cost layer/history rather than blindly using the current cost.

This must be implemented transactionally.

---

# 25. Purchase Returns

Purchase Returns should also be supported.

A Purchase Return reverses stock received from a Purchase Invoice.

Example:

```text
Purchase:
100 Pieces @ 8
```

Return:

```text
10 Pieces
```

The system must:

- Reduce available stock.
- Reverse the appropriate inventory/cost effect.
- Affect Supplier balance when a Supplier exists.
- Preserve the reference to the original Purchase Invoice.

---

# 26. Expenses

Expenses are separate from Purchases.

### Purchase

Means the business acquired inventory.

### Expense

Means money was spent on operating/business costs.

Examples:

```text
Rent
Electricity
Water
Internet
Maintenance
Transportation
Salaries
Other
```

---

## 26.1 Expense Structure

Recommended:

```text
Expense
-------
Id
CategoryId
Amount
PaymentMethod
Date
Description
Reference
CreatedBy
CreatedAt
```

Expense Categories may be managed separately.

---

# 27. Cashier Expenses

A Cashier can create a cash Expense according to permissions.

The Expense must affect the Cash/Shift calculation.

Example:

```text
Opening Cash = 1000

Sales = +5000
Expense = -500
Returns = -300

Expected Cash = 5200
```

The exact Shift implementation should integrate Expenses with the existing Shift/Cash workflow.

---

# 28. Expense and Shift

A Cashier Expense should normally be linked to the active Shift/Cash session.

This allows End-of-Shift reporting to calculate:

```text
Opening Cash
+ Cash Sales
+ Other Cash In
- Cash Returns
- Cash Expenses
= Expected Cash
```

Administrative/general expenses can later be supported separately if needed.

---

# 29. Reports Foundations

Reports are intentionally designed after the transaction and costing model.

The system should not implement Profit Reports before Purchase + FIFO + Sales Cost snapshots are stable.

Potential report groups:

## Sales

- Sales by date
- Sales by Product
- Sales by Category
- Sales by Brand
- Sales by Unit
- Sales by Cashier
- Sales by Payment Method
- Retail vs Wholesale
- Discounts
- Price Overrides

## Purchases

- Purchases by date
- Purchases by Product
- Purchases by Supplier
- Purchase costs
- Purchase cost changes

## Inventory

- Current Stock
- Low Stock
- Stock Movement
- Stock Valuation
- Inventory Adjustments
- Stock In
- Stock Out

## Profit

```text
Revenue
- COGS
= Gross Profit
```

And:

```text
Gross Margin
= Gross Profit / Revenue
```

Potential dimensions:

- Product
- Category
- Brand
- Unit
- Cashier
- Date

## Returns

- Sales Returns
- Purchase Returns
- Returned Quantity
- Refund Amount

## Expenses

- Expenses by Date
- Expenses by Category
- Expenses by Payment Method

## Cash

- Cash In
- Cash Out
- Expenses
- Returns
- Expected Cash

---

# 30. Profit Calculation

For a posted sale:

```text
Revenue = Final Sales Amount
COGS = FIFO-derived historical TotalCost
Gross Profit = Revenue - COGS
```

Do not calculate historical COGS from the Product's current purchase cost.

The Sales Invoice snapshot + FIFO allocation are the historical basis.

---

# 31. Printer Settings

The current system has printer functionality, including receipt/barcode printing, and the current architecture already exposes printing endpoints. fileciteturn1file7turn1file17

The next settings feature should move printer configuration out of hardcoded values wherever practical.

---

## 31.1 Printer Configuration Goals

The system should eventually allow configuration of:

### Printer

- Receipt Printer
- Barcode Printer
- Report Printer
- Printer Name
- Paper Width
- Number of Copies
- Auto Cut
- Cash Drawer

### Receipt

- Store Name
- Address
- Phone
- Tax Number
- Logo
- Header
- Footer
- Thank-you Message
- Customer Information
- Cashier Information
- Date/Time

### Reports

- Header
- Logo
- Date
- Cashier
- Summary
- Details

The exact printer settings schema should be designed in the dedicated Printer Settings feature.

---

# 32. Sales Invoice Lifecycle

Sales Invoices should support:

```text
Draft
Posted
Cancelled
```

---

## Draft

A Draft:

- Can be edited.
- Does not deduct stock.
- Does not create revenue.
- Does not consume FIFO layers.
- Does not affect Client balance.
- Does not create final payment effects.

---

## Posted

A Posted Sale:

- Deducts stock.
- Consumes FIFO layers.
- Records historical selling price.
- Records historical total cost.
- Creates revenue.
- Creates Client balance if credit.
- Records payment transaction(s).

---

## Cancelled

Cancellation must reverse transaction effects safely.

Do not delete posted transactional history.

---

# 33. Permissions

Existing RBAC/permission architecture should control sensitive actions.

The current project already uses permissions for products, invoices, pricing, reports, printing, settings, etc. fileciteturn1file11

Sensitive operations should require dedicated permissions, especially:

- Edit Posted Purchase Invoice
- Sales Return
- Purchase Return
- Expense
- Payment
- Price Update
- Cancel Invoice
- Reports

The exact permission keys should be finalized during implementation.

---

# 34. Tenant Features

Categories and Brands may be controlled through Tenant Features.

The existing system already supports Tenant Feature switches and combines feature access with permissions. fileciteturn1file11

Important rule:

> Disabling a Tenant Feature must not destroy existing data relationships.

For example, disabling Categories must not set all `CategoryId` values to NULL.

The feature should simply be unavailable in the current UI/business flow.

---

# 35. Data Integrity Rules

The following rules are mandatory:

### Product

- One Product → one Category maximum.
- One Product → one Brand maximum.
- Category nullable.
- Brand nullable.
- Product has exactly one Base Unit.
- Product may have multiple selling units.

### Category

- No hierarchy.
- Cannot delete.
- Can rename.
- Can deactivate.
- Deactivation does not detach Products.

### Brand

- Can exist without Products.
- Can be deleted only when unused.
- Can rename.
- Can deactivate.
- Deactivation does not detach Products.

### Unit

- Can be managed independently.
- Used Units cannot be deleted.
- Used Units can be renamed/inactivated.
- Base Unit cannot be deleted.
- Base Unit should not be changed after transactional history exists.

### Purchase

- Invoice number required.
- Supplier optional for cash.
- Supplier required for credit.
- Draft does not affect stock.
- Posted affects stock/cost.
- FIFO layers must be preserved.

### Sales

- Customer optional for cash.
- Customer required for credit.
- Draft does not affect stock.
- Posted consumes FIFO.
- Historical cost and sale price are preserved.

### Returns

- Must reference original invoice.
- Full and partial returns supported.
- No fixed return-period restriction.
- Return quantity cannot exceed available returnable quantity.
- Refund uses original payment method.

---

# 36. Recommended Transactional Flow

## Purchase

```text
Create Draft Purchase
        ↓
Add Products / Units
        ↓
Enter Purchase Cost
        ↓
Enter Retail / Wholesale Prices
        ↓
Select Supplier (optional for Cash)
        ↓
Enter Payment
        ↓
Post
        ↓
Stock In
        ↓
Create FIFO Cost Layers
        ↓
Update Current Selling Prices
        ↓
Update Supplier Balance if applicable
```

---

## Sale

```text
Create Draft Sale
        ↓
Select Products / Units
        ↓
Determine Retail / Wholesale Price
        ↓
Apply allowed Overrides / Discounts
        ↓
Select Payment
        ↓
Customer required if Credit
        ↓
Post
        ↓
Convert Quantity → Base Unit
        ↓
Consume FIFO Layers
        ↓
Store Historical TotalCost
        ↓
Deduct Stock
        ↓
Create Payment / Customer Balance
```

---

## Sales Return

```text
Select Original Invoice
        ↓
Select Full or Partial Return
        ↓
Validate Returnable Quantity
        ↓
Calculate Refund
        ↓
Restore Stock
        ↓
Restore/Reverse appropriate FIFO cost allocation
        ↓
Create Refund Payment
        ↓
Update Invoice Return Status
```

---

# 37. Important Historical Data Rule

The system must preserve transaction-time values.

Never depend on current Master Data to reconstruct historical financial values.

Examples:

### Product Price

Old invoice keeps its old sale price.

### Product Cost

Old invoice keeps its historical cost.

### Category

Existing Product relationship remains even if Category becomes inactive.

### Brand

Existing Product relationship remains even if Brand becomes inactive.

### Unit

Historical invoice lines keep the Unit information used at transaction time.

This principle is critical for reliable Reports and auditing.

---

# 38. Out of Scope for This Phase

The following are intentionally postponed:

- Bundle / Composite Products
- Automatic price/margin calculation
- Advanced customer-specific pricing
- Supplier-specific pricing rules
- Complex accounting/GL
- Multi-warehouse inventory
- Nested bundles
- Manual returns without an original invoice

These may be introduced later without changing the core principles above.

---

# 39. Implementation Order

Recommended implementation sequence:

```text
1. Categories
2. Brands
3. Units
4. Suppliers
5. Clients
6. Product/ProductUnit adjustments
7. Stock Movement foundation
8. Purchase Invoices
9. FIFO Cost Layers
10. Payment Transactions
11. Sales Invoice lifecycle
12. Sales Returns
13. Purchase Returns
14. Expenses
15. Reports
16. Printer Settings
```

Do not implement Reports before the underlying transaction and costing rules are stable.

---

# 40. Acceptance Criteria

The implementation is considered correct when:

## Categories

- [ ] Single-level Categories work.
- [ ] Product has max one Category.
- [ ] Category can be inactive.
- [ ] Inactive Category remains referenced by Products.
- [ ] UI displays inactive/missing Category as Other.
- [ ] Reactivating Category restores its display automatically.
- [ ] Category cannot be deleted.

## Brands

- [ ] Product may have no Brand.
- [ ] Brand can exist without Products.
- [ ] Unused Brand can be deleted.
- [ ] Used Brand cannot be deleted.
- [ ] Brand can be renamed.
- [ ] Brand can be inactive.
- [ ] Inactive Brand remains referenced by Products.
- [ ] UI displays inactive Brand as Other.

## Units

- [ ] Units have a dedicated management page.
- [ ] Product can have multiple Units.
- [ ] Exactly one Base Unit exists per Product.
- [ ] Quantity Factor supports decimals.
- [ ] Weighted products are supported.
- [ ] Inventory is stored in Base Units.
- [ ] Base Unit cannot be deleted.
- [ ] Used Units cannot be deleted.
- [ ] Unit can be inactive/renamed without breaking historical references.

## Purchasing

- [ ] Purchase Invoice Number exists.
- [ ] Purchase can be Draft.
- [ ] Purchase can be Posted.
- [ ] Purchase can be Cancelled.
- [ ] Supplier is optional for Cash purchases.
- [ ] Supplier is required for Credit purchases.
- [ ] Purchase Cost is historical.
- [ ] Retail/Wholesale prices can be entered during purchase.
- [ ] Auto pricing is not required.

## FIFO

- [ ] Purchase creates Cost Layers.
- [ ] Sales consume Cost Layers FIFO.
- [ ] Units are converted to Base Units before FIFO.
- [ ] Multiple FIFO layers can contribute to one Sales line.
- [ ] Historical TotalCost is stored on the Sales transaction.
- [ ] Old sales do not change when current Product cost changes.

## Payments

- [ ] Payments are independent transactions.
- [ ] Multiple payments per invoice are supported.
- [ ] Paid / Partially Paid / Unpaid are correctly derived.
- [ ] Supplier balances can be tracked.
- [ ] Client balances can be tracked.
- [ ] Credit transactions require the appropriate Party.

## Sales Returns

- [ ] Return requires original invoice.
- [ ] Full Return supported.
- [ ] Partial Return supported.
- [ ] Old invoices can be returned.
- [ ] No fixed return-period restriction.
- [ ] Return quantity cannot exceed sold quantity minus previous returns.
- [ ] Refund uses original payment method.
- [ ] Original invoice remains intact.
- [ ] Return is a separate transaction.

## Expenses

- [ ] Cashier can create Expense with permission.
- [ ] Expense affects cash/shift.
- [ ] Expense is separate from Purchase.
- [ ] Expense categories are supported.

## Reports

- [ ] Sales reports can aggregate historical revenue.
- [ ] COGS uses stored/FIFO historical cost.
- [ ] Gross Profit is accurate.
- [ ] Category/Brand/Product dimensions can be supported.
- [ ] Purchase/Return/Expense data is available for reporting.

---

# 41. Final Architectural Rule

The most important rule for this phase is:

> **Master Data describes the current business configuration. Transactions preserve what actually happened. Inventory Cost Layers preserve how stock was valued. Payments preserve how money moved.**

Therefore:

```text
Master Data
    ↓
defines current behavior

Transactions
    ↓
preserve historical events

Cost Layers
    ↓
preserve historical inventory cost

Payments
    ↓
preserve money movement

Reports
    ↓
aggregate the historical truth
```

This separation should guide the database schema, Domain logic, Application services, API contracts, and UI implementation.
