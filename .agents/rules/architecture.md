---
trigger: always_on
---

# Rule: 3-Tier Architecture & Dependency Injection

This rule enforces the 3-Tier pattern and dependency injection conventions across the MihiraStores backend.

## Layer Boundaries
1. **Controller Layer (`Controllers/`)**:
   - Thin HTTP adapter.
   - Depends only on Service interfaces (`IStockTransferService`) or read-only Repository interfaces.
   - Never inject `AppDbContext` or use EF Core LINQ operators directly.
   - Never expose raw EF Core entities; always return and accept C# record DTOs.
   - Return status codes: `200 OK`, `201 Created`, `400 Bad Request`, `404 Not Found`.

2. **Service Layer (`Services/`)**:
   - Interfaces should go in `Services\ServiceContracts`
   - Coordinates domain operations and validates business rules.
   - Manages database transaction scopes (`BeginTransactionAsync`).
   - Maps between domain models and DTOs.
   - Independent of HTTP constructs (`HttpContext`, `HttpRequest`).

3. **Repository Layer (`Repositories/`)**:
   - Interfaces should go in `Repositories\RepositoryContracts`
   - Sole owner of `AppDbContext` operations.
   - Exposes asynchronous operations returning domain models or collections.
   - Read queries MUST specify `.AsNoTracking()`.

## Dependency Injection
- Always use constructor injection.
- Store dependencies in `private readonly` fields.
- Register all pairs in `Program.cs` as scoped services.
