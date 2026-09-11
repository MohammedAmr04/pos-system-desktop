# Dry Clean Edition Plan

## Purpose

The Dry Clean edition will extend the POS v2 foundation with a controlled workflow for receiving garments from a customer, tracking the order through processing, and completing delivery and payment.

This is a service-order workflow, not a purchase workflow. The garments remain the customer's property and must not be treated as saleable inventory.

## Core Workflow

```text
Received -> Processing -> Ready -> Delivered
                       \-> Needs review
Received -> Cancelled
```

The initial release should keep the workflow simple and visible. Every status change must record the user and timestamp.

## Customer and Order Data

Each order should contain:

- A unique, human-readable receipt number.
- Customer name and phone number.
- Optional customer address and notes.
- Date and time received.
- Expected delivery date and time.
- Current status.
- Assigned employee, when applicable.
- Subtotal, discount, total, paid amount, and remaining balance.
- Payment method and payment history.
- General order notes.

## Garment Line Data

Each order line should contain:

- Garment type, such as shirt, trousers, suit, dress, or blanket.
- Quantity.
- Color or identifying description.
- Service type, such as dry clean, wash, press, stain treatment, or repair.
- Price per item and line total.
- Condition before acceptance.
- Existing damage, stains, missing buttons, or other customer-visible notes.
- Internal processing notes.

The initial version should support text descriptions. Photos and per-item QR codes remain future enhancements unless the pilot proves they are necessary.

## Status Rules

### Received

- The order has been accepted by the store.
- Garments and their pre-existing condition have been recorded.
- The expected delivery date is visible.
- The customer can receive a printed receipt.

### Processing

- Work has started.
- Editing garment lines or prices requires a supervisor permission.

### Ready

- All garments are ready for customer collection.
- The customer may be notified manually or through a future messaging integration.

### Delivered

- The garments were handed back to the customer.
- The delivery user and timestamp are recorded.
- The order cannot be edited afterward except through a controlled correction process.

### Needs review

- Used when there is damage, a missing item, a quality issue, or another exception.
- Delivery should require supervisor approval when this status is active.

### Cancelled

- The order is stopped before delivery.
- The cancellation reason is mandatory.
- A cancelled order remains visible in reports and audit history.

## Required Screens

- Dry Clean dashboard with counts by status.
- New order screen.
- Orders list with server-side search, status, date, and customer filters.
- Order details screen.
- Order status update controls.
- Ready-for-delivery list.
- Delivery and payment dialog.
- Customer search and customer history.
- Printable receipt.
- Reports for orders, revenue, outstanding balances, late orders, and cancelled orders.

## Permissions

The feature should use dedicated permissions rather than relying on generic product permissions:

- View dry-clean orders.
- Create dry-clean orders.
- Edit received orders.
- Change processing status.
- Mark an order ready.
- Deliver an order.
- Apply a discount or override a price.
- View dry-clean reports.
- Manage exceptions and cancellations.

The backend remains the source of truth. Hiding a button in the frontend is only a usability improvement.

## Payment and Delivery Rules

- A delivery operation must identify the order and the delivering user.
- The system must show total, paid, and remaining amounts before confirmation.
- Full payment is the default requirement.
- Delivery with an outstanding balance requires an explicit permission.
- Every payment is recorded through the existing payment model or a dedicated service-order payment reference.
- Reprinting a receipt must not create a duplicate payment or delivery event.

## Audit Requirements

Audit events should cover:

- Order creation.
- Garment line additions, removals, and price changes.
- Status changes.
- Discounts and price overrides.
- Payments.
- Delivery.
- Cancellation.
- Exception resolution.

## Out of Scope for the First Dry Clean Release

- Customer mobile application.
- Automatic SMS or WhatsApp messages.
- Home delivery routing.
- Photos for every garment.
- Individual QR tracking for every garment.
- Multi-branch synchronization.
- Cloud deployment.
- Full accounting integration.

## Acceptance Criteria

- A cashier can create an order with multiple garment lines.
- The customer receives a clear receipt number and printed receipt.
- Staff can find an order by receipt number or phone number.
- Staff can move the order through the permitted statuses.
- The system prevents invalid delivery or duplicate delivery.
- Payment and remaining balance are correct.
- A manager can review exceptions and cancellations.
- All sensitive changes appear in the audit log.
- The normal POS sales workflow continues to work independently.

