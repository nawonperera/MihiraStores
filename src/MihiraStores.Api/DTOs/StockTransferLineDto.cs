namespace MihiraStores.Api.DTOs;

public record StockTransferLineDto(
    int Id,
    int ProductId,
    string ProductName,
    string ProductSku,
    int Quantity
);
