# Mihira Stores

**Mihira Stores** is a multi branch retail inventory and stock transfer management system built on a strict 3 tier enterprise architecture (.NET 10 Web API, Entity Framework Core 10, SQL Server, and React 19 with TypeScript, Vite, and Tailwind CSS v4).

---

## 1. Prerequisites

Before setting up or running the application, ensure the following tooling is installed:

- **.NET SDK**: .NET 10.0 SDK (`dotnet --version` >= 10.0)
- **Node.js**: Node.js v20.x or higher (`node -v`) and npm v10.x or higher (`npm -v`)
- **Database Engine**: Microsoft SQL Server (one of the following):
  - SQL Server 2022 Express / Developer Edition
  - SQL Server LocalDB (`(localdb)\mssqllocaldb`, bundled with Visual Studio)
  - Full SQL Server instance or Docker SQL Server container (`mcr.microsoft.com/mssql/server:2022-latest`)
- **EF Core CLI Tools** (optional, for running migrations manually):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## 2. Connection String Configuration

Database connection settings are located in [`src/MihiraStores.Api/appsettings.json`](src/MihiraStores.Api/appsettings.json) and can be customized per environment in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MihiraStoresDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### Connection String Options
- **SQL Server LocalDB (Default)**:
  ```
  Server=(localdb)\\mssqllocaldb;Database=MihiraStoresDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
  ```
- **SQL Server Express**:
  ```
  Server=.\\SQLEXPRESS;Database=MihiraStoresDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
  ```
- **Standard / Docker SQL Server (SQL Authentication)**:
  ```
  Server=localhost,1433;Database=MihiraStoresDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true
  ```

---

## 3. Database Migration & Startup Seeding

The application automatically applies all pending EF Core migrations and executes an idempotent seed at startup:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DataSeeder.SeedAsync(context);
}
```

### Manual Database Migration Commands
If you prefer applying or managing migrations via CLI:

```bash
# Apply migrations to the configured database
dotnet ef database update --project src/MihiraStores.Api

# Add a new migration (after modifying entity models or AppDbContext)
dotnet ef migrations add <DescriptiveMigrationName> --project src/MihiraStores.Api

# Rollback to a specific migration
dotnet ef database update <PreviousMigrationName> --project src/MihiraStores.Api

# Remove the latest unapplied migration
dotnet ef migrations remove --project src/MihiraStores.Api
```

### Seeded Baseline Data
- **4 Locations**:
  - `Central Warehouse` (`WH`, IsWarehouse = true) - initial stock: **500 units** per product
  - `Colombo Branch` (`CMB`, IsWarehouse = false) - initial stock: **100 units** per product
  - `Kandy Branch` (`KDY`, IsWarehouse = false) - initial stock: **75 units** per product
  - `Galle Branch` (`GLE`, IsWarehouse = false) - initial stock: **50 units** per product
- **5 Catalog Products**:
  - Coconut Milk 400ml (`PROD-COCO-400`)
  - Ceylon Black Tea 500g (`PROD-TEA-500`)
  - Basmati Rice 5kg (`PROD-RICE-5KG`)
  - Red Split Lentils 1kg (`PROD-LENT-1KG`)
  - White Sugar 1kg (`PROD-SUGR-1KG`)
- **20 Stock Records**: Pre-seeded across all 4 branches.
- **Idempotency Guarantee**: Seeding checks for existing branch codes, SKUs, and `(BranchId, ProductId)` keys; subsequent app restarts will never duplicate records or corrupt stock levels.

---

## 4. How to Run the Application

### Running the Backend Web API
From the repository root:
```bash
dotnet run --project src/MihiraStores.Api
```
- **HTTP Endpoint**: `http://localhost:5079`
- **Swagger UI**: `http://localhost:5079/swagger`

### Running the Frontend (React + Vite)
In a separate terminal:
```bash
# Navigate to the frontend project
cd src/MihiraStores.Web

# Install npm dependencies
npm install

# Start the Vite development server
npm run dev
```
- The frontend will be accessible at `http://localhost:5173`.

---

## 5. Running Automated Tests


```bash
# Run all tests
dotnet test tests/MihiraStores.Api.Tests

# Run tests with detailed test names
dotnet test tests/MihiraStores.Api.Tests --logger "console;verbosity=normal"
```

---

## 6. API Endpoint Summary

All endpoints return and accept immutable C# record DTOs. Database entities (`Branch`, `Product`, `Stock`, `StockTransfer`) are never exposed directly.

| Method | Route | Description | Status Codes |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/branches` | Retrieves all registered store branches & warehouses | `200 OK` |
| `GET` | `/api/products` | Retrieves all catalog products | `200 OK` |
| `GET` | `/api/branches/{id}/stock` | Retrieves current inventory levels for a branch | `200 OK`, `404 Not Found` |
| `POST` | `/api/transfers` | Creates a new stock transfer in `Requested` status | `201 Created`, `400 Bad Request`, `404 Not Found` |
| `GET` | `/api/transfers` | Lists transfers with optional `?branchId=&status=` filters | `200 OK` |
| `GET` | `/api/transfers/{id}` | Retrieves a specific transfer by its ID | `200 OK`, `404 Not Found` |
| `POST` | `/api/transfers/{id}/dispatch` | Validates stock, deducts source inventory, and sets status to `Dispatched` | `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict` |
| `POST` | `/api/transfers/{id}/receive` | Increments destination stock and sets status to `Received` | `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict` |
| `POST` | `/api/transfers/{id}/cancel` | Cancels a `Requested` transfer without modifying stock | `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict` |



