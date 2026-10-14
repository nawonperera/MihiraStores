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
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTransferDto>> CreateTransfer([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateTransferAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetTransferById), new { id = created.Id }, created);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTransferDto>> GetTransferById(int id, CancellationToken cancellationToken)
    {
        var transfer = await _service.GetTransferByIdAsync(id, cancellationToken);
        if (transfer == null)
        {
            return NotFound(new ErrorResponseDto($"Stock transfer with ID {id} was not found."));
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
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockTransferDto>> DispatchTransfer(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.DispatchTransferAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ErrorResponseDto(ex.Message));
        }
    }

    [HttpPost("{id:int}/receive")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockTransferDto>> ReceiveTransfer(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ReceiveTransferAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ErrorResponseDto(ex.Message));
        }
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponseDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockTransferDto>> CancelTransfer(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CancelTransferAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new ErrorResponseDto(ex.Message));
        }
    }
}
