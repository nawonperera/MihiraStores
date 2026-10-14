namespace MihiraStores.Api.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MihiraStores.Api.Data;
using MihiraStores.Api.Models;
using MihiraStores.Api.Repositories.RepositoryContracts;

public class StockTransferRepository : IStockTransferRepository
{
    private readonly AppDbContext _context;

    public StockTransferRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StockTransfer?> GetByIdAsync(int id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<StockTransfer> query = _context.StockTransfers
            .Include(st => st.FromBranch)
            .Include(st => st.ToBranch)
            .Include(st => st.Lines)
                .ThenInclude(l => l.Product);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(st => st.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransfer>> GetTransfersAsync(int? branchId = null, TransferStatus? status = null, CancellationToken cancellationToken = default)
    {
        IQueryable<StockTransfer> query = _context.StockTransfers
            .AsNoTracking()
            .Include(st => st.FromBranch)
            .Include(st => st.ToBranch)
            .Include(st => st.Lines)
                .ThenInclude(l => l.Product);

        if (branchId.HasValue)
        {
            query = query.Where(st => st.FromBranchId == branchId.Value || st.ToBranchId == branchId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(st => st.Status == status.Value);
        }

        return await query.OrderByDescending(st => st.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        await _context.StockTransfers.AddAsync(transfer, cancellationToken);
    }

    public Task UpdateAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        _context.StockTransfers.Update(transfer);
        return Task.CompletedTask;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
