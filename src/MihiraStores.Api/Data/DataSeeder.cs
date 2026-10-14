namespace MihiraStores.Api.Data;

using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.Models;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Seed Locations (Branches) idempotently
        var defaultBranches = new List<Branch>
        {
            new() { Name = "Central Warehouse", Code = "WH", IsWarehouse = true },
            new() { Name = "Colombo Branch", Code = "CMB", IsWarehouse = false },
            new() { Name = "Kandy Branch", Code = "KDY", IsWarehouse = false },
            new() { Name = "Galle Branch", Code = "GLE", IsWarehouse = false }
        };

        foreach (var branch in defaultBranches)
        {
            var exists = await context.Branches.AnyAsync(b => b.Code == branch.Code);
            if (!exists)
            {
                context.Branches.Add(branch);
            }
        }
        await context.SaveChangesAsync();

        // 2. Seed Products idempotently
        var defaultProducts = new List<Product>
        {
            new() { Name = "Coconut Milk 400ml", SKU = "PROD-COCO-400" },
            new() { Name = "Ceylon Black Tea 500g", SKU = "PROD-TEA-500" },
            new() { Name = "Basmati Rice 5kg", SKU = "PROD-RICE-5KG" },
            new() { Name = "Red Split Lentils 1kg", SKU = "PROD-LENT-1KG" },
            new() { Name = "White Sugar 1kg", SKU = "PROD-SUGR-1KG" }
        };

        foreach (var product in defaultProducts)
        {
            var exists = await context.Products.AnyAsync(p => p.SKU == product.SKU);
            if (!exists)
            {
                context.Products.Add(product);
            }
        }
        await context.SaveChangesAsync();

        // 3. Seed Initial Stock for all locations idempotently
        var allBranches = await context.Branches.ToListAsync();
        var allProducts = await context.Products.ToListAsync();
        var existingStocks = await context.Stocks.ToListAsync();

        var utcNow = DateTime.UtcNow;

        foreach (var branch in allBranches)
        {
            int initialQty = branch.IsWarehouse ? 500 : branch.Code switch
            {
                "CMB" => 100,
                "KDY" => 75,
                "GLE" => 50,
                _ => 50
            };

            foreach (var product in allProducts)
            {
                var stockExists = existingStocks.Any(s => s.BranchId == branch.Id && s.ProductId == product.Id);
                if (!stockExists)
                {
                    context.Stocks.Add(new Stock
                    {
                        BranchId = branch.Id,
                        ProductId = product.Id,
                        Quantity = initialQty,
                        LastUpdatedUtc = utcNow
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
