# Release 1.4 - Advanced Pricing & Product Units

## Objective

Implement a complete pricing and product units system that allows the POS to support:

* Multiple selling prices
* Retail & Wholesale selling
* Price override during checkout
* Line discounts
* Multiple selling units
* Automatic unit conversion
* Pack quantities
* Unit-specific barcodes

This feature should replace the current single-price and single-unit product model with a scalable design suitable for supermarkets, grocery stores, wholesalers, pharmacies, and retail businesses.

---

# Business Goals

The current system assumes that every product:

* Has one selling price.
* Has one barcode.
* Is sold only as a single unit.

This is not realistic.

Real businesses sell products as:

* Piece
* Pack
* Box
* Carton
* Dozen
* Bottle
* Case

Each unit may have:

* Different barcode
* Different retail price
* Different wholesale price

The POS must support all of these scenarios.

---

# New Product Structure

Products should no longer contain selling prices directly.

Instead, each product contains one or more selling units.

Example

Pepsi

Units

* Piece
* Pack
* Carton

Each unit has its own:

* Quantity Factor
* Retail Price
* Wholesale Price
* Barcode(s)

---

# Database Design

## Products

Fields remain unchanged except removing selling price fields.

Example

* Id
* Name
* CategoryId
* PurchasePrice
* Description
* etc.

---

## ProductUnits

Create a new table.

Fields

* Id
* ProductId
* UnitName
* QuantityFactor
* RetailPrice
* WholesalePrice
* IsBaseUnit
* CreatedAt

---

### Business Rules

Every product must contain exactly one Base Unit.

Example

Piece

Quantity Factor = 1

IsBaseUnit = true

All inventory is stored using the Base Unit.

---

## ProductBarcodes

Modify barcode table to belong to ProductUnit instead of Product.

Fields

* Id
* ProductUnitId
* Barcode
* IsDefault
* CreatedAt

---

### Barcode Rules

* Barcode must be unique.
* A unit may have multiple barcodes.
* Different units may have different barcodes.

Example

Piece

Barcode

622111111111

Pack

Barcode

622111111128

Carton

Barcode

622111111135

---

# Product Creation

When creating a new product:

System automatically creates

Base Unit

Example

Unit Name

Piece

Quantity Factor

1

Retail Price

User Input

Wholesale Price

Optional

Barcode

User Input

This keeps product creation simple.

The user should not need to manually create the base unit.

---

# Product Details

Add a new section

Product Units

Example

---

Piece

Qty Factor

1

Retail

10

Wholesale

9

Barcode

622111111111

---

Pack

Qty Factor

6

Retail

55

Wholesale

50

Barcode

622111111128

---

Carton

Qty Factor

24

Retail

210

Wholesale

195

Barcode

622111111135

---

Actions

* Add Unit
* Edit Unit
* Delete Unit

---

# Unit Rules

Base Unit

Cannot be deleted.

Quantity Factor must always equal 1.

Other units

Must have Quantity Factor greater than 1.

---

# Invoice Price Mode

Inside POS screen

Add selector

Invoice Price Mode

Options

* Retail
* Wholesale

Current mode determines which selling price is used automatically.

---

### Retail Mode

Uses

Retail Price

---

### Wholesale Mode

Uses

Wholesale Price

If Wholesale Price is empty

Fallback to Retail Price.

---

# Barcode Scanning

Cashier scans barcode.

System searches

ProductBarcode

↓

ProductUnit

↓

Product

↓

Adds correct unit automatically.

Example

Scan

622111111128

System

Product

Pepsi

Unit

Pack

Quantity

1 Pack

Price

Pack Retail Price

---

# Inventory

Inventory must always be stored using the Base Unit.

Example

Current Stock

120 Pieces

Sell

2 Packs

Pack Size

6 Pieces

System

Subtract

12 Pieces

Remaining Stock

108 Pieces

No inventory should ever be stored as Packs or Cartons.

---

# Unit Conversion

The system must automatically convert every sold unit into the Base Unit.

Examples

1 Pack

↓

6 Pieces

1 Carton

↓

24 Pieces

Reports and inventory calculations always use Base Units internally.

---

# Multiple Selling Prices

Each Product Unit supports

* Retail Price
* Wholesale Price

Example

Piece

Retail

10

Wholesale

9

Pack

Retail

55

Wholesale

50

Carton

Retail

210

Wholesale

195

---

# Edit Unit Price

Inside POS

Cashier can edit the selling price of one invoice line.

This changes only

InvoiceItem.UnitPrice

It must never update ProductUnit prices.

Use Cases

* VIP Customer
* Negotiated Price
* Clearance Sale
* Damaged Product
* Manual Wholesale Adjustment

Store

* Original Price
* Overridden Price

for auditing and reporting.

---

# Line Discount

Allow discount per invoice line.

Supported Types

* Fixed Amount
* Percentage

Example

Original Price

100

Discount

10%

Final

90

Or

Original Price

100

Discount

15 EGP

Final

85

Discount affects only that invoice item.

Product prices remain unchanged.

---

# Invoice Item Model

Each invoice item should store

* ProductId
* ProductUnitId
* Quantity
* OriginalUnitPrice
* UnitPrice
* DiscountType
* DiscountValue
* DiscountAmount
* LineSubtotal
* FinalTotal

This ensures reports always know:

* Original selling price
* Edited selling price
* Discount applied
* Final selling price

---

# POS Item Actions

When selecting an item inside the invoice, provide the following actions:

* Edit Quantity
* Edit Unit Price
* Apply Discount
* Remove Item

Future actions may include:

* Add Note
* Change Tax
* Manager Approval
* Return Item

---

# Validation Rules

* Every product must have one Base Unit.
* Base Unit Quantity Factor must equal 1.
* Barcode must be unique.
* Quantity Factor must be greater than zero.
* Retail Price is required.
* Wholesale Price is optional.
* Pack Quantity must be greater than one.
* Unit prices cannot be negative.
* Discounts cannot exceed line total.

---

# Migration

Existing products should automatically migrate.

Each existing product becomes:

One Base Unit

Quantity Factor

1

Current Selling Price

↓

Retail Price

Current Barcode

↓

Base Unit Barcode

Wholesale Price

Initially empty.

No existing data should be lost.

---

# Acceptance Criteria

* Products support multiple selling units.
* Every unit has its own barcode.
* Every unit has Retail Price.
* Every unit may have Wholesale Price.
* POS supports Retail and Wholesale selling modes.
* Barcode scanning detects the correct unit automatically.
* Inventory always uses Base Units.
* Unit conversion happens automatically.
* Cashier can override line price.
* Cashier can apply fixed or percentage discounts.
* Original prices remain unchanged.
* Reports preserve original price, overridden price, discount, and final price.
* Existing products migrate without breaking current functionality.

---

# Future Ready

This architecture prepares the POS for future features without requiring another database redesign.

Examples

* Purchase Invoices
* Supplier Pricing
* Promotions
* Customer Price Lists
* Loyalty Programs
* Batch Tracking
* Multiple Warehouses
* Barcode Printing
* Dynamic Price Lists
* Permission-based Price Editing
* Manager Approval for Discounts
