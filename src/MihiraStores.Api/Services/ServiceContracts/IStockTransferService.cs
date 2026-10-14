namespace MihiraStores.Api.Services.ServiceContracts;

using MihiraStores.Api.DTOs;
using MihiraStores.Api.Models;

public interface IStockTransferService
{
    Task<StockTransferDto> CreateTransferAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto?> GetTransferByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransferDto>> GetTransfersAsync(int? branchId = null, TransferStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockDto>> GetBranchStockAsync(int branchId, CancellationToken cancellationToken = default);
    Task<StockTransferDto> DispatchTransferAsync(int id, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ReceiveTransferAsync(int id, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CancelTransferAsync(int id, CancellationToken cancellationToken = default);
}
