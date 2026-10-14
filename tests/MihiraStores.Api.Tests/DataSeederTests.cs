namespace MihiraStores.Api.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.Data;

public class DataSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public DataSeederTests()
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
    public async Task SeedAsync_ShouldSeedFourBranchesFiveProductsAndTwentyStockRecords()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);

        // Act
        await DataSeeder.SeedAsync(context);

        // Assert
        var branches = await context.Branches.ToListAsync();
        Assert.Equal(4, branches.Count);
        Assert.Contains(branches, b => b.Code == "WH" && b.IsWarehouse && b.Name == "Central Warehouse");
        Assert.Contains(branches, b => b.Code == "CMB" && !b.IsWarehouse && b.Name == "Colombo Branch");
        Assert.Contains(branches, b => b.Code == "KDY" && !b.IsWarehouse && b.Name == "Kandy Branch");
        Assert.Contains(branches, b => b.Code == "GLE" && !b.IsWarehouse && b.Name == "Galle Branch");

        var products = await context.Products.ToListAsync();
        Assert.Equal(5, products.Count);
        Assert.Contains(products, p => p.SKU == "PROD-COCO-400");
        Assert.Contains(products, p => p.SKU == "PROD-TEA-500");
        Assert.Contains(products, p => p.SKU == "PROD-RICE-5KG");
        Assert.Contains(products, p => p.SKU == "PROD-LENT-1KG");
        Assert.Contains(products, p => p.SKU == "PROD-SUGR-1KG");

        var stocks = await context.Stocks.ToListAsync();
        Assert.Equal(20, stocks.Count); // 4 locations * 5 products = 20 stock rows

        // Warehouse should have 500 of each
        var whBranch = branches.First(b => b.Code == "WH");
        var whStocks = stocks.Where(s => s.BranchId == whBranch.Id).ToList();
        Assert.All(whStocks, s => Assert.Equal(500, s.Quantity));

        // Colombo should have 100 of each
        var colBranch = branches.First(b => b.Code == "CMB");
        var colStocks = stocks.Where(s => s.BranchId == colBranch.Id).ToList();
        Assert.All(colStocks, s => Assert.Equal(100, s.Quantity));
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent_WhenExecutedMultipleTimes()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);

        // Act: Execute initial seed
        await DataSeeder.SeedAsync(context);

        // Re-execute seed (simulating restart)
        await DataSeeder.SeedAsync(context);
        await DataSeeder.SeedAsync(context);

        // Assert: Counts remain exactly the same, no duplicates created
        var branchCount = await context.Branches.CountAsync();
        var productCount = await context.Products.CountAsync();
        var stockCount = await context.Stocks.CountAsync();

        Assert.Equal(4, branchCount);
        Assert.Equal(5, productCount);
        Assert.Equal(20, stockCount);
    }
}
