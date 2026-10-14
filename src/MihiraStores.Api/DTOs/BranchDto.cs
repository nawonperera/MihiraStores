namespace MihiraStores.Api.DTOs;

public record BranchDto(
    int Id,
    string Name,
    string Code,
    bool IsWarehouse
);
