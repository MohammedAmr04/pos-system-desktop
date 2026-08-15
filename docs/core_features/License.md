# POS License & Subscription System

> **Status:** Proposed Architecture
>
> This document defines the licensing, subscription, device activation, and offline validation architecture for the POS application.
>
> **Last reviewed:** 2026-08-15

---

# 1. Purpose

The POS application is a Windows desktop application.

The licensing system is responsible for:

* Connecting a POS installation to a restaurant/account.
* Validating the restaurant's subscription.
* Preventing simple copy/paste of the POS installation to unauthorized devices.
* Controlling the maximum number of activated devices.
* Enabling/disabling product features based on the subscription/plan.
* Supporting offline POS operation through a limited grace period.
* Allowing device activation and deactivation.
* Providing server-side license validation.

The licensing system is separate from Roles & Permissions.

---

# 2. Core Concepts

The licensing architecture consists of:

```text
Account / Restaurant
        │
        ├── Subscription
        │
        ├── License
        │
        ├── Activated Devices
        │
        └── Users
                │
                └── Roles
                        │
                        └── Permissions
```

There are four different concepts that must not be mixed:

| Concept      | Question it answers                                |
| ------------ | -------------------------------------------------- |
| License      | Is this restaurant authorized to use the POS?      |
| Subscription | Is the restaurant's subscription currently active? |
| Feature      | What capabilities are available to the restaurant? |
| Permission   | What is this user allowed to do?                   |

---

# 3. License

A License represents the authorization for a restaurant/account to use the POS application.

Example:

```text
License
--------
License Key: POS-XXXX-XXXX-XXXX
Restaurant: Restaurant A
Status: Active
Max Devices: 3
```

A license should not be considered valid based only on local files.

The License Server is the source of truth.

---

# 4. Subscription

The subscription controls the commercial status of the restaurant.

Example:

```text
Subscription
------------
Plan: Professional
Status: Active
StartedAt: 2026-08-01
ExpiresAt: 2026-09-01
```

Possible statuses:

```text
Active
Trial
Expired
Suspended
Cancelled
```

The exact statuses should follow the business requirements.

The License Server must validate both:

```text
License Status
+
Subscription Status
```

before allowing activation or renewal.

---

# 5. Plans

A subscription may belong to a Plan.

Example:

```text
Basic
Professional
Enterprise
```

A Plan determines which Features are available.

Example:

```text
Professional
    │
    ├── product_discount
    ├── invoice_discount
    ├── multiple_barcodes
    ├── multiple_units
    └── wholesale_price
```

Recommended relationship:

```text
Plan
  ↓
Features
  ↓
Restaurant Subscription
```

Avoid hard-coding plan names throughout the frontend.

---

# 6. Features

Features determine what capabilities are available to a restaurant.

Examples:

```text
product_discount
invoice_discount
multiple_barcodes
multiple_units
wholesale_price
retail_price
advanced_reports
```

A Feature answers:

> Does this restaurant have this capability?

A Permission answers:

> Is this user allowed to use this capability?

Example:

```text
Restaurant
    product_discount = enabled

Manager
    discounts.product = allowed

Cashier
    discounts.product = denied
```

Therefore:

```text
Feature enabled
AND
Permission granted
=
User can use the feature
```

---

# 7. Device Activation

Each POS installation must be activated against a License.

Example:

```text
License
  ├── Device 1
  ├── Device 2
  └── Device 3
```

If:

```text
MaxDevices = 3
```

the fourth device should not be activated unless an existing device is deactivated/released.

---

# 8. Installation ID

An Installation ID identifies a specific installation of the POS application.

It is generated locally the first time the application is installed/launched.

Example:

```text
8f3c1c6e-7c2a-4e91-a4f0-91c9...
```

The Installation ID is:

* Randomly generated.
* Unique per installation.
* Stored locally.
* Sent to the License Server during activation.
* NOT a secret.
* NOT a security boundary by itself.

Example local data:

```json
{
  "installationId": "8f3c1c6e-7c2a-4e91-a4f0-91c9..."
}
```

Possible storage location:

```text
%ProgramData%\CompanyName\POS\
```

The exact storage mechanism should follow the existing POS architecture.

---

# 9. Machine Fingerprint

A Machine Fingerprint represents the device on which the POS is running.

The POS should collect stable machine characteristics and derive a fingerprint.

Conceptually:

```text
Machine Characteristics
        ↓
Normalize
        ↓
Hash
        ↓
Machine Fingerprint
```

Example:

```text
A7F91D2C8...
```

The exact fingerprint inputs should be selected carefully.

Do NOT rely on a single value such as:

```text
MAC Address
```

because hardware/network identifiers may change.

The fingerprint is not expected to be impossible to spoof.

Its purpose is to make simple copying of an installation to another machine ineffective.

---

# 10. Installation ID vs Machine Fingerprint

These two values have different meanings.

```text
Installation ID
    =
"This installation of the POS"
```

```text
Machine Fingerprint
    =
"This machine"
```

Example:

```text
Installation:
ABC-123

Machine:
MACHINE-X
```

The server stores the relationship:

```text
ABC-123 → MACHINE-X
```

If the same installation is copied to another machine:

```text
ABC-123 → MACHINE-Y
```

the License Server can detect the mismatch.

---

# 11. Activation Flow

The first activation should follow this flow:

```text
POS starts
    ↓
Generate Installation ID if missing
    ↓
Generate Machine Fingerprint
    ↓
User enters License Key
    ↓
POST /api/license/activate
    ↓
License Server
    ↓
Validate License
    ↓
Validate Subscription
    ↓
Check Device Limit
    ↓
Check Existing Device
    ↓
Register Device
    ↓
Create Signed Activation Token
    ↓
Return Activation
    ↓
POS stores activation locally
    ↓
POS starts
```

---

# 12. Activation Request

Example:

```http
POST /api/license/activate
```

Request:

```json
{
  "licenseKey": "POS-XXXX-XXXX-XXXX",
  "installationId": "8f3c1c6e-...",
  "machineFingerprint": "A7F91D2C8..."
}
```

---

# 13. Server-Side Activation Validation

The License Server must validate:

```text
1. License exists
2. License is active
3. Subscription exists
4. Subscription is active
5. Subscription has not expired
6. Device is allowed
7. Device limit has not been exceeded
8. Installation/device relationship is valid
9. Restaurant/account is valid
```

If all checks pass:

```text
Activation Approved
```

Otherwise:

```text
Activation Rejected
```

---

# 14. Device Registration

Suggested database structure:

```text
Devices
-------
Id
LicenseId
RestaurantId
InstallationId
MachineFingerprint
Status
ActivatedAt
LastSeenAt
DeactivatedAt
CreatedAt
UpdatedAt
```

Example:

```text
License: LIC-100

Device 1
InstallationId: ABC
Fingerprint: MACHINE-A
Status: Active

Device 2
InstallationId: DEF
Fingerprint: MACHINE-B
Status: Active
```

---

# 15. Suggested Database Model

Minimum entities:

```text
Restaurants
Subscriptions
Plans
Features
PlanFeatures
Licenses
Devices
```

Possible structure:

```text
Restaurants
-----------
Id
Name
Status
CreatedAt
UpdatedAt
```

```text
Plans
-----
Id
Name
Description
CreatedAt
UpdatedAt
```

```text
Features
--------
Id
Key
Name
Description
CreatedAt
UpdatedAt
```

```text
PlanFeatures
------------
PlanId
FeatureId
```

```text
Subscriptions
-------------
Id
RestaurantId
PlanId
Status
StartsAt
ExpiresAt
CreatedAt
UpdatedAt
```

```text
Licenses
--------
Id
RestaurantId
SubscriptionId
LicenseKey
MaxDevices
Status
CreatedAt
UpdatedAt
```

```text
Devices
-------
Id
LicenseId
RestaurantId
InstallationId
MachineFingerprint
Status
ActivatedAt
LastSeenAt
DeactivatedAt
CreatedAt
UpdatedAt
```

Use the project's existing database conventions and naming standards.

---

# 16. Signed Activation Token

After successful activation, the License Server should return a signed activation token.

Conceptually:

```json
{
  "licenseId": "LIC-123",
  "restaurantId": "REST-55",
  "installationId": "ABC",
  "expiresAt": "2026-09-15T23:59:59",
  "features": [
    "product_discount",
    "multiple_units"
  ],
  "signature": "..."
}
```

The License Server signs the token using a private signing key.

```text
License Server
    │
    └── Private Key
```

The POS contains only the corresponding public key.

```text
POS
    │
    └── Public Key
```

The POS uses the public key to verify that the token was signed by the License Server.

---

# 17. Private Key Security

The private signing key MUST NOT be included in:

* POS executable
* POS DLLs
* Frontend
* Configuration files distributed to customers
* Git repository
* Client-side environment variables

The private key belongs only to the License Server infrastructure.

The POS should only contain the public verification key.

---

# 18. Local License Cache

The POS should store the most recent valid activation locally.

Example:

```text
%ProgramData%\CompanyName\POS\license.json
```

Possible contents:

```json
{
  "activationId": "ACT-123",
  "token": "SIGNED_TOKEN",
  "lastValidatedAt": "2026-08-15T10:00:00"
}
```

The local license cache is used to support offline operation.

It is NOT the source of truth.

---

# 19. Startup Validation

When POS starts:

```text
Read local activation
        ↓
Verify token signature
        ↓
Check token expiration
        ↓
Check Installation ID
        ↓
If online:
    Contact License Server
        ↓
    Validate current status
        ↓
Update local activation
        ↓
Start POS
```

If offline:

```text
Read local activation
        ↓
Verify signature
        ↓
Check offline grace period
        ↓
If allowed:
    Start POS
```

---

# 20. Offline Grace Period

The POS should support a limited offline period.

Example:

```text
Last successful validation:
2026-08-15

Offline grace period:
7 days
```

The POS can continue operating during the grace period.

After the grace period:

```text
Internet connection required
```

The exact grace period should be configurable based on the product's business requirements.

Do not make the POS completely dependent on constant internet access unless that is explicitly required.

---

# 21. Periodic Server Validation

When internet connectivity exists, the POS should periodically validate the license.

Possible flow:

```text
POS
 ↓
POST /api/license/validate
 ↓
License Server
 ↓
Validate:
    License
    Subscription
    Device
    Restaurant
 ↓
Return current status
```

The validation interval should be configurable.

---

# 22. Suggested License API

Initial API surface:

```http
POST /api/license/activate
POST /api/license/validate
POST /api/license/deactivate
GET  /api/license/status
```

Potential administration APIs:

```http
GET  /api/licenses/{id}
GET  /api/licenses/{id}/devices
POST /api/licenses/{id}/devices/{deviceId}/deactivate
```

Exact endpoints should follow the existing API conventions.

---

# 23. Activation Response

Example:

```json
{
  "success": true,
  "activationId": "ACT-123",
  "licenseId": "LIC-456",
  "restaurantId": "REST-10",
  "expiresAt": "2026-09-15T23:59:59",
  "offlineUntil": "2026-09-22T23:59:59",
  "features": [
    "product_discount",
    "invoice_discount",
    "multiple_units"
  ],
  "token": "SIGNED_TOKEN"
}
```

The exact response should be designed according to the project's security requirements.

---

# 24. Device Deactivation

The system should support deactivating a device.

Example:

```text
Restaurant
    ↓
License Settings
    ↓
Activated Devices
```

Example UI:

| Device | Activated | Last Seen | Status | Action     |
| ------ | --------- | --------- | ------ | ---------- |
| POS-01 | Aug 1     | Aug 15    | Active | Deactivate |
| POS-02 | Aug 5     | Aug 15    | Active | Deactivate |
| POS-03 | Aug 7     | Aug 14    | Active | Deactivate |

After deactivation:

```text
Device
    ↓
Status = Deactivated
```

The device should no longer be able to renew/validate its activation.

---

# 25. Copying the POS Folder

The licensing system must prevent simple copy/paste activation.

Scenario:

```text
PC A
    InstallationId = ABC
    MachineFingerprint = MACHINE-A
```

The user copies the POS folder to:

```text
PC B
```

The copied installation still contains:

```text
InstallationId = ABC
```

but the new machine has:

```text
MachineFingerprint = MACHINE-B
```

The License Server detects:

```text
ABC → MACHINE-A

Incoming:
ABC → MACHINE-B
```

and rejects or requires a new authorized activation.

The exact behavior should be determined by the device management policy.

---

# 26. Reinstallation

If the user uninstalls and reinstalls the POS:

```text
Old Installation ID
        ↓
New Installation ID
```

The system should not automatically assume this is a completely new device.

The server may use:

```text
Machine Fingerprint
+
Previous Device Record
```

to identify a reinstall.

Possible policies:

### Policy A — Automatic Reclaim

If the same machine is detected:

```text
Old Device
    ↓
Same machine
    ↓
Re-activate installation
```

### Policy B — Manual Re-activation

Require the restaurant administrator to deactivate the old device before activating the new installation.

The chosen policy should be based on the business requirements.

---

# 27. Security Model

The licensing system follows:

```text
Server-side source of truth
+
Signed activation token
+
Device registration
+
Machine fingerprint
+
Installation ID
+
Periodic validation
+
Offline grace period
```

No single mechanism should be treated as sufficient protection.

---

# 28. Reverse Engineering Considerations

The POS is a client application and therefore should be considered potentially inspectable by the customer.

The system must NOT rely on hiding secrets inside the POS.

Assume that a technically skilled user may:

* Inspect the executable.
* Decompile .NET assemblies.
* Inspect configuration.
* Analyze API calls.
* Modify local files.
* Patch local validation logic.

Therefore:

```text
Do NOT put secrets in the client.
```

The client should not contain:

```text
Private signing key
Database credentials
License master secret
Server administrative credentials
```

Critical authorization decisions must remain server-side.

---

# 29. Defense in Depth

Do not rely on a single:

```text
IsLicenseValid()
```

check.

Use multiple layers:

```text
Application Startup
        ↓
Local Token Validation

Online
        ↓
License Server Validation

Important API Operation
        ↓
Backend License/Subscription Validation

Periodic
        ↓
Server Revalidation
```

The goal is not to make reverse engineering impossible.

The goal is to make simple unauthorized copying ineffective and ensure that the License Server remains the source of truth.

---

# 30. Relationship With RBAC

Licensing and RBAC must remain separate.

Correct flow:

```text
License
    ↓
Subscription Active?
    ↓
Feature Enabled?
    ↓
User Authentication
    ↓
Role
    ↓
Permission
    ↓
Authorization
```

Example:

```text
Restaurant
    │
    ├── Subscription: Professional
    │
    ├── Feature:
    │      product_discount ✓
    │
    └── User:
           Manager
              │
              └── Permission:
                    discounts.product ✓
```

The user can use Product Discount only if:

```text
Subscription Active
AND
Feature Enabled
AND
Permission Granted
```

---

# 31. Frontend Usage

The frontend should expose helpers such as:

```ts
hasFeature(feature)
hasPermission(permission)
isLicenseValid()
```

Example:

```ts
const canUseProductDiscount =
    isLicenseValid() &&
    hasFeature(FEATURES.PRODUCT_DISCOUNT) &&
    hasPermission(PERMISSIONS.PRODUCT_DISCOUNT);
```

Then:

```tsx
{canUseProductDiscount && (
    <ProductDiscountButton />
)}
```

The frontend behavior is for UX.

The backend remains the security boundary.

---

# 32. License Statuses

Recommended initial statuses:

```text
Active
Expired
Suspended
Cancelled
```

Device statuses:

```text
Active
Deactivated
Blocked
```

Subscription statuses:

```text
Trial
Active
Expired
Suspended
Cancelled
```

Use only the statuses required by the actual business requirements.

---

# 33. Failure Scenarios

The system must handle:

### License expired

```text
License Server
    ↓
Expired
    ↓
POS should not continue normal operation
```

### Subscription expired

```text
Subscription
    ↓
Expired
    ↓
License validation fails
```

### Device limit exceeded

```text
3 / 3 devices
    ↓
New activation
    ↓
Reject
```

### Device deactivated

```text
Device
    ↓
Deactivated
    ↓
Validation fails
```

### Internet unavailable

```text
Offline
    ↓
Within grace period
    ↓
Allow
```

After grace period:

```text
Offline
    ↓
Grace period expired
    ↓
Require validation
```

### Server unavailable

The POS should distinguish between:

```text
License is known to be invalid
```

and:

```text
Server temporarily unavailable
```

Do not immediately invalidate a valid license because of a temporary network/server failure.

---

# 34. Recommended Initial Scope

Do not over-engineer the first version.

Initial implementation should contain:

```text
License
Subscription
Plan
Features
Devices
Activation
Validation
Deactivation
Signed Token
Offline Grace Period
```

Avoid initially implementing:

```text
Complex DRM
Hardware dongles
Advanced obfuscation
Custom licensing DSL
Complex anti-debugging
```

unless the product requirements justify them.

---

# 35. Final Architecture

```text
                    LICENSE SERVER
                         │
          ┌──────────────┼──────────────┐
          │              │              │
          ▼              ▼              ▼
    Subscription      License        Devices
          │              │              │
          ▼              ▼              │
         Plan       Max Devices         │
          │                             │
          ▼                             │
       Features                         │
          │                             │
          └──────────────┬──────────────┘
                         │
                       HTTPS
                         │
                         ▼
                       POS
                         │
              ┌──────────┴──────────┐
              │                     │
      Installation ID       Machine Fingerprint
              │                     │
              └──────────┬──────────┘
                         │
                         ▼
                 Signed Activation
                         │
                         ▼
                  Offline Cache
```

---

# 36. Relationship With POS Feature Documentation

The following licensing-related capabilities should also be represented in `docs/FEATURES.md` where appropriate:

```text
license_activation
license_validation
device_activation
device_management
offline_license_mode
subscription_validation
```

However, distinguish between:

### Core Platform Capabilities

```text
license_activation
license_validation
device_activation
subscription_validation
```

and:

### Commercial POS Features

```text
product_discount
invoice_discount
multiple_units
multiple_barcodes
wholesale_price
```

Licensing capabilities are part of the platform infrastructure and should not be treated exactly like customer-facing POS features.

---

# 37. Documentation Maintenance

Whenever licensing behavior changes:

1. Update this document.
2. Update `FEATURES.md` if a customer-facing licensing feature changes.
3. Update API documentation.
4. Update database documentation.
5. Update migration/seed documentation.
6. Update frontend behavior documentation.

The License Server and POS must remain consistent with this document.
