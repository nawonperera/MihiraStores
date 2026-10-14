namespace MihiraStores.Api.Repositories.RepositoryContracts;

using MihiraStores.Api.Models;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default);
}
