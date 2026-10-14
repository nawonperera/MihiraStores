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
}
