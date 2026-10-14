namespace MihiraStores.Api.Repositories;

using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.Data;
using MihiraStores.Api.Models;
using MihiraStores.Api.Repositories.RepositoryContracts;

public class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Stock?> GetStockAsync(int branchId, int productId, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Stock> query = _context.Stocks
            .Include(s => s.Branch)
            .Include(s => s.Product);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s => s.BranchId == branchId && s.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<Stock>> GetBranchStockAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Include(s => s.Branch)
            .Include(s => s.Product)
            .Where(s => s.BranchId == branchId)
            .OrderBy(s => s.Product!.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        await _context.Stocks.AddAsync(stock, cancellationToken);
    }

    public Task UpdateAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        _context.Stocks.Update(stock);
        return Task.CompletedTask;
    }
}
