namespace MihiraStores.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MihiraStores.Api.DTOs;
using MihiraStores.Api.Exceptions;
using MihiraStores.Api.Services.ServiceContracts;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BranchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetBranches(CancellationToken cancellationToken)
    {
        var branches = await _branchService.GetAllBranchesAsync(cancellationToken);
        return Ok(branches);
    }

    [HttpGet("{id:int}/stock")]
    [ProducesResponseType<IReadOnlyList<StockDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StockDto>>> GetBranchStock(int id, CancellationToken cancellationToken)
    {
        try
        {
            var stock = await _branchService.GetBranchStockAsync(id, cancellationToken);
            return Ok(stock);
        }
        catch (EntityNotFoundException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                title: "Branch Not Found",
                instance: HttpContext.Request.Path
            );
        }
    }
}
