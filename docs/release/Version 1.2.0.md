# POS System Release Notes

# Version 1.2.0

Release Name

Smart Discount & Low Stock Management

---

## Overview

This release introduces a new discount engine with profit protection,
product-level discount eligibility, configurable low stock thresholds,
and improvements across backend and frontend.

This is a Business Logic update and is fully backward compatible.

---

# New Features

## Product Discount Eligibility

Products can now be marked as:

- Allow Discount
- Do Not Allow Discount

Invoice discounts ignore products that do not allow discounts.

---

## Configurable Low Stock

Each product now has its own Low Stock Threshold.

The system no longer relies on hardcoded values.

---

## Smart Invoice Discount Engine

Invoice discounts now work only on eligible products.

Supported:

- Percentage Discount
- Fixed Amount Discount

---

## Internal Discount Distribution

Discounts are internally distributed across invoice items.

This improves:

- Returns
- Profit Reports
- Invoice Accuracy
- Future Analytics

The cashier still sees a single invoice discount.

---

## Profit Protection

The system now prevents selling products below cost price.

Validation exists in both Backend and Frontend.

Invoices violating this rule cannot be completed.

---

# Database Changes

## Product

Added

- allowDiscount
- lowStockThreshold

---

## Invoice

Added

- discountType
- discountValue
- discountAmount

---

## InvoiceDetail

Added

- discountAmount

---

# Backend Improvements

Updated

- Invoice Calculation Service
- Discount Engine
- Validation Layer
- Product APIs
- Invoice APIs
- Reports

---

# Frontend Improvements

## Product Page

Added

- Allow Discount Switch
- Low Stock Threshold Field

---

## Invoice Screen

Added

- Smart Discount Validation
- Profit Protection Validation
- Better Arabic Error Messages

---

# Reports

Low Stock Report now uses product-specific thresholds.

---

# Technical Notes

The discount engine now performs the following sequence:

1. Calculate eligible products.
2. Ignore non-discountable products.
3. Calculate invoice discount.
4. Distribute discount internally.
5. Validate profit margin.
6. Save distributed discounts.
7. Complete invoice.

---

# Compatibility

Database Migration Required

Yes

Frontend Update Required

Yes

Backend Update Required

Yes

API Changes

Minor

Breaking Changes

None

---

# Future Ready

This implementation prepares the system for:

- Promotions
- Category Discounts
- Customer Discounts
- Coupons
- Loyalty Programs
- Advanced Profit Reports
- Partial Returns
- Multi-level Pricing