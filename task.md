# POS Clean Architecture Refactoring Task

## Objective

Refactor the existing POS backend into a cleaner, maintainable architecture inspired by Clean Architecture principles.

The main goal is **not** to rewrite the application.

The goal is to:

1. Preserve all existing functionality and business behavior.
2. Separate business rules from ASP.NET Web API, EF6, SQLite, printing, filesystem, and other infrastructure concerns.
3. Make adding future POS features easier and safer.
4. Reduce coupling to `.NET Framework 4.8`, ASP.NET Web API 2, EF6, and SQLite.
5. Make a future migration to ASP.NET Core / .NET 9 significantly easier.
6. Keep the current application working after every refactoring step.

---

# 1. Important Constraints

## DO NOT

* Do not rewrite the entire project.
* Do not migrate from .NET Framework 4.8 to .NET 9 now.
* Do not migrate from EF6 to EF Core now.
* Do not replace SQLite now.
* Do not introduce unnecessary frameworks.
* Do not change existing business behavior.
* Do not modify database schema unless absolutely required.
* Do not change API contracts unless required and explicitly documented.
* Do not blindly move every existing class into a different folder.
* Do not create abstractions that have no real architectural value.
* Do not introduce a generic repository/unit-of-work abstraction everywhere just for the sake of Clean Architecture.

## DO

* Inspect the existing project before making changes.
* Understand the current architecture and dependencies.
* Refactor incrementally.
* Keep the application buildable after each logical step.
* Preserve existing endpoints and behavior.
* Prefer small, focused changes.
* Use dependency inversion where it provides real value.
* Keep the Domain independent from frameworks and infrastructure.

---

# 2. Target Architecture

The target structure should conceptually be:

```text
Pos
│
├── Domain
│   ├── Entities
│   ├── ValueObjects
│   ├── Enums
│   ├── Exceptions
│   └── Rules
│
├── Application
│   ├── Interfaces
│   ├── Services
│   ├── UseCases
│   ├── DTOs
│   └── Validators
│
├── Infrastructure
│   ├── Persistence
│   │   ├── EF
│   │   ├── Dapper
│   │   └── SQLite
│   ├── Repositories
│   ├── Printing
│   ├── Logging
│   └── System
│
└── Presentation
    ├── Controllers
    ├── Requests
    └── Responses
```

The exact folder/project structure may be adapted to the existing codebase.

Do not create unnecessary projects if the current project structure makes that impractical.

The architectural boundaries are more important than the exact folder names.

---

# 3. Dependency Direction

The dependency direction must be:

```text
Presentation
      ↓
Application
      ↓
Domain

Infrastructure
      ↓
Application
      ↓
Domain
```

Infrastructure must implement interfaces defined by Application or Domain.

The Domain must NOT depend on:

* ASP.NET
* Web API
* Entity Framework
* SQLite
* Dapper
* System.Web
* HttpContext
* Windows printer APIs
* filesystem implementations
* logging frameworks

Application should also avoid depending directly on infrastructure implementations.

For example:

```text
BAD:

Application
   ↓
SqliteProductRepository
```

Instead:

```text
Application
   ↓
IProductRepository

Infrastructure
   ↓
SqliteProductRepository
```

---

# 4. Domain Layer

The Domain represents the actual POS business.

Identify and move/refactor business concepts such as:

* Product
* Category
* Order
* OrderLine
* Customer
* Payment
* Discount
* Unit
* UnitConversion
* Barcode
* Shift
* Table
* Delivery
* Takeaway
* Pricing
* Receipt-related business concepts where appropriate

Do not blindly move database models into Domain.

Determine whether each existing model is:

* Domain Entity
* Database Entity
* DTO
* Request Model
* Response Model
* Infrastructure model

These should not automatically be the same class.

---

# 5. Domain Business Rules

Business rules must live in Domain/Application rather than Controllers.

Examples from the POS:

```text
Quantity must be greater than zero.

Line discount cannot exceed line subtotal.

Order total must be calculated consistently.

A product unit conversion must be valid.

A barcode must identify a valid product/unit.

A table can only have one active invoice if this is an existing business rule.

A completed invoice cannot be modified if this is an existing business rule.

Payment amount must satisfy the existing payment rules.

Shift/day calculations must respect the configured business day.

Retail/wholesale pricing must follow the existing pricing rules.
```

Do not invent new business rules.

Only extract rules that already exist in the application/documentation.

If a rule is unclear, preserve current behavior and document the ambiguity instead of guessing.

---

# 6. Application Layer

Application represents POS use cases.

Examples:

```text
CreateOrder
AddProductToOrder
RemoveOrderItem
UpdateOrderItemQuantity
ApplyLineDiscount
UpdateUnitPrice
ApplyCustomerDiscount
CalculateOrderTotal
CompleteOrder
ProcessPayment
HoldOrder
ResumeOrder
CloseShift
GenerateDailyReport
PrintReceipt
```

These are examples.

First inspect the existing code and identify the actual use cases.

Application services should orchestrate operations.

They should not contain:

```text
SQL queries
EF DbContext logic
SQLite connection logic
Windows printer implementation
HTTP-specific code
```

---

# 7. Interfaces

Create interfaces only where they protect the application/domain from infrastructure.

Examples:

```csharp
public interface IProductRepository
{
    Product GetById(int id);
}
```

```csharp
public interface IOrderRepository
{
    Order GetById(int id);
    void Save(Order order);
}
```

```csharp
public interface IReceiptPrinter
{
    void Print(Receipt receipt);
}
```

```csharp
public interface IClock
{
    DateTime Now { get; }
}
```

Only create an interface when there is a meaningful boundary.

Avoid creating interfaces for every class automatically.

---

# 8. Infrastructure

Infrastructure contains implementation details.

Current infrastructure may include:

```text
EF6
SQLite
Dapper
Winspool / printer APIs
Serilog
Filesystem
Configuration
External services
```

Move infrastructure-specific logic here.

For example:

```text
Infrastructure
    Persistence
        PosDbContext
        EF configurations

    Repositories
        ProductRepository
        OrderRepository
        CustomerRepository

    Printing
        WindowsPrinter
        ReceiptPrinter

    Logging
        ...
```

The rest of the application should depend on abstractions rather than these implementations.

---

# 9. Presentation

Controllers should become thin.

A controller should primarily:

1. Receive HTTP request.
2. Validate request shape.
3. Map request to application input.
4. Execute application use case.
5. Map result to HTTP response.

Avoid putting business rules inside controllers.

BAD:

```csharp
if (discount > product.Price * quantity)
{
    ...
}

db.Products...
db.Orders...
```

Better:

```csharp
var result = _applyLineDiscount.Execute(command);

return Ok(result);
```

---

# 10. DTO Separation

Do not expose EF/database entities directly as API contracts when that creates coupling.

Separate:

```text
Request DTO
Application Command
Domain Entity
Response DTO
Database Entity
```

when the distinction is meaningful.

Do not create unnecessary mapping layers for trivial cases.

---

# 11. Database Compatibility

The current database is SQLite.

Keep it.

The refactoring must not require a database migration unless absolutely necessary.

Database-specific concerns should remain in Infrastructure.

The Domain must not contain:

```csharp
[Table(...)]
[Column(...)]
```

or similar persistence-specific concerns unless the existing architecture makes a specific exception necessary.

Prefer keeping persistence configuration in Infrastructure.

---

# 12. Future Migration Goal

The architecture must make this future transition easier:

## Current

```text
.NET Framework 4.8
ASP.NET Web API 2
EF6
SQLite
```

## Future

```text
.NET 9
ASP.NET Core
EF Core
PostgreSQL / SQL Server
```

The goal is that the following areas require minimal or no business-logic changes:

```text
Domain
Business Rules
Application Use Cases
```

The main migration work should be concentrated around:

```text
Presentation
Infrastructure
Persistence
Hosting
Authentication
Configuration
```

Do not implement the future migration now.

---

# 13. Existing POS Features

Before changing architecture, identify the existing functionality.

Pay special attention to:

* Products
* Categories
* Multiple Barcodes
* Units
* Unit Conversion
* Pack Quantity
* Pack Barcode
* Retail Price
* Wholesale Price
* Line Discount
* Edit Unit Price
* Orders
* Tables
* Takeaway
* Delivery
* Customers
* Payments
* Discounts
* Service Tax
* Hold Invoice
* Receipt Printing
* Daily Reports
* Shifts
* Audit Log
* Licensing

Do not break any of these.

---

# 14. Refactoring Strategy

Follow this sequence.

## Step 1 — Analyze

First inspect the entire backend.

Identify:

* Controllers
* Models
* Services
* Database access
* EF6
* Dapper
* SQLite
* Printer code
* Business rules
* Authentication
* Logging
* Configuration
* Existing dependencies

Create an architecture/dependency map before modifying code.

---

## Step 2 — Identify Coupling

Find cases such as:

```text
Controller → DbContext
Controller → SQL
Controller → Business Logic
Service → HttpContext
Domain → EF
Domain → SQLite
Business Logic → Printer API
Application → concrete infrastructure class
```

Document them.

---

## Step 3 — Extract Domain

Move business rules into Domain entities/services where appropriate.

Do not change behavior.

---

## Step 4 — Extract Application Use Cases

Move orchestration logic into Application services/use cases.

---

## Step 5 — Introduce Interfaces

Introduce abstractions only at real architectural boundaries.

---

## Step 6 — Move Implementations

Move:

```text
EF6
SQLite
Dapper
Printer
Filesystem
Logging
```

into Infrastructure.

---

## Step 7 — Thin Controllers

Controllers should become transport adapters rather than business logic containers.

---

## Step 8 — Validate

After each major refactoring step:

* Build the solution.
* Run the existing application.
* Verify existing endpoints.
* Verify database operations.
* Verify order calculations.
* Verify discounts.
* Verify pricing.
* Verify unit conversion.
* Verify printing.
* Verify reports.
* Verify authentication.
* Verify licensing if already implemented.

---

# 15. Important Architectural Rule

Do not confuse Clean Architecture with:

```text
More folders
More interfaces
More abstractions
More classes
```

The objective is:

```text
Low Coupling
+
High Cohesion
+
Clear Dependencies
+
Protected Business Rules
+
Replaceable Infrastructure
```

Every abstraction should have a reason.

---

# 16. Before/After Requirement

For every significant refactoring, explain:

```text
BEFORE:
What was coupled?

AFTER:
What is separated?

WHY:
How does this improve maintainability?

FUTURE:
How does this help migration to ASP.NET Core / .NET 9?
```

---

# 17. Do Not Over-Engineer

This is a real POS application, not an architecture demonstration.

Avoid unnecessary:

* CQRS
* MediatR
* Event Sourcing
* Generic Repository
* Unit of Work abstraction over everything
* excessive factories
* excessive interfaces
* unnecessary design patterns

Use simple C# where simple C# is enough.

Clean Architecture principles are more important than following a specific template.

---

# 18. Final Deliverables

After completing the refactoring, provide:

### 1. Architecture Summary

Explain the resulting architecture.

### 2. Dependency Map

Show:

```text
Presentation
    ↓
Application
    ↓
Domain

Infrastructure
    ↓
Application / Domain
```

### 3. Changed Files

List important files created, moved, or modified.

### 4. Remaining Technical Debt

Clearly identify what is still coupled to:

```text
.NET Framework 4.8
ASP.NET Web API 2
EF6
SQLite
Windows
```

### 5. Migration Readiness

Explain what would need to change when migrating to:

```text
.NET 9
ASP.NET Core
EF Core
```

### 6. Feature Development Guide

Provide a short example showing where a future feature should be implemented.

For example:

```text
Feature: Multiple Units

Domain:
    Unit
    UnitConversion

Application:
    AddProductUnit
    ConvertQuantity

Infrastructure:
    UnitRepository
    EF mapping

Presentation:
    UnitsController
```

---

# Success Criteria

The refactoring is successful if:

* Existing functionality still works.
* Business rules are not hidden inside controllers.
* Domain does not depend on ASP.NET/EF/SQLite.
* Application does not depend on concrete infrastructure implementations.
* Infrastructure contains database/printer/system-specific code.
* Controllers are thin.
* New features can be added without touching unrelated layers.
* The architecture provides a realistic path from `.NET Framework 4.8` to `.NET 9 / ASP.NET Core`.
* The code remains simple and understandable for a small-to-medium POS product.

**Most important rule: preserve behavior first, improve architecture second. Do not perform a large rewrite just to achieve a theoretical Clean Architecture structure.**

# 19. Testing Requirements

Testing is a required part of this refactoring.

The purpose of the tests is to create a reliable safety net before adding more features and before the future migration from `.NET Framework 4.8` to `.NET 9 / ASP.NET Core`.

## Main Goal

After the refactoring, we should be able to run the test suite and verify that the important POS business behavior still works.

The tests should protect against regressions.

For example:

```text
Refactor code
    ↓
Run Tests
    ↓
All tests pass
    ↓
Business behavior is still correct
```

---

# 20. What Should Be Unit Tested?

Prioritize business-critical logic.

At minimum, create unit tests for:

### Orders

* Adding an item.
* Removing an item.
* Updating quantity.
* Calculating subtotal.
* Calculating total.
* Empty order behavior.

### Discounts

* Valid line discount.
* Discount equal to subtotal.
* Discount greater than subtotal.
* Zero discount.
* Negative discount if the business rules prohibit it.

### Pricing

* Retail price.
* Wholesale price.
* Editing unit price.
* Correct price selection.

### Units

* Piece.
* Pack.
* Carton.
* Unit conversion.
* Pack quantity.
* Invalid conversion.

### Multiple Barcodes

* Valid barcode lookup.
* Unknown barcode.
* Multiple barcodes for the same product/unit.
* Barcode associated with the correct unit where applicable.

### Payments

* Valid payment.
* Insufficient payment.
* Exact payment.
* Change calculation.
* Multiple payment methods if supported by the current system.

### Tables / Orders

Test the existing business rules around:

* Active table orders.
* One active invoice per table if this is an existing rule.
* Completing an invoice.
* Preventing invalid modifications after completion if applicable.

### Shifts / Business Day

Test the existing rules around:

* Shift start.
* Shift end.
* Business-day boundaries.
* Cross-midnight shifts.

### Reports

Where practical, test business calculations used by reports:

* Totals.
* Order counts.
* Dine-in totals.
* Takeaway totals.
* Delivery totals.
* Payment totals.

Do not test presentation formatting as a unit test unless it contains meaningful business logic.

---

# 21. Domain Tests Should Be Pure

Domain unit tests should not require:

* SQLite
* EF6
* Database connection
* HTTP
* ASP.NET
* Filesystem
* Physical printer
* Windows APIs

For example:

```csharp
[Test]
public void ApplyDiscount_ShouldRejectDiscountGreaterThanSubtotal()
{
    var line = new OrderLine(
        productId: 1,
        unitPrice: 100,
        quantity: 2
    );

    Assert.Throws<InvalidOperationException>(
        () => line.ApplyDiscount(250)
    );
}
```

This should run without starting the API or connecting to SQLite.

---

# 22. Application Tests

Application use cases should also be tested.

Infrastructure dependencies should be mocked/faked through interfaces.

Example:

```text
AddProductToOrder
       ↓
IProductRepository
IOrderRepository
```

The test should provide fake/mock implementations instead of SQLite.

Example scenario:

```text
Given:
    Product exists
    Order exists

When:
    AddProductToOrder is executed

Then:
    Order contains the product
    Correct quantity is used
    Correct price is used
```

Also test failure cases:

```text
Product does not exist
Order does not exist
Invalid quantity
Invalid business rule
```

---

# 23. Integration Tests

Do not confuse Unit Tests with Integration Tests.

Integration tests may be introduced for important infrastructure behavior such as:

```text
Repository + SQLite
EF6 mappings
Database persistence
API endpoint behavior
```

However, do not create a huge integration-test suite during this refactoring.

Priority should be:

```text
1. Domain Unit Tests
2. Application Unit Tests
3. Critical Integration Tests
```

---

# 24. Test Project Structure

Prefer a separate test project if the current solution structure allows it.

For example:

```text
Pos
│
├── Pos.Domain
├── Pos.Application
├── Pos.Infrastructure
├── Pos.Api
│
└── Pos.Tests
    ├── Domain
    │   ├── OrderTests
    │   ├── OrderLineTests
    │   ├── DiscountTests
    │   ├── PricingTests
    │   └── UnitConversionTests
    │
    └── Application
        ├── AddProductToOrderTests
        ├── CompleteOrderTests
        └── PaymentTests
```

Adapt the structure to the existing solution.

Do not create unnecessary test projects.

---

# 25. Test Naming

Use clear names that describe behavior.

Prefer:

```text
AddItem_WithValidProduct_AddsItemToOrder

ApplyDiscount_WhenDiscountExceedsSubtotal_ThrowsException

ConvertQuantity_WithValidConversion_ReturnsExpectedQuantity

CompleteOrder_WhenOrderIsValid_CompletesOrder
```

Avoid:

```text
Test1
TestOrder
ShouldWork
CheckDiscount
```

---

# 26. Arrange / Act / Assert

Prefer the AAA pattern:

```csharp
// Arrange
var order = CreateOrder();
var product = CreateProduct(price: 100);

// Act
order.AddItem(product, 2);

// Assert
Assert.AreEqual(200, order.Subtotal);
```

Tests should be easy to read.

A developer should understand the business rule by reading the test.

---

# 27. Test Current Behavior Before Changing It

Before changing business logic, inspect the current implementation.

If an existing behavior is unclear:

1. Do not invent a new rule.
2. Capture the current behavior with a test if it is valid.
3. Refactor.
4. Make sure the test still passes.

This is especially important for:

* Discounts
* Pricing
* Unit conversion
* Shift/day calculations
* Payment calculations
* Reports

---

# 28. Tests Are Part of the Architecture

The architecture should make testing easy.

If testing a business rule requires:

```text
Start API
    ↓
Start SQLite
    ↓
Create database
    ↓
Insert data
    ↓
Call HTTP endpoint
```

then the business logic is probably too tightly coupled.

A good target is:

```text
Business Rule
     ↓
Plain C# Test
     ↓
Milliseconds
```

---

# 29. Regression Protection

After the refactoring, the test suite should become the minimum regression safety net for future development.

Whenever a new feature is added:

```text
New Feature
    ↓
Implement
    ↓
Add/Update Tests
    ↓
Run Full Test Suite
    ↓
All Green
```

If a bug is discovered later:

```text
Bug
 ↓
Write failing test
 ↓
Fix bug
 ↓
Test passes
```

This prevents the same bug from silently returning.

---

# 30. Migration Safety

The test suite should also help with the future migration:

```text
.NET Framework 4.8
       ↓
Run Tests
       ↓
All Green

Migrate to .NET 9
       ↓
Run Same Tests
       ↓
All Green
```

The goal is not to guarantee that the migration has zero issues.

The goal is to verify that the **core business behavior remained unchanged**.

---

# 31. Important Testing Rule

Do not chase 100% code coverage.

Prioritize **business-critical behavior** over coverage percentage.

A small suite of meaningful tests is better than hundreds of meaningless tests.

Focus on:

```text
Business Rules
+
Calculations
+
Use Cases
+
Important Edge Cases
```

rather than testing trivial getters/setters.

---

# 32. Final Testing Deliverables

At the end of the refactoring, provide:

### Test Project

A working test project that can be executed independently.

### Test Coverage Summary

Explain which important business areas are covered.

### Commands

Document exactly how to run the tests.

For example:

```text
dotnet test
```

or the appropriate command for the current .NET Framework test setup.

### Remaining Test Gaps

Clearly document areas that are not yet covered.

---

# Testing Success Criteria

The refactoring is considered successful only if:

* The test project builds successfully.
* Tests can run independently.
* Core business rules have meaningful unit tests.
* Application use cases have tests where appropriate.
* Tests do not require a physical printer.
* Domain tests do not require SQLite or EF6.
* Important regression scenarios are covered.
* Existing functionality remains unchanged.
* The test suite can be reused after the future migration to ASP.NET Core / .NET 9.

