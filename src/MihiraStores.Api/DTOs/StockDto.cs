namespace MihiraStores.Api.DTOs;

public record StockDto(
    int Id,
    int BranchId,
    string BranchName,
    int ProductId,
    string ProductName,
    string ProductSku,
    int Quantity,
    DateTime LastUpdatedUtc
);
