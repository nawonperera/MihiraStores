namespace MihiraStores.Api.Repositories.RepositoryContracts;

using MihiraStores.Api.Models;

public interface IStockRepository
{
    Task<Stock?> GetStockAsync(int branchId, int productId, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Stock>> GetBranchStockAsync(int branchId, CancellationToken cancellationToken = default);
    Task AddAsync(Stock stock, CancellationToken cancellationToken = default);
    Task UpdateAsync(Stock stock, CancellationToken cancellationToken = default);
}
