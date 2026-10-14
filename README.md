# Mihira Stores

**Mihira Stores** is a multi-branch retail inventory and stock transfer management application designed with a strict 3-tier enterprise architecture.

---

## Technology Stack

- **Backend**: ASP.NET Core Web API (.NET 10)
- **ORM**: Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`)
- **Database**: Microsoft SQL Server (supports LocalDB, SQL Server Express, and full SQL Server instances)
- **Frontend**: React 19 with TypeScript, Vite 8, and Tailwind CSS v4
- **Testing**: xUnit with in-memory SQLite integration tests

---

## Database Configuration

The application uses **Microsoft SQL Server** as its configured database provider.

### 1. Connection String Configuration

The database connection string is located in [`src/MihiraStores.Api/appsettings.json`](src/MihiraStores.Api/appsettings.json) and can be overridden for local development in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MihiraStoresDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

#### SQL Server Options
- **SQL Server LocalDB (Default)**:
  ```
  Server=(localdb)\\mssqllocaldb;Database=MihiraStoresDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
  ```
- **SQL Server Express**:
  ```
  Server=.\\SQLEXPRESS;Database=MihiraStoresDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
  ```
- **Standard SQL Server (with SQL Authentication)**:
  ```
  Server=localhost,1433;Database=MihiraStoresDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true
  ```

---

## Entity Framework Core Migrations

### Prerequisites

Ensure the `dotnet-ef` global CLI tool is installed:

```bash
dotnet tool install --global dotnet-ef
# or update existing tool:
dotnet tool update --global dotnet-ef
```

### Applying Migrations

To apply pending migrations to the configured database, run:

```bash
dotnet ef database update --project src/MihiraStores.Api
```

### Adding New Migrations

When modifying domain entity models in `src/MihiraStores.Api/Models/` or configuration in `AppDbContext`:

```bash
dotnet ef migrations add <DescriptiveMigrationName> --project src/MihiraStores.Api
```

Inspect the generated migration under `src/MihiraStores.Api/Migrations/` before applying it.

### Rolling Back Migrations

To roll back to a specific previous migration or revert all migrations:

```bash
# Revert to a specific migration
dotnet ef database update <PreviousMigrationName> --project src/MihiraStores.Api

# Revert all migrations
dotnet ef database update 0 --project src/MihiraStores.Api

# Remove the latest unapplied migration
dotnet ef migrations remove --project src/MihiraStores.Api
```

---

## Startup Seeding & Idempotency

When the API starts up, it automatically executes pending migrations and runs the idempotent seeder:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DataSeeder.SeedAsync(context);
}
```

- **4 Locations**: Central Warehouse (`WH`), Colombo Branch (`CMB`), Kandy Branch (`KDY`), Galle Branch (`GLE`).
- **5 Products**: Coconut Milk 400ml, Ceylon Black Tea 500g, Basmati Rice 5kg, Red Split Lentils 1kg, White Sugar 1kg.
- **20 Stock Records**: Initialized with tier-appropriate positive quantities across all branches.
- **Idempotency Guarantee**: The seeder queries existing records by unique code/SKU/composite keys; restarting the application will never produce duplicate records or corrupt data.

---

## Building and Running Tests

### Build Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test tests/MihiraStores.Api.Tests
```

### Run Web API
```bash
dotnet run --project src/MihiraStores.Api
```
Open OpenAPI / Swagger in browser at `https://localhost:<port>/openapi/v1.json`.
