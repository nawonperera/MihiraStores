namespace MihiraStores.Api.Services;

using MihiraStores.Api.DTOs;
using MihiraStores.Api.Exceptions;
using MihiraStores.Api.Repositories.RepositoryContracts;
using MihiraStores.Api.Services.ServiceContracts;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IStockRepository _stockRepository;

    public BranchService(IBranchRepository branchRepository, IStockRepository stockRepository)
    {
        _branchRepository = branchRepository;
        _stockRepository = stockRepository;
    }

    public async Task<IReadOnlyList<BranchDto>> GetAllBranchesAsync(CancellationToken cancellationToken = default)
    {
        var branches = await _branchRepository.GetAllAsync(cancellationToken);
        return branches.Select(b => new BranchDto(b.Id, b.Name, b.Code, b.IsWarehouse)).ToList();
    }

    public async Task<BranchDto?> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var branch = await _branchRepository.GetByIdAsync(id, cancellationToken);
        return branch == null ? null : new BranchDto(branch.Id, branch.Name, branch.Code, branch.IsWarehouse);
    }

    public async Task<IReadOnlyList<StockDto>> GetBranchStockAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var branchExists = await _branchRepository.ExistsAsync(branchId, cancellationToken);
        if (!branchExists)
        {
            throw new EntityNotFoundException($"Branch with ID {branchId} was not found.");
        }

        var stocks = await _stockRepository.GetBranchStockAsync(branchId, cancellationToken);
        return stocks.Select(s => new StockDto(
            s.Id,
            s.BranchId,
            s.Branch?.Name ?? string.Empty,
            s.ProductId,
            s.Product?.Name ?? string.Empty,
            s.Product?.SKU ?? string.Empty,
            s.Quantity,
            s.LastUpdatedUtc
        )).ToList();
    }
}
