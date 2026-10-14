namespace MihiraStores.Api.Repositories.RepositoryContracts;

using Microsoft.EntityFrameworkCore.Storage;
using MihiraStores.Api.Models;

public interface IStockTransferRepository
{
    Task<StockTransfer?> GetByIdAsync(int id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransfer>> GetTransfersAsync(int? branchId = null, TransferStatus? status = null, CancellationToken cancellationToken = default);
    Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
    Task UpdateAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
