namespace MihiraStores.Api.DTOs;

using MihiraStores.Api.Models;

public record StockTransferDto(
    int Id,
    int FromBranchId,
    string FromBranchName,
    int ToBranchId,
    string ToBranchName,
    TransferStatus Status,
    DateTime CreatedAtUtc,
    DateTime? DispatchedAtUtc,
    DateTime? ReceivedAtUtc,
    DateTime? CancelledAtUtc,
    IReadOnlyList<StockTransferLineDto> Lines
);
