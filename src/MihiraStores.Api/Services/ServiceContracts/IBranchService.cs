namespace MihiraStores.Api.Services.ServiceContracts;

using MihiraStores.Api.DTOs;

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> GetAllBranchesAsync(CancellationToken cancellationToken = default);
    Task<BranchDto?> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockDto>> GetBranchStockAsync(int branchId, CancellationToken cancellationToken = default);
}
