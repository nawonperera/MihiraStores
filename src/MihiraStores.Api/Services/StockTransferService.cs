namespace MihiraStores.Api.Services;

using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.DTOs;
using MihiraStores.Api.Exceptions;
using MihiraStores.Api.Models;
using MihiraStores.Api.Repositories.RepositoryContracts;
using MihiraStores.Api.Services.ServiceContracts;

public class StockTransferService : IStockTransferService
{
    private readonly IStockTransferRepository _transferRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;

    public StockTransferService(IStockTransferRepository transferRepository,IStockRepository stockRepository,IBranchRepository branchRepository,IProductRepository productRepository)
    {
        _transferRepository = transferRepository;
        _stockRepository = stockRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
    }

    public async Task<StockTransferDto> CreateTransferAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Rule: The sending and receiving branches cannot be the same.
        if (request.FromBranchId == request.ToBranchId)
        {
            throw new BusinessRuleException("Source and destination branches cannot be the same.");
        }

        // 2. Rule: Must contain at least one line item
        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new BusinessRuleException("Stock transfer must contain at least one item line.");
        }

        // 3. Rule: Quantities must be positive integers
        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                throw new BusinessRuleException($"Quantity for product ID {line.ProductId} must be a positive integer greater than zero.");
            }
        }

        // 4. Validate existence of source and destination branches
        var fromBranch = await _branchRepository.GetByIdAsync(request.FromBranchId, cancellationToken);
        if (fromBranch == null)
        {
            throw new EntityNotFoundException($"Source branch with ID {request.FromBranchId} does not exist.");
        }

        var toBranch = await _branchRepository.GetByIdAsync(request.ToBranchId, cancellationToken);
        if (toBranch == null)
        {
            throw new EntityNotFoundException($"Destination branch with ID {request.ToBranchId} does not exist.");
        }

        // 5. Validate existence of all requested products and consolidate duplicate lines if present
        var consolidatedLines = request.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => new CreateStockTransferLineRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        foreach (var line in consolidatedLines)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId, cancellationToken);
            if (product == null)
            {
                throw new EntityNotFoundException($"Product with ID {line.ProductId} does not exist.");
            }
        }

        // 6. Set status to Requested by default and persist
        var transfer = new StockTransfer
        {
            FromBranchId = request.FromBranchId,
            ToBranchId = request.ToBranchId,
            Status = TransferStatus.Requested,
            CreatedAtUtc = DateTime.UtcNow,
            Lines = consolidatedLines.Select(l => new StockTransferLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity
            }).ToList()
        };

        await _transferRepository.AddAsync(transfer, cancellationToken);
        await _transferRepository.SaveChangesAsync(cancellationToken);

        var created = await _transferRepository.GetByIdAsync(transfer.Id, asNoTracking: true, cancellationToken);
        return MapToDto(created!);
    }

    public async Task<StockTransferDto?> GetTransferByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
        return transfer == null ? null : MapToDto(transfer);
    }

    public async Task<IReadOnlyList<StockTransferDto>> GetTransfersAsync(int? branchId = null, TransferStatus? status = null, CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.GetTransfersAsync(branchId, status, cancellationToken);
        return transfers.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<StockDto>> GetBranchStockAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var branchExists = await _branchRepository.ExistsAsync(branchId, cancellationToken);
        if (!branchExists)
        {
            throw new EntityNotFoundException($"Branch with ID {branchId} does not exist.");
        }

        var stocks = await _stockRepository.GetBranchStockAsync(branchId, cancellationToken);
        return stocks.Select(s => new StockDto(
            s.Id,
            s.BranchId,
            s.Branch?.Name ?? string.Empty,
            s.ProductId,
            s.Product?.Name ?? string.Empty,
            s.Product?.SKU ?? string.Empty,
            s.Quantity,
            s.LastUpdatedUtc
        )).ToList();
    }

    public async Task<StockTransferDto> DispatchTransferAsync(int id, CancellationToken cancellationToken = default)
    {
        using var transaction = await _transferRepository.BeginTransactionAsync(cancellationToken);
        try
        {
            var transfer = await _transferRepository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
            if (transfer == null)
            {
                throw new EntityNotFoundException($"Stock transfer with ID {id} does not exist.");
            }

            // Operation Idempotency: re-dispatching already dispatched transfer returns current state
            if (transfer.Status == TransferStatus.Dispatched)
            {
                await transaction.RollbackAsync(cancellationToken);
                return MapToDto(transfer);
            }

            // State Machine Invariant: only transfers in Requested status can be dispatched
            if (transfer.Status != TransferStatus.Requested)
            {
                throw new InvalidTransferStateException(
                    $"Transfer {id} cannot be dispatched because it is in '{transfer.Status}' status. Only transfers in 'Requested' status can be dispatched.");
            }

            // 1. Stock Invariant: Check sending branch's stock for EVERY item BEFORE modifying anything
            var stockToDeduct = new List<(Stock Stock, int Quantity)>();
            foreach (var line in transfer.Lines)
            {
                var stock = await _stockRepository.GetStockAsync(transfer.FromBranchId, line.ProductId, asNoTracking: false, cancellationToken);
                var availableStock = stock?.Quantity ?? 0;

                if (stock == null || availableStock < line.Quantity)
                {
                    var productName = line.Product?.Name ?? $"Product ID {line.ProductId}";
                    var branchName = transfer.FromBranch?.Name ?? $"Branch ID {transfer.FromBranchId}";
                    throw new InsufficientStockException(
                        $"Insufficient stock for '{productName}' at '{branchName}'. Required: {line.Quantity}, Available: {availableStock}.");
                }

                stockToDeduct.Add((stock, line.Quantity));
            }

            // 2. Atomic mutation: Deduct stock and update transfer state in a single step
            var now = DateTime.UtcNow;
            foreach (var (stock, quantity) in stockToDeduct)
            {
                stock.Quantity -= quantity;
                stock.LastUpdatedUtc = now;
            }

            transfer.Status = TransferStatus.Dispatched;
            transfer.DispatchedAtUtc = now;

            await _transferRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapToDto(transfer);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var reloaded = await _transferRepository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (reloaded?.Status == TransferStatus.Dispatched)
            {
                return MapToDto(reloaded);
            }
            throw new ConcurrencyConflictException($"A concurrency conflict occurred while dispatching transfer {id}. Please retry.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StockTransferDto> ReceiveTransferAsync(int id, CancellationToken cancellationToken = default)
    {
        using var transaction = await _transferRepository.BeginTransactionAsync(cancellationToken);
        try
        {
            var transfer = await _transferRepository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
            if (transfer == null)
            {
                throw new EntityNotFoundException($"Stock transfer with ID {id} does not exist.");
            }

            // Operation Idempotency: re-receiving already received transfer returns current state
            if (transfer.Status == TransferStatus.Received)
            {
                await transaction.RollbackAsync(cancellationToken);
                return MapToDto(transfer);
            }

            // State Machine Invariant: only transfers in Dispatched status can be received
            if (transfer.Status != TransferStatus.Dispatched)
            {
                throw new InvalidTransferStateException(
                    $"Transfer {id} cannot be received because it is in '{transfer.Status}' status. Only transfers in 'Dispatched' status can be received.");
            }

            // Add requested item amounts to the receiving branch's stock exactly once
            var now = DateTime.UtcNow;
            foreach (var line in transfer.Lines)
            {
                var stock = await _stockRepository.GetStockAsync(transfer.ToBranchId, line.ProductId, asNoTracking: false, cancellationToken);
                if (stock == null)
                {
                    stock = new Stock
                    {
                        BranchId = transfer.ToBranchId,
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        LastUpdatedUtc = now
                    };
                    await _stockRepository.AddAsync(stock, cancellationToken);
                }
                else
                {
                    stock.Quantity += line.Quantity;
                    stock.LastUpdatedUtc = now;
                }
            }

            transfer.Status = TransferStatus.Received;
            transfer.ReceivedAtUtc = now;

            await _transferRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapToDto(transfer);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var reloaded = await _transferRepository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (reloaded?.Status == TransferStatus.Received)
            {
                return MapToDto(reloaded);
            }
            throw new ConcurrencyConflictException($"A concurrency conflict occurred while receiving transfer {id}. Please retry.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StockTransferDto> CancelTransferAsync(int id, CancellationToken cancellationToken = default)
    {
        using var transaction = await _transferRepository.BeginTransactionAsync(cancellationToken);
        try
        {
            var transfer = await _transferRepository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
            if (transfer == null)
            {
                throw new EntityNotFoundException($"Stock transfer with ID {id} does not exist.");
            }

            // Operation Idempotency: re-cancelling already cancelled transfer returns current state
            if (transfer.Status == TransferStatus.Cancelled)
            {
                await transaction.RollbackAsync(cancellationToken);
                return MapToDto(transfer);
            }

            // State Machine Invariant: only transfers in Requested status can be cancelled
            if (transfer.Status != TransferStatus.Requested)
            {
                throw new InvalidTransferStateException(
                    $"Transfer {id} cannot be cancelled because it is in '{transfer.Status}' status. Transfers can only be cancelled while in 'Requested' status.");
            }

            // Cancel without modifying stock
            transfer.Status = TransferStatus.Cancelled;
            transfer.CancelledAtUtc = DateTime.UtcNow;

            await _transferRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapToDto(transfer);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var reloaded = await _transferRepository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (reloaded?.Status == TransferStatus.Cancelled)
            {
                return MapToDto(reloaded);
            }
            throw new ConcurrencyConflictException($"A concurrency conflict occurred while cancelling transfer {id}. Please retry.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static StockTransferDto MapToDto(StockTransfer transfer)
    {
        return new StockTransferDto(
            transfer.Id,
            transfer.FromBranchId,
            transfer.FromBranch?.Name ?? string.Empty,
            transfer.ToBranchId,
            transfer.ToBranch?.Name ?? string.Empty,
            transfer.Status,
            transfer.CreatedAtUtc,
            transfer.DispatchedAtUtc,
            transfer.ReceivedAtUtc,
            transfer.CancelledAtUtc,
            transfer.Lines.Select(l => new StockTransferLineDto(
                l.Id,
                l.ProductId,
                l.Product?.Name ?? string.Empty,
                l.Product?.SKU ?? string.Empty,
                l.Quantity
            )).ToList()
        );
    }
}
