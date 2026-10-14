namespace MihiraStores.Api.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.Data;
using MihiraStores.Api.Models;

public class DomainModelConstraintTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public DomainModelConstraintTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_contextOptions);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task Branch_DuplicateCode_ShouldThrowDbUpdateException()
    {
        using var context = new AppDbContext(_contextOptions);

        context.Branches.Add(new Branch { Name = "Branch 1", Code = "B1", IsWarehouse = false });
        await context.SaveChangesAsync();

        context.Branches.Add(new Branch { Name = "Branch 2", Code = "B1", IsWarehouse = false });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Product_DuplicateSKU_ShouldThrowDbUpdateException()
    {
        using var context = new AppDbContext(_contextOptions);

        context.Products.Add(new Product { Name = "Product 1", SKU = "SKU-1" });
        await context.SaveChangesAsync();

        context.Products.Add(new Product { Name = "Product 2", SKU = "SKU-1" });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Stock_DuplicateBranchAndProduct_ShouldThrowDbUpdateException()
    {
        using var context = new AppDbContext(_contextOptions);

        var branch = new Branch { Name = "Colombo", Code = "CMB", IsWarehouse = false };
        var product = new Product { Name = "Tea", SKU = "SKU-TEA" };
        context.Branches.Add(branch);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.Stocks.Add(new Stock
        {
            BranchId = branch.Id,
            ProductId = product.Id,
            Quantity = 50,
            LastUpdatedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        context.Stocks.Add(new Stock
        {
            BranchId = branch.Id,
            ProductId = product.Id,
            Quantity = 20,
            LastUpdatedUtc = DateTime.UtcNow
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task StockTransfer_CascadesDeleteToLines_WhenTransferIsRemoved()
    {
        using var context = new AppDbContext(_contextOptions);

        var b1 = new Branch { Name = "Warehouse", Code = "WH", IsWarehouse = true };
        var b2 = new Branch { Name = "Colombo", Code = "CMB", IsWarehouse = false };
        var prod = new Product { Name = "Rice", SKU = "SKU-RICE" };
        context.Branches.AddRange(b1, b2);
        context.Products.Add(prod);
        await context.SaveChangesAsync();

        var transfer = new StockTransfer
        {
            FromBranchId = b1.Id,
            ToBranchId = b2.Id,
            Status = TransferStatus.Requested,
            CreatedAtUtc = DateTime.UtcNow,
            Lines = new List<StockTransferLine>
            {
                new() { ProductId = prod.Id, Quantity = 10 }
            }
        };

        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.StockTransferLines.CountAsync());

        // Delete transfer
        context.StockTransfers.Remove(transfer);
        await context.SaveChangesAsync();

        // Lines should be cascade-deleted
        Assert.Equal(0, await context.StockTransferLines.CountAsync());
    }
}
