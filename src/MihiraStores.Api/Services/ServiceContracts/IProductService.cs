namespace MihiraStores.Api.Services.ServiceContracts;

using MihiraStores.Api.DTOs;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
}
