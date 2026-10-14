namespace MihiraStores.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MihiraStores.Api.DTOs;
using MihiraStores.Api.Exceptions;
using MihiraStores.Api.Services.ServiceContracts;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockTransferService _service;

    public StockController(IStockTransferService service)
    {
        _service = service;
    }

    [HttpGet("branch/{branchId:int}")]
    [ProducesResponseType<IReadOnlyList<StockDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StockDto>>> GetBranchStock(int branchId, CancellationToken cancellationToken)
    {
        try
        {
            var stock = await _service.GetBranchStockAsync(branchId, cancellationToken);
            return Ok(stock);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
    }
}
