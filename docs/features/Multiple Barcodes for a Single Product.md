# Feature Request: Multiple Barcodes for a Single Product

## Objective

Implement support for **multiple barcodes assigned to the same product** to eliminate duplicate products when suppliers provide different barcodes for identical items.

This feature should be designed to work with the current POS architecture without introducing Purchase Invoices yet.

---

# Background

Currently, every product has only one barcode.

In real businesses, the same product may arrive with different barcodes due to:

* Different distributors
* Different packaging
* Manufacturer barcode changes
* Imported vs local versions

Example:

| Barcode      | Product     | Purchase Price | Selling Price |
| ------------ | ----------- | -------------: | ------------: |
| 622111111111 | Pepsi 330ml |           8.50 |         10.00 |
| 622222222222 | Pepsi 330ml |           8.70 |         10.00 |
| 622333333333 | Pepsi 330ml |           8.20 |         10.00 |

Although there are multiple barcodes, they all represent the same product.

The current system forces users to create duplicate products, which causes:

* Duplicate products
* Incorrect inventory
* Difficult product management
* Search confusion
* Reporting inconsistencies

---

# Business Goal

A product should support **multiple barcodes**.

Scanning any barcode should always return the same product.

---

# Scope

This feature must NOT introduce Purchase Invoices.

Purchase Price will continue to be stored inside the Product as it is today.

The only goal of this feature is barcode management.

---

# Database Changes

## Remove barcode from Products table

The barcode should no longer be stored directly inside the Products table.

Instead, create a dedicated table.

---

## New Table

### ProductBarcodes

Fields:

* Id
* ProductId (FK -> Products)
* Barcode
* IsDefault
* CreatedAt

Rules:

* Barcode must be UNIQUE across the system.
* One product can have multiple barcodes.
* One barcode belongs to only one product.
* The first barcode added automatically becomes the default barcode.

---

# Product Creation

The existing Add Product screen should continue to work almost exactly the same.

User enters:

* Product Name
* Barcode
* Purchase Price
* Selling Price
* Category
* etc.

On Save:

1. Create Product.
2. Automatically create the first ProductBarcode record.
3. Mark it as IsDefault = true.

The user should not notice any workflow difference.

---

# Edit Product

Add a new section called:

## Barcodes

Example:

Primary Barcode

622111111111

Other Barcodes

* 622222222222
* 622333333333

Button:

* Add Barcode

---

# Add Barcode Dialog

When clicking **Add Barcode**, open a modal.

Fields:

* Barcode

Buttons:

* Save
* Cancel

Validation:

Before saving:

* Check if barcode already exists.

If barcode exists:

Display:

"This barcode is already assigned to another product."

Do not allow duplicates.

---

# POS Selling Screen Enhancement

## Unknown Barcode Workflow

Current behavior:

When cashier scans an unknown barcode:

"Barcode not found"

Replace this behavior.

When an unknown barcode is scanned, display a dialog.

---

## Dialog

Title:

Barcode Not Found

Message:

This barcode is not assigned to any product.

Barcode:

<scanned barcode>

Actions:

1. Create New Product
2. Link To Existing Product
3. Cancel

---

# Create New Product

Should behave exactly like today.

Open Add Product screen.

The scanned barcode should already be filled automatically.

---

# Link To Existing Product

Open a searchable product picker.

Allow searching by:

* Product Name
* Existing Barcode

Example:

Search Product

Pepsi

Results

* Pepsi 330 ml
* Pepsi Diet
* Pepsi Can

User selects a product.

Click Confirm.

System creates:

ProductBarcode

ProductId = Selected Product

Barcode = Scanned Barcode

---

# Smart POS Behavior

After successfully linking the barcode:

The POS should automatically:

1. Save the barcode.
2. Retrieve the linked product.
3. Add the product to the current cart automatically.

The cashier should NOT need to scan the barcode again.

This creates a seamless checkout experience.

---

# Validation Rules

* Barcode is required.
* Barcode must be unique.
* Product may contain unlimited barcodes.
* Every product must always have at least one barcode.
* Default barcode cannot be deleted unless another barcode becomes default.

---

# UI Requirements

Product Details page should include a dedicated Barcode Management section.

Example:

---

Barcodes

Primary

622111111111

Other Barcodes

622222222222

622333333333

[ Add Barcode ]

---

Each secondary barcode should support:

* Delete
* Set As Default (optional for future)

---

# Technical Notes

The barcode lookup service should search inside ProductBarcodes instead of Products.

Current flow:

Scan Barcode

↓

Find ProductBarcode

↓

Load Product

↓

Continue POS sale normally

No changes should be required in the checkout process after the product is resolved.

---

# Backward Compatibility

During migration:

* Existing Products.Barcode values should be copied into ProductBarcodes.
* Each migrated barcode should become IsDefault = true.
* Existing functionality should continue working without data loss.

---

# Future Compatibility

This design prepares the system for future features without requiring another database redesign.

Examples:

* Purchase Invoices
* Supplier-specific purchasing
* Barcode printing
* Packaging variations
* Product aliases
* Inventory batches
* Cost history

No implementation of these features is required now; this structure simply ensures the system can support them later.

---

# Acceptance Criteria

* A product can have multiple barcodes.
* Barcode remains unique across the system.
* Existing product creation flow continues to work.
* Users can manually add additional barcodes.
* Unknown barcode dialog appears during POS checkout.
* Users can link an unknown barcode to an existing product.
* After linking, the product is automatically added to the cart.
* Duplicate products caused by different barcodes are eliminated.
* Existing data is migrated safely without breaking current functionality.
