# AI Usage 



* Antigravity CLI paired with Gemini 3.8 Flash was used as the interactive agentic pair programmer for code generation


* `GEMINI.md`: Wrote clear, project wide guidelines that require a 3 layer code structure (controllers, services, and repositories), block controllers from talking directly to the database (`AppDbContext`), require safe, unchangeable data models (record DTOs) for responses, and ensure stock levels can never drop below zero.

* **`.agents/agents/`**: Configured specialized subagent profiles to narrow AI context and responsibilities:

  * system-architect.md = Focuses on designing database tables, organizing data cleanly, setting up fast search indexes, and making sure seed data can be run safely multiple times without duplicating.

  * `backend-engineer.md` = Focuses on building the 3 layer backend code, making sure stock updates happen all-at-once (or not at all), and writing the web API endpoints (Controllers).

  * `frontend-engineer.md`: Specialized in React 19, TypeScript, Tailwind CSS, and UX edge cases.

  * `invariant-auditor.md`: Focuses on double checking that core stock rules are never broken and writing tests for edge cases.
  
* **`.agents/rules/`**: Created enforceable markdown rules:

  * `architecture.md`: Defined layer boundaries (Controllers ➔ Services ➔ Repositories) and dependency injection standards.

  * `invariants.md`: Sets the unbreakable business rules: stock can never drop below zero, dispatching a transfer must succeed completely or fail completely, and repeating the same request shouldn't accidentally change the status or stock twice.

* **`.agents/skills/`**: Created custom skill routines:

  * `efcore-migrations`: Step-by-step procedure for generating, verifying, and applying SQL Server migrations safely.

  * `stock-invariant-auditor`: A guide for checking the code for timing bugs (when two requests happen at the exact same time), accidental double clicks.

---

## 3. Manual Steps

* Hand-crafted the project instructions file (`GEMINI.md`), agent definitions (`system-architect.md`, `backend-engineer.md`, `frontend-engineer.md`, `invariant-auditor.md`), and constraint documents (`architecture.md`, `invariants.md`) to establish firm engineering guardrails before issuing any implementation prompts.

* Manually added the necessary Entity Framework Core SQL Server NuGet packages (`Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design`) to `src/MihiraStores.Api.csproj` to anchor the database engine before generating migrations.

* Manually configured `appsettings.json` and `appsettings.Development.json` with the local SQL Server LocalDB connection string (`Server=(localdb)\\mssqllocaldb;Database=MihiraStoresDb;Trusted_Connection=True;...`) rather than relying on AI guesswork for server instances.

* Created the initial database migration entirely  from the CLI using `dotnet ef migrations add InitialInventorySchema --project src/MihiraStores.Api`, reviewed the generated migration file to ensure that tables, foreign keys, and the unique composite index `X_Stocks_BranchId_ProductId` were configured accurately, and applied the migration to the local SQL Server database using `dotnet ef database update --project src/MihiraStores.Api`.

* After confirming that the initial database schema and migration compiled and ran cleanly against SQL Server, manually committed the baseline schema to version control.

* Manually integrated Swashbuckle in `Program.cs` and enabled Swagger UI at `/swagger` to enable hands-on manual inspection of request/response payloads and status codes in the browser.

---

## 4. AI Prompts and Implementation Steps

### Step 1: Minimum Domain Model & Idempotent Seeding
* **Prompt Used**:

```
@system-architect.md @architecture.md @invariants.md

Design and implement the minimum domain model for the required stock-transfer workflow.

Entities:
1. Branch: Id, Name, Code, IsWarehouse.
2. Product: Id, Name, SKU.
3. Stock: Id, BranchId, ProductId, Quantity, LastUpdatedUtc.
4. StockTransfer: Id, FromBranchId, ToBranchId, Status, CreatedAtUtc,
DispatchedAtUtc (nullable), ReceivedAtUtc (nullable), CancelledAtUtc (nullable),
and a collection of lines.
5. StockTransferLine: Id, StockTransferId, ProductId, Quantity.

Use clear enum values: Requested, Dispatched, Received, Cancelled.
Configure foreign keys and sensible delete behaviour. Configure a unique index on
(BranchId, ProductId). Use integer quantities and validate that they are positive.
Use DTOs for API requests and responses.

Seed four locations: Central Warehouse, Colombo Branch, Kandy Branch, and Galle Branch.
Seed five products and starting stock for all required locations.

Make the seed operation idempotent so restarting the application does not create duplicates.
Use the project's configured SQL Server.
Build the solution and inspect the generated model.
```


* **Review & Actions**:
  * Checked that database models properly linked related tables together and blocked accidental deletions of connected records (like preventing a branch or product from being deleted if it's already used in a transfer).
  * Checked that DataSeeder looks up existing branches by their code and products by their SKU before adding them, so restarting the app never creates duplicate items or messes up stock numbers.


### Step 2: Stock Transfer Business Service
* **Prompt Used**:

```
@backend-engineer.md @architecture.md @invariants.md

Implement the stock transfer business service and request/response DTOs.

Required operations:
- CreateTransferAsync
- GetTransferByIdAsync
- GetTransfersAsync with optional branchId and status filters
- GetBranchStockAsync
- DispatchTransferAsync
- ReceiveTransferAsync
- CancelTransferAsync

Create:
- Reject identical source and destination branches.
- Reject an empty list of items.
- Reject quantities <= 0.
- Check that the branches and products actually exist in the system.
- Set the status of all new transfers to Requested by default.

Dispatch:
- Only allow transfers that are currently in Requested status.
- Check the sending branch's stock for every single item before making any changes.
- If even one item doesn't have enough stock, cancel the whole process without changing any stock amounts or the transfer status.
- Subtract the stock for all items and update the status to Dispatched together in a single step.
- Save the exact time the transfer was dispatched.
- Make sure the same transfer cannot be dispatched twice at the same time so stock is never subtracted more than once.

Receive:
- Only allow transfers that are currently in Dispatched status.
- Add the requested item amounts to the receiving branch's stock just once.
- Change the status to Received and save the exact time it was received together in a single step.
- Make sure the same transfer cannot be marked as received twice at the same time so stock is never added more than once.

Cancel:
- Only allow transfers that are currently in Requested status.
- Change the status to Cancelled without making any changes to the stock.
- Save the exact time the transfer was cancelled.

Use the right database tools to safely handle multiple requests happening at the exact same time. Simply checking the status in code before saving changes is not enough to stop two identical requests from running together. You should use row versions, concurrency tokens, conditional updates, or proper database locks depending on what your database supports. Finally, make sure that if anything goes wrong during the process, all changes are completely undone.

Return clear error messages when business rules or checks fail so the controllers can easily turn them into proper HTTP responses.Once you finish writing the code, build the project and run the tests to make sure everything works.
```

* **Review & Actions**:
  * Checked the dispatch code to make sure that if even one item didn't have enough stock, an error was thrown right away and all database changes were completely undone (rolled back).
  * Confirmed that the timestamp (LastUpdatedUtc) used to detect simultaneous edits was properly updated whenever stock levels changed.

### Step 3: Thin API Controllers
* **Prompt Used**:
```
@backend-engineer.md @architecture.md

Implement thin API controllers using the service layer and DTOs.

Provide endpoints consistent with these routes:
- GET /api/branches
- GET /api/products
- GET /api/branches/{id}/stock
- POST /api/transfers
- GET /api/transfers?branchId=&status=
- GET /api/transfers/{id}
- POST /api/transfers/{id}/dispatch
- POST /api/transfers/{id}/receive
- POST /api/transfers/{id}/cancel

Use appropriate response codes:
- 200 for successful reads/actions.
- 201 Created for a successfully created transfer.
- 400 Bad Request for invalid request data.
- 404 Not Found for unknown branch, product, or transfer.
- 409 Conflict where a transfer's current status or concurrency state prevents the action.

Use a consistent error format (like ProblemDetails) with clear, easy-to-read messages. Never show raw database models (EF Core entities), system crash details (stack traces), or internal database errors in the response.

Run dotnet build and dotnet test. Update the README with the actual route contract.

```

* **Review & Actions**:
  * Verified that zero database contexts or LINQ queries were present inside controllers.
  * Confirmed that controllers accepted and returned record DTOs exclusively.

### Step 4: Automated xUnit Tests for Business Rules
* **Prompt Used**:

```
@invariant-auditor.md @stock-invariant-auditor

Create and run automated xUnit tests for the required business rules.

At minimum, include:
1. Dispatch_WhenQuantityExceedsStock_LeavesStockAndStatusUnchanged.
2. Dispatch_WhenRepeated_DoesNotDeductStockTwice.
3. Receive_AfterDispatch_AddsStockToDestination.
4. Receive_WhenRepeated_DoesNotAddStockTwice.
5. Receive_WhenTransferIsRequested_IsRejected.
6. Cancel_WhenTransferIsDispatched_IsRejected.
7. CreateTransfer_WhenBranchesAreSame_IsRejected.
8. CreateTransfer_WhenQuantityIsZeroOrNegative_IsRejected.
9. Dispatch_WhenOneOfSeveralLinesHasInsufficientStock_RollsBackAllLines.

Use a real relational database for your tests whenever possible. If you use an in-memory SQLite database, make sure the connection stays open for the entire test and verify that it behaves the same way as your actual production database. Do not rely on EF Core's basic InMemory provider, as it cannot properly test real database transactions, rules, or simultaneous requests.

For every test, check both the returned result (or error) and the actual data saved in the database to make sure everything updated properly. Run dotnet test and share the real test output.
```

* **Review & Actions**:
  * Executed `dotnet test` and confirmed all 25 tests passed.
  * Verified that tests asserted both method return values and direct database state after transactions completed.

### Step 5: Frontend React
* **Prompt Used**:
```
@frontend-engineer.md

Build one React + TypeScript with Tailwind CSS screen in src/MihiraStores.Web.

Required UI:
1. Fetch the list of branches and products from the backend and show them on the screen.
2. Let the user pick a sending branch and a different receiving branch.
3. Show how much stock is currently available at the chosen sending branch.
4. Let the user add one or more items with their amounts to the transfer.
5. Check that there is at least one item in the transfer and that all amounts are 1 or higher.
6. Send the new transfer request to the backend.
7. Show a list of all transfers along with their current status.
8. Only show the Dispatch and Cancel buttons for transfers in Requested status.
9. Only show the Receive button for transfers in Dispatched status.
10. Automatically reload the transfer list and stock numbers whenever an action succeeds.
11. Show clear, easy to read messages when data is loading, when an action succeeds, when a list is empty, or when an error happens.
12. Disable buttons while a request is still loading so users cannot accidentally click them twice (while keeping in mind that the backend still does the real safety checks).

Follow the exact API URLs and formats listed in README.md. Keep the design clean, simple, and easy for everyone to use.
Run the frontend build and fix any errors that come up. If the backend API is down or unreachable, show a clear error message to the user instead of hiding the problem with fake test data.
```

* **Review & Actions**:
  * Validated UI design against backend REST contract.
  * Tested user flows: creating transfers, verifying stock updates, and testing error banners when API calls failed.

### Step 5: Final Testing
 ```
 `@invariant-auditor.md` `@stock-invariant-auditor`

Do a final check of the entire project to make sure everything works properly.

1. Build the backend project to make sure there are no code errors.
2. Run all the automated tests and make sure they pass.
3. Build the frontend for production to make sure there are no TypeScript or bundling errors.
4. Double-check that every required business and stock rule is properly followed.
5. Confirm that the main application is connected to a real SQL Server or PostgreSQL database.
6. Check that the starter data (seed data) includes the main warehouse, three branches, five products, and their starting stock amounts.
7. Test the full user interface flow from start to finish (creating, dispatching, receiving, and cancelling transfers).
8. Check that the system safely blocks duplicate clicks and handles simultaneous requests without messing up stock numbers.
9. Point out any remaining bugs, missing features, or limitations honestly—do not hide any issues.
 ```
---

## 5. Issues Encountered

1. Problem: The AI initially imported TypeScript data types like regular JavaScript values (e.g., import { Branch, Product } from './types'). Because the project uses strict TypeScript settings (verbatimModuleSyntax), the build failed with an error (TS1484) saying types must be imported explicitly.
**Correction: I checked all frontend files (App.tsx, CreateTransferForm.tsx, TransfersTable.tsx, BranchStockView.tsx, api.ts) and updated the imports to use import type { ... }, which fixed all build errors.**

2. Problem: In App.tsx, the initial AI code ran a data-loading function inside useEffect that immediately cleared the error state (setBackendError(null)). This triggered a React 19 ESLint warning (react-hooks/set-state-in-effect) because updating state immediately inside an effect causes extra, unnecessary screen re-renders.
**Correction: I separated the initial page load data fetch from the manual refresh button action. Now, the app waits for the API call to finish before updating the state, stopping the extra re-renders.**

3. Problem: AI tools often default to EF Core's basic InMemory database when writing tests. However, InMemory does not support real database transactions (BeginTransactionAsync), table link rules (foreign keys), or unique stock rules, which means tests can pass even when the real database would fail.
**Correction: I switched the tests to use an in memory SQLite database (Microsoft.EntityFrameworkCore.Sqlite) with a database connection kept open for the duration of each test. This made sure that all-or-nothing transactions, rollbacks, and unique stock rules were tested accurately.**

---

## 6. Architectural Decisions

* Set up the system to check that the sending branch has enough stock for every single item before subtracting anything. If even one item doesn't have enough stock, the whole transfer is stopped with a clear error message so stock is never left half updated.
* Made sure that repeating a dispatch or receive request doesn't crash or double-count stock. If a request is sent twice (for example, during a slow internet retry), the system sees that the transfer is already in that status and safely returns the current transfer details without changing the stock a second time.
* Disabled buttons and showed loading spinners on the screen while requests are running, while also using database transactions and simultaneous edit checks on the backend for full protection.
* Kept each part of the code strictly separate so database connections stay inside the repositories and raw database models never leave the services. This keeps the API responses and frontend completely independent from how the database tables are built.


