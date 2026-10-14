namespace MihiraStores.Api.DTOs;

public record ErrorResponseDto(
    string Message,
    string? Details = null
);
