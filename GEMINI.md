# Mihira Stores - Agent Instructions & Workspace Rules

The **Mihira Stores** project repository. When operating within this codebase, you must adhere strictly to the architectural constraints, business invariants, and development rules described below.

---

## Technology Stack
- **Backend**: ASP.NET Core Web API (.NET 8)
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Frontend**: React with TypeScript and Vite
- **Testing**: xUnit (unit and integration tests)


1. **Strict 3-Tier Layering Diagram & Hard Prohibitions:**
   - Never inject `AppDbContext` into a Controller.
   - Never place business logic or mathematical calculations inside Controllers.
   - Never expose raw EF Core entity models (`Branch`, `Product`, `StockTransfer`) to the API or Frontend; always use immutable record DTOs.
   - Never perform multi-row inventory mutations without an atomic database transaction (`BeginTransactionAsync()`).
2. **Dependency Injection Standards:**
   - Constructor injection using `private readonly` fields.
     ```csharp
  public class TransfersController : ControllerBase
  {
      private readonly IStockTransferService _service;

      public TransfersController(IStockTransferService service)
      {
          _service = service;
      }
  }
  ```
   - Every service and repository must define a clear interface (`IBranchRepository`, `IBranchService`).
   - Register all pairs with `.AddScoped<TInterface, TImplementation>()`.
3. **The 3 Golden Business Invariants:**
   - Non-Negative Stock (`CurrentStock >= RequestedQuantity`).
   - Operation Idempotency (re-executing `/dispatch` or `/receive` returns current state without re-mutating stock).
   - Strict Transfer State Machine (`Requested` ➔ `Dispatched` ➔ `Received`; cancellation only legal from `Requested`).
4. **EF Core & Database Conventions:**
   - Always `.AsNoTracking()` on read queries.
   - Eagerly load required relationships using `.Include()` and `.ThenInclude()` within the repository layer.
   - Composite unique keys: `HasIndex(s => new { s.BranchId, s.ProductId }).IsUnique()`.
   - Restrict delete behaviors on relational foreign keys.
5. **Quality Verification Gate:**
   - Every modification must be validated by running `dotnet test tests/MihiraStores.Api.Tests`.
