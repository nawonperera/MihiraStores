namespace MihiraStores.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MihiraStores.Api.DTOs;
using MihiraStores.Api.Exceptions;
using MihiraStores.Api.Models;
using MihiraStores.Api.Services.ServiceContracts;

[ApiController]
[Route("api/[controller]")]
public class TransfersController : ControllerBase
{
    private readonly IStockTransferService _service;

    public TransfersController(IStockTransferService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTransferDto>> CreateTransfer([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateTransferAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetTransferById), new { id = created.Id }, created);
        }
        catch (EntityNotFoundException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource Not Found",
                instance: HttpContext.Request.Path
            );
        }
        catch (BusinessRuleException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Request Data",
                instance: HttpContext.Request.Path
            );
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTransferDto>> GetTransferById(int id, CancellationToken cancellationToken)
    {
        var transfer = await _service.GetTransferByIdAsync(id, cancellationToken);
        if (transfer == null)
        {
            return Problem(
                detail: $"Stock transfer with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Transfer Not Found",
                instance: HttpContext.Request.Path
            );
        }
        return Ok(transfer);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StockTransferDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockTransferDto>>> GetTransfers(
        [FromQuery] int? branchId,
        [FromQuery] TransferStatus? status,
        CancellationToken cancellationToken)
    {
        var transfers = await _service.GetTransfersAsync(branchId, status, cancellationToken);
        return Ok(transfers);
    }

    [HttpPost("{id:int}/dispatch")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockTransferDto>> DispatchTransfer(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.DispatchTransferAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                title: "Transfer Not Found",
                instance: HttpContext.Request.Path
            );
        }
        catch (InvalidTransferStateException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Transfer State Conflict",
                instance: HttpContext.Request.Path
            );
        }
        catch (ConcurrencyConflictException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency Conflict",
                instance: HttpContext.Request.Path
            );
        }
        catch (BusinessRuleException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dispatch Validation Failed",
                instance: HttpContext.Request.Path
            );
        }
    }

    [HttpPost("{id:int}/receive")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockTransferDto>> ReceiveTransfer(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ReceiveTransferAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                title: "Transfer Not Found",
                instance: HttpContext.Request.Path
            );
        }
        catch (InvalidTransferStateException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Transfer State Conflict",
                instance: HttpContext.Request.Path
            );
        }
        catch (ConcurrencyConflictException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency Conflict",
                instance: HttpContext.Request.Path
            );
        }
        catch (BusinessRuleException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Receive Validation Failed",
                instance: HttpContext.Request.Path
            );
        }
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockTransferDto>> CancelTransfer(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CancelTransferAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                title: "Transfer Not Found",
                instance: HttpContext.Request.Path
            );
        }
        catch (InvalidTransferStateException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Transfer State Conflict",
                instance: HttpContext.Request.Path
            );
        }
        catch (ConcurrencyConflictException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency Conflict",
                instance: HttpContext.Request.Path
            );
        }
        catch (BusinessRuleException ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cancel Validation Failed",
                instance: HttpContext.Request.Path
            );
        }
    }
}
