# Smart Discount & Low Stock Feature Implementation

## Goal

Implement a new feature for the POS system that introduces:

- Product discount eligibility.
- Product low stock threshold.
- Safe discount calculation.
- Profit protection.
- Frontend validation.
- Backend validation.
- Database migration.

This feature affects Database, Backend, Business Logic, API, and Frontend.

---

# Business Requirements

## Product Discount Eligibility

Each product must contain a flag:

```
allowDiscount
```

Meaning:

- true → Product participates in invoice discounts.
- false → Product is excluded from all invoice discounts.

Example:

Invoice

- Coke → 100 (allow discount)
- Water → 200 (allow discount)
- Cigarettes → 100 (NOT allow discount)

Invoice Total = 400

If cashier enters

10%

Discount must be calculated only on

300

NOT on

400

---

# Profit Protection

The system must never allow selling below cost price.

For every discounted product:

Final Unit Price >= Buy Price

If any product violates this rule:

Reject the invoice.

Example:

Buy Price = 95
Sale Price = 100

Cashier enters

10%

Reject because selling price becomes lower than buy price.

This validation MUST exist in:

- Backend (Required)
- Frontend (UX)

Frontend validation is only for user experience.
Backend validation is mandatory.

---

# Low Stock Threshold

Each product must contain:

```
lowStockThreshold
```

Instead of using a hardcoded value.

Example

Current Stock = 7

Threshold = 10

Product should appear in Low Stock Report.

---

# Discount Calculation

Discount Types

- Percentage
- Fixed Amount

Both must support the new business rules.

---

## Percentage Discount

Only discountable products participate.

Formula:

Eligible Total =
Sum(products where allowDiscount == true)

Discount =
Eligible Total × Percentage

---

## Fixed Amount Discount

Only discountable products participate.

If the entered discount exceeds the eligible amount:

Reject.

---

# Internal Discount Distribution

Although the cashier applies discount to the whole invoice,
the backend must distribute the discount internally across eligible invoice lines proportionally.

Example

Product A = 300
Product B = 200

Eligible Total = 500

Discount = 50

Distribution

A receives 30

B receives 20

Store the distributed discount in InvoiceDetail.

---

# Database Changes

## Product

Add

allowDiscount BOOLEAN NOT NULL DEFAULT TRUE

lowStockThreshold INTEGER NOT NULL DEFAULT 0

---

## Invoice

Add

discountType
discountValue
discountAmount

discountType

Percentage
Fixed

discountValue

User entered value.

discountAmount

Actual applied discount after calculations.

---

## InvoiceDetail

Add

discountAmount REAL NOT NULL DEFAULT 0

This is required for:

- Profit Reports
- Returns
- Invoice Details
- Future Analytics

---

# Backend

Update:

- Invoice Calculation Service
- Discount Engine
- Validation Layer
- Product DTO
- Product Create API
- Product Update API
- Invoice APIs
- Reports

Backend validations

✓ Ignore non-discountable products.

✓ Never sell below buy price.

✓ Reject invalid discount.

✓ Reject discount larger than eligible total.

---

# Frontend

Update Product Form

New fields

- Allow Discount (Switch)
- Low Stock Threshold (Number)

---

Update Invoice Screen

Before submitting invoice

Validate:

- Discount does not exceed allowed profit.
- Discount does not exceed eligible amount.

Show friendly Arabic error messages.

---

Invoice UI

Discount should still appear as Invoice Discount.

Cashier should not notice the internal discount distribution.

---

Reports

Low Stock Report

Must use

lowStockThreshold

instead of any hardcoded value.

---

Testing

Cover:

- Products allowing discount
- Products not allowing discount
- Mixed invoices
- Percentage discounts
- Fixed discounts
- Profit protection
- Low stock
- Returns after discount
- Invoice details correctness

---

Rules

- Do not break existing APIs.
- Keep backward compatibility.
- Keep code reusable.
- Use SOLID principles.
- Update all documentation.
- Add database migration.
- Update seed data if needed.