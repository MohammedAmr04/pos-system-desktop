# POS — Feature-Based Access Control & Permissions Architecture

## Objective

Analyze the existing POS project thoroughly and introduce a clean, scalable authorization architecture based on:

1. **Features**
2. **Roles**
3. **Permissions**
4. **Role-Permission assignments**
5. **Restaurant/Tenant feature availability**

The goal is NOT to blindly refactor the project.

First understand the existing codebase, identify all current features and authorization-related behavior, classify them correctly, then propose and implement the architecture with minimal disruption to the existing POS functionality.

---

# 1. Important Concepts

Use these definitions throughout the implementation.

## Feature

A Feature answers:

> "Does this restaurant/tenant have this capability enabled?"

Examples:

```text
product_discount
invoice_discount
multiple_barcodes
multiple_units
wholesale_price
reports
```

A Feature is associated with the restaurant/tenant.

Example:

```text
Restaurant A
    product_discount = enabled

Restaurant B
    product_discount = disabled
```

Even if a user has the required permission, the feature must not be available if the restaurant does not have it enabled.

---

## Permission

A Permission answers:

> "Is this user/role allowed to perform this action?"

Use the format:

```text
resource.action
```

Examples:

```text
orders.view
orders.create
orders.update
orders.delete

products.view
products.create
products.update
products.delete

discounts.product
discounts.invoice

reports.view
reports.export
```

Permissions belong to Roles.

Users receive permissions through their assigned Roles.

Do NOT create a large list of permissions directly on every user unless the existing architecture absolutely requires it.

Preferred relationship:

```text
User
  ↓
Role
  ↓
Permissions
```

---

# 2. First Task — Analyze the Existing Project

Before changing code, inspect the entire project.

Understand:

* Backend architecture
* Frontend architecture
* Database structure
* Authentication
* Existing users
* Existing roles
* Existing permission logic
* Existing API endpoints
* Existing pages
* Existing components
* Existing feature flags/configuration
* Existing restaurant/tenant structure
* Existing business rules
* Existing validation
* Existing authorization checks

Do NOT start implementation immediately.

First produce an analysis report.

---

# 3. Build a Complete Feature Inventory

Search the entire project and identify all major POS features.

Examples include but are not limited to:

```text
Orders
Products
Categories
Customers
Tables
Takeaway
Delivery
Payments
Discounts
Product Discount
Invoice Discount
Multiple Barcodes
Multiple Units
Unit Conversion
Pack Quantity
Pack Barcode
Retail Price
Wholesale Price
Reports
Daily Reports
Expenses
Returns
Held Invoices
Printing
Receipt Printing
End of Shift
Users
Roles
Settings
```

Do not assume the above list is complete.

Discover additional features from the codebase.

---

# 4. Classify Every Feature

For every discovered feature, classify it as one of:

### A. Feature

The restaurant may or may not have access to this capability.

Example:

```text
product_discount
multiple_units
wholesale_price
```

### B. Permission

The feature exists for the restaurant, but different users/roles may or may not be allowed to use it.

Example:

```text
discounts.product
discounts.invoice
reports.export
products.delete
```

### C. UI-only

The behavior exists only for presentation/UX and does not represent a meaningful authorization or business capability.

Do NOT create unnecessary permissions for simple UI behavior.

### D. Business Rule

The behavior affects business data or financial calculations and therefore must be enforced by the backend/business layer.

Examples:

```text
discount amount
unit price
payment amount
order total
tax
refund
```

A UI restriction must NEVER be treated as the only security mechanism for these operations.

---

# 5. Create the Main Audit Table

Create a detailed table with the following columns:

| Area | Feature / Action | Type | Resource | Permission Key | Feature Key | Frontend Location | Backend Endpoint | Requires Backend Authorization | Notes |
| ---- | ---------------- | ---- | -------- | -------------- | ----------- | ----------------- | ---------------- | ------------------------------ | ----- |

Example:

| Area      | Feature / Action   | Type                 | Resource  | Permission Key    | Feature Key       | Frontend       | Backend     | Authorization | Notes              |
| --------- | ------------------ | -------------------- | --------- | ----------------- | ----------------- | -------------- | ----------- | ------------- | ------------------ |
| Discounts | Product Discount   | Feature + Permission | discounts | discounts.product | product_discount  | Order screen   | Order API   | Yes           | Financial impact   |
| Discounts | Invoice Discount   | Feature + Permission | discounts | discounts.invoice | invoice_discount  | Payment screen | Order API   | Yes           | Financial impact   |
| Products  | Multiple Barcode   | Feature              | products  | -                 | multiple_barcodes | Product form   | Product API | Depends       | Product capability |
| Reports   | Export Report      | Permission           | reports   | reports.export    | reports           | Reports page   | Reports API | Yes           | Sensitive data     |
| UI        | Advanced UI filter | UI-only              | -         | -                 | -                 | Component X    | None        | No            | UI behavior only   |

The actual table must be generated from the real project, not invented.

---

# 6. Identify Existing Roles

Find every existing role in the project.

For each role determine:

```text
Role name
Current permissions
Current usage
Where it is checked
Whether it is hard-coded
Whether it is stored in DB
Whether it exists only in frontend
```

Create:

| Role | Current Behavior | Existing Permissions | Problems | Recommended Changes |
| ---- | ---------------- | -------------------- | -------- | ------------------- |

---

# 7. Design the Permission Model

If the current architecture does not already have a proper permission system, design one.

Preferred entities:

```text
Users
Roles
Permissions
UserRoles
RolePermissions
```

For multi-restaurant / multi-tenant support:

```text
Restaurants
RestaurantFeatures
```

Potential schema:

```text
Permissions
-----------
Id
Key
Name
Description
Resource
Action
CreatedAt
UpdatedAt
```

Example:

```text
Key: discounts.product
Name: Product Discount
Resource: discounts
Action: product
```

Roles:

```text
Roles
-----
Id
Name
Description
CreatedAt
UpdatedAt
```

RolePermissions:

```text
RolePermissions
---------------
RoleId
PermissionId
```

UserRoles:

```text
UserRoles
---------
UserId
RoleId
```

RestaurantFeatures:

```text
RestaurantFeatures
------------------
RestaurantId
FeatureKey
Enabled
```

Adapt this design to the existing project's conventions instead of blindly copying it.

---

# 8. Permission Naming Convention

Use a consistent convention:

```text
resource.action
```

Examples:

```text
orders.view
orders.create
orders.update
orders.delete

products.view
products.create
products.update
products.delete

customers.view
customers.create
customers.update

discounts.product
discounts.invoice

payments.create
payments.refund

reports.view
reports.export

users.view
users.create
users.update
users.delete

roles.view
roles.create
roles.update
roles.delete
```

Avoid names like:

```text
CanManageEverything
CanDoOrders
AdminPermission
SuperPermission
```

Permissions should represent a specific capability/action.

---

# 9. Backend Permission API

Design and implement APIs for managing permissions.

At minimum evaluate whether the project needs:

```http
GET    /api/permissions
GET    /api/permissions/{id}

POST   /api/permissions
PUT    /api/permissions/{id}
DELETE /api/permissions/{id}
```

However, if permissions are system-defined and should NOT be freely created by restaurant admins, do NOT expose unnecessary CRUD APIs.

Instead consider:

```http
GET /api/permissions
```

for reading system permissions, while creating permissions through:

* database seed
* migration
* backend constants
* controlled system configuration

Determine which approach is appropriate based on the project's business model.

---

# 10. Role-Permission APIs

Evaluate and implement APIs such as:

```http
GET /api/roles
GET /api/roles/{id}

POST /api/roles
PUT /api/roles/{id}
DELETE /api/roles/{id}

GET /api/roles/{id}/permissions

PUT /api/roles/{id}/permissions
```

Example request:

```json
{
  "permissionIds": [
    1,
    2,
    5,
    7
  ]
}
```

The backend should validate:

* Permission exists
* Role exists
* User is authorized to modify roles
* Restaurant/tenant boundaries are respected
* System roles cannot be modified if the business rules prohibit it

---

# 11. Current User Authorization API

The frontend needs to know the current user's effective access.

Evaluate the existing authentication response.

If appropriate, expose something like:

```http
GET /api/auth/me
```

Response:

```json
{
  "user": {
    "id": 10,
    "name": "Ahmed"
  },
  "roles": [
    "Manager"
  ],
  "permissions": [
    "orders.view",
    "orders.create",
    "discounts.product",
    "discounts.invoice"
  ],
  "features": [
    "product_discount",
    "invoice_discount",
    "multiple_units"
  ]
}
```

Adapt this to the existing auth architecture.

Do not duplicate authorization state unnecessarily.

---

# 12. Backend Authorization

Create a reusable authorization mechanism.

For example:

```text
RequirePermission("products.delete")
```

or equivalent middleware / attribute / policy based on the existing backend framework.

Every sensitive endpoint must validate authorization on the backend.

Example:

```text
DELETE /api/products/{id}
        ↓
Authentication
        ↓
Permission Check
        ↓
products.delete
        ↓
Business Validation
        ↓
Database
```

Do NOT rely on frontend checks for security.

---

# 13. Feature Check

Create a reusable backend mechanism for checking restaurant features.

Conceptually:

```text
HasFeature("product_discount")
```

Then sensitive operations can require:

```text
Feature enabled
AND
User has permission
```

Example:

```text
Product Discount

Restaurant Feature:
product_discount = true

AND

User Permission:
discounts.product = true
```

Only then should the operation be allowed.

---

# 14. Frontend Authorization Utilities

Create reusable frontend utilities.

For example:

```ts
hasPermission(permission)
hasFeature(feature)
```

Or an equivalent hook/context/service matching the existing architecture.

Example:

```ts
const canProductDiscount =
  hasFeature(FEATURES.PRODUCT_DISCOUNT) &&
  hasPermission(PERMISSIONS.DISCOUNTS_PRODUCT);
```

Then:

```tsx
{canProductDiscount && (
  <ProductDiscountButton />
)}
```

Avoid scattering raw permission strings throughout the application.

Create centralized constants:

```ts
export const PERMISSIONS = {
  ORDERS_VIEW: "orders.view",
  ORDERS_CREATE: "orders.create",

  PRODUCT_DISCOUNT: "discounts.product",
  INVOICE_DISCOUNT: "discounts.invoice",

  REPORTS_VIEW: "reports.view",
  REPORTS_EXPORT: "reports.export",
} as const;
```

And:

```ts
export const FEATURES = {
  PRODUCT_DISCOUNT: "product_discount",
  INVOICE_DISCOUNT: "invoice_discount",
  MULTIPLE_BARCODES: "multiple_barcodes",
  MULTIPLE_UNITS: "multiple_units",
} as const;
```

Adapt naming to project conventions.

---

# 15. UI for Roles & Permissions

Create an administration UI for managing roles and their permissions.

Suggested page:

```text
Settings
  └── Roles & Permissions
```

Roles list:

| Role    | Description   | Users | Permissions | Actions |
| ------- | ------------- | ----- | ----------- | ------- |
| Manager | Store manager | 2     | 18          | Edit    |
| Cashier | POS cashier   | 5     | 8           | Edit    |

Role form:

```text
Role Name
Description

Permissions

Orders
[x] View Orders
[x] Create Orders
[x] Update Orders
[ ] Delete Orders

Products
[x] View Products
[x] Create Products
[x] Update Products
[ ] Delete Products

Discounts
[x] Invoice Discount
[ ] Product Discount

Reports
[x] View Reports
[ ] Export Reports
```

Group permissions by resource/module.

Do NOT display a flat list of dozens of permissions if grouping improves usability.

---

# 16. Permission Creation UI

Before creating a "Create Permission" UI, determine whether permissions are:

### System-defined

If permissions represent capabilities of the POS product, prefer defining them in code/database migrations/seeds.

Example:

```text
orders.view
orders.create
orders.update
discounts.product
reports.export
```

Restaurant admins should generally NOT be able to invent:

```text
my.custom.permission
```

unless the product explicitly supports custom permissions.

Therefore, most likely:

```text
Permission Management
    ↓
Read-only system permissions

Role Management
    ↓
Assign/unassign permissions
```

Do not create unnecessary permission CRUD UI.

If the project requirements indicate custom permissions are needed, then design a proper CRUD UI.

---

# 17. Feature Management UI

Create a separate UI for restaurant feature configuration if the product requires it.

Example:

```text
Settings
  └── Features

[x] Invoice Discount
[ ] Product Discount
[x] Multiple Barcodes
[x] Multiple Units
[ ] Wholesale Price
```

This must remain separate from Role & Permission management.

Remember:

```text
Feature = Restaurant capability
Permission = User capability
```

---

# 18. Frontend Route Protection

Inspect existing routes.

Determine which pages need permissions.

Examples:

```text
/orders
/products
/customers
/reports
/settings/users
/settings/roles
```

Create reusable route protection if appropriate.

Example concept:

```tsx
<PermissionGuard permission="reports.view">
    <ReportsPage />
</PermissionGuard>
```

Do not rely only on route hiding.

Backend APIs must also enforce authorization.

---

# 19. Important Business Rule

For any financial or business-critical operation:

```text
Discount
Price
Payment
Refund
Tax
Order Total
```

frontend hiding is NOT enough.

Even if:

```tsx
{canProductDiscount && <ProductDiscount />}
```

the backend must validate the operation when the request is submitted.

The frontend is responsible for UX.

The backend is responsible for security and business integrity.

---

# 20. Do Not Overengineer

Do NOT introduce:

* ABAC
* ReBAC
* complex policy engines
* role inheritance
* dynamic authorization DSL
* microservice authorization
* unnecessary permission hierarchy

unless the existing project clearly requires them.

Start with:

```text
Feature Flags
+
RBAC
+
Backend Authorization
```

Add more advanced authorization only when an actual requirement appears.

---

# 21. Implementation Order

Follow this order:

### Phase 1 — Audit

* Analyze existing project
* Identify features
* Identify roles
* Identify permissions
* Identify UI-only behavior
* Identify backend-sensitive behavior
* Produce the audit table

### Phase 2 — Architecture

* Design DB changes
* Define permission naming convention
* Define feature naming convention
* Define roles
* Define role-permission relationships
* Define restaurant-feature relationships

### Phase 3 — Backend

* Create migrations
* Create entities/models
* Create seed permissions
* Create default roles if needed
* Implement permission service
* Implement feature service
* Implement authorization middleware/policies
* Implement role APIs
* Implement permission APIs where appropriate
* Implement current-user access endpoint
* Protect sensitive endpoints

### Phase 4 — Frontend

* Create permission constants
* Create feature constants
* Create auth access state
* Create `hasPermission`
* Create `hasFeature`
* Create permission/feature guards
* Update existing UI visibility
* Protect routes where appropriate

### Phase 5 — Administration UI

Create:

```text
Roles List
Role Details
Create Role
Edit Role
Assign Permissions
Feature Configuration
```

Only create Permission CRUD UI if custom permissions are actually required.

### Phase 6 — Testing

Test at least:

```text
Manager + Feature enabled
Manager + Feature disabled

Cashier + Feature enabled
Cashier + Feature disabled

User without permission
User with permission

Direct API request without permission
Direct API request with permission

Restaurant A accessing Restaurant B resources
```

Especially verify that frontend-hidden actions cannot be executed through direct API calls.

---

# 22. Deliverables Before Coding

Before modifying implementation, produce these artifacts:

## A. Feature Inventory

Complete table of discovered features.

## B. Permission Inventory

```text
Permission Key
Resource
Action
Description
Used By
Related Feature
```

## C. Feature Inventory

```text
Feature Key
Name
Description
Restaurant Configurable?
Related Permissions
```

## D. Role Matrix

Example:

| Role    | orders.view | orders.create | products.delete | discounts.product | reports.export |
| ------- | ----------- | ------------- | --------------- | ----------------- | -------------- |
| Admin   | ✓           | ✓             | ✓               | ✓                 | ✓              |
| Manager | ✓           | ✓             | ✓               | ✓                 | ✓              |
| Cashier | ✓           | ✓             | ✗               | ✗                 | ✗              |

Use the actual roles and permissions discovered in the project.

## E. API Design

List every new/modified endpoint:

```text
HTTP Method
Route
Purpose
Authentication
Required Permission
Required Feature
Request
Response
```

## F. Database Changes

Document:

```text
New tables
Modified tables
Relationships
Indexes
Unique constraints
Seed data
Migration strategy
```

## G. Frontend Changes

Document:

```text
New hooks
New services
New contexts/stores
New guards
New constants
Modified pages
Modified components
```

---

# 23. Critical Constraint

Do not rewrite working POS functionality unnecessarily.

Preserve:

* Existing business rules
* Existing API contracts where possible
* Existing UI behavior where access is allowed
* Existing database data
* Existing authentication behavior
* Existing printing functionality
* Existing order/payment behavior

Make the authorization system incremental and backward-compatible.

---

# 24. Final Expected Output

Before implementation, return:

1. **Architecture summary**
2. **Feature inventory table**
3. **Permission inventory table**
4. **Role matrix**
5. **Feature matrix**
6. **Database design**
7. **API design**
8. **Frontend architecture**
9. **Roles & Permissions UI design**
10. **Migration/seed strategy**
11. **Security risks found in the current project**
12. **Implementation plan ordered by dependency**

Then STOP and wait for approval before making large architectural changes.

Do not implement the entire system before presenting the analysis and proposed architecture.
# 25. Documentation — Permissions & Features

As part of the analysis and implementation, create dedicated Markdown documentation files for the discovered and implemented authorization model.

The documentation must be generated from the actual project analysis.

Do NOT invent permissions or features that do not exist unless they are explicitly proposed as new architecture.

---

## A. Create `docs/PERMISSIONS.md`

Create a complete documentation file containing every permission discovered or introduced in the project.

The file should include:

### 1. Permission Overview

Explain briefly:

* What permissions represent
* How permissions relate to Roles
* Permission naming convention
* Difference between Feature and Permission

Use the convention:

```text
resource.action
```

---

### 2. Complete Permission Table

Create a table similar to:

| Permission Key    | Resource  | Action  | Name             | Description                             | Related Feature  | Sensitive? |
| ----------------- | --------- | ------- | ---------------- | --------------------------------------- | ---------------- | ---------- |
| orders.view       | orders    | view    | View Orders      | Allows viewing orders                   | orders           | No         |
| orders.create     | orders    | create  | Create Orders    | Allows creating orders                  | orders           | Yes        |
| discounts.product | discounts | product | Product Discount | Allows applying product-level discounts | product_discount | Yes        |
| discounts.invoice | discounts | invoice | Invoice Discount | Allows applying invoice-level discounts | invoice_discount | Yes        |
| reports.export    | reports   | export  | Export Reports   | Allows exporting reports                | reports          | Yes        |

The actual table must be generated from the real project.

---

### 3. Permissions Grouped by Module

Group permissions logically:

```text
Orders
Products
Customers
Payments
Discounts
Reports
Users
Roles
Settings
Printing
Tables
Delivery
Takeaway
```

Only include modules that actually exist.

Example:

```text
## Orders

- `orders.view`
- `orders.create`
- `orders.update`
- `orders.delete`
```

---

### 4. Role → Permission Matrix

Create a complete matrix:

| Permission        | Admin | Manager | Cashier |
| ----------------- | ----: | ------: | ------: |
| orders.view       |     ✓ |       ✓ |       ✓ |
| orders.create     |     ✓ |       ✓ |       ✓ |
| orders.delete     |     ✓ |       ✓ |       ✗ |
| discounts.product |     ✓ |       ✓ |       ✗ |
| discounts.invoice |     ✓ |       ✓ |       ✓ |
| reports.export    |     ✓ |       ✓ |       ✗ |

Use the actual roles discovered in the project.

---

### 5. Backend Enforcement

For every sensitive permission, document where it is enforced.

Example:

| Permission        | Endpoint / Service      | Authorization Mechanism    |
| ----------------- | ----------------------- | -------------------------- |
| orders.delete     | DELETE /api/orders/{id} | RequirePermission          |
| discounts.product | POST /api/orders        | Permission + Feature check |
| reports.export    | GET /api/reports/export | RequirePermission          |

This section is important because the documentation should make it clear that frontend visibility is not the security boundary.

---

### 6. Frontend Usage

Document where each permission is used in the frontend.

Example:

```text
discounts.product
    ↓
OrderPage
    ↓
ProductDiscountButton
```

Use actual file paths from the project.

Example:

```text
src/pages/orders/OrderPage.tsx
src/components/orders/ProductDiscountButton.tsx
```

---

## B. Create `docs/FEATURES.md`

Create a complete documentation file containing every POS feature discovered or introduced.

---

### 1. Feature Overview

Explain:

```text
Feature = capability available to a restaurant/tenant.
```

Explain that Features are different from Permissions.

Example:

```text
Feature:
product_discount
```

means:

> The restaurant has Product Discount capability.

While:

```text
Permission:
discounts.product
```

means:

> The current user is allowed to use Product Discount.

---

### 2. Complete Feature Table

Create:

| Feature Key       | Name              | Description                           | Category  | Tenant Configurable? | Related Permissions | Frontend Location | Backend Impact |
| ----------------- | ----------------- | ------------------------------------- | --------- | -------------------- | ------------------- | ----------------- | -------------- |
| product_discount  | Product Discount  | Apply discount to individual products | Discounts | Yes                  | discounts.product   | Order Page        | Yes            |
| invoice_discount  | Invoice Discount  | Apply discount to the entire invoice  | Discounts | Yes                  | discounts.invoice   | Payment Page      | Yes            |
| multiple_barcodes | Multiple Barcodes | Allow multiple barcodes per product   | Products  | Yes                  | -                   | Product Form      | Yes            |
| multiple_units    | Multiple Units    | Piece / Pack / Carton support         | Products  | Yes                  | -                   | Product Form      | Yes            |

Again, use the actual project.

---

### 3. Features Grouped by Module

Example:

```text
## Discounts

- `product_discount`
- `invoice_discount`

## Products

- `multiple_barcodes`
- `multiple_units`
- `wholesale_price`
- `retail_price`
```

Only include discovered/proposed features.

---

### 4. Feature → Permission Relationship

For every Feature, document whether it requires permissions.

Example:

| Feature           | Required Permission(s) | Description                                          |
| ----------------- | ---------------------- | ---------------------------------------------------- |
| product_discount  | discounts.product      | User must have permission to apply product discounts |
| invoice_discount  | discounts.invoice      | User must have permission to apply invoice discounts |
| multiple_barcodes | -                      | Product configuration capability                     |

Explain cases where a feature has no permission because it is configuration-only.

---

### 5. Feature Access Flow

Document the expected access flow:

```text
Restaurant
    ↓
Feature Enabled?
    ↓
YES
    ↓
User Permission
    ↓
Permission Granted?
    ↓
YES
    ↓
Feature Available in UI
    ↓
Backend validates again for sensitive operations
```

Example:

```text
product_discount
        ↓
Restaurant has product_discount?
        ↓
        YES
        ↓
User has discounts.product?
        ↓
        YES
        ↓
Show Product Discount UI
        ↓
Backend validates Feature + Permission
```

---

### 6. Frontend Usage

For every Feature, document:

```text
Feature
Frontend component/page
Access helper
Expected behavior when disabled
```

Example:

```text
product_discount

Frontend:
src/pages/orders/OrderPage.tsx

Access:
hasFeature(FEATURES.PRODUCT_DISCOUNT)
&&
hasPermission(PERMISSIONS.PRODUCT_DISCOUNT)

Disabled behavior:
Product Discount button/modal is not rendered.
```

---

### 7. Backend Usage

Document backend enforcement where applicable.

Example:

| Feature          | Endpoint / Service | Feature Check |
| ---------------- | ------------------ | ------------- |
| product_discount | POST /api/orders   | Required      |
| invoice_discount | POST /api/orders   | Required      |
| multiple_units   | Product APIs       | Required      |

Do not mark frontend-only features as backend-enforced unless the feature actually affects backend behavior.

---

# 26. Documentation Rules

The documentation must be treated as part of the authorization system.

Whenever a new Permission or Feature is introduced:

1. Add it to the backend seed/constants.
2. Add it to the frontend constants if required.
3. Add it to `docs/PERMISSIONS.md`.
4. Add it to `docs/FEATURES.md`.
5. Add it to the relevant Role matrix.
6. Document the related API/UI behavior.

Avoid having permissions defined in code but missing from documentation.

---

# 27. Documentation Status

At the top of both files, include:

```md
> **Status:** Active
>
> This document represents the current Feature/Permission model of the POS application.
>
> Last reviewed: YYYY-MM-DD
```

Use the actual current date.

Also include a small version/change section:

```md
## Changelog

| Date | Change |
|------|--------|
| YYYY-MM-DD | Initial Feature and Permission inventory |
```

Update this when the authorization model changes.

---

# 28. Final Repository Structure

Prefer the following structure if it matches the existing project conventions:

```text
docs/
├── PERMISSIONS.md
├── FEATURES.md
├── business-overview.md
├── functional-requirements.md
├── business-rules.md
├── domain-model.md
├── database-design.md
├── api-spec.md
└── ...
```

Do not move or rename existing documentation unnecessarily.

---

# 29. Final Verification

Before finishing, verify that:

* Every discovered Feature exists in `FEATURES.md`.
* Every discovered Permission exists in `PERMISSIONS.md`.
* Every Permission has a clear naming convention.
* Every Permission has a known purpose.
* Every sensitive permission has backend enforcement.
* Every Feature has a clear tenant/restaurant meaning.
* Feature and Permission are not incorrectly mixed.
* Role-Permission relationships are documented.
* Frontend usage is documented.
* Backend endpoints are documented.
* No undocumented permission strings are scattered through the codebase.

Finally, provide a summary of:

```text
Total Features: X
Total Permissions: X
Total Roles: X
Features requiring permissions: X
Frontend-only features: X
Backend-enforced permissions: X
```

Do not simply create the Markdown files from the proposed architecture.

They must reflect the actual project after the audit and implementation plan.
