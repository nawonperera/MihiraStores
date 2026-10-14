namespace MihiraStores.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public record CreateStockTransferRequest(
    [Required] int FromBranchId,
    [Required] int ToBranchId,
    [Required, MinLength(1, ErrorMessage = "Transfer must contain at least one line.")] IReadOnlyList<CreateStockTransferLineRequest> Lines
);
