namespace MihiraStores.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public record CreateStockTransferLineRequest(
    [Required] int ProductId,
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer.")] int Quantity
);
