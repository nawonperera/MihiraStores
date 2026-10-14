namespace MihiraStores.Api.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.Data;
using MihiraStores.Api.DTOs;
using MihiraStores.Api.Exceptions;
using MihiraStores.Api.Models;
using MihiraStores.Api.Repositories;
using MihiraStores.Api.Services;
using MihiraStores.Api.Services.ServiceContracts;

public class StockInvariantTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public StockInvariantTests()
    {
        // Real relational database testing: In-memory SQLite connection kept open for the test duration
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_contextOptions);
        context.Database.EnsureCreated();
        DataSeeder.SeedAsync(context).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private (AppDbContext Context, IStockTransferService Service) CreateServiceContext()
    {
        var context = new AppDbContext(_contextOptions);
        var transferRepo = new StockTransferRepository(context);
        var stockRepo = new StockRepository(context);
        var branchRepo = new BranchRepository(context);
        var productRepo = new ProductRepository(context);
        var service = new StockTransferService(transferRepo, stockRepo, branchRepo, productRepo);

        return (context, service);
    }

    private AppDbContext CreateVerificationContext() => new(_contextOptions);

    // 1. Dispatch_WhenRepeated_DoesNotDeductStockTwice
    [Fact]
    public async Task Dispatch_WhenRepeated_DoesNotDeductStockTwice()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            var initialStock = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 50) }
            ));

            // Act 1: First dispatch
            var firstDispatch = await service.DispatchTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Dispatched, firstDispatch.Status);

            // Act 2: Second dispatch (idempotency check)
            var secondDispatch = await service.DispatchTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Dispatched, secondDispatch.Status);

            // Assert: Verify stock was deducted exactly once in database
            await using var verifyContext = CreateVerificationContext();
            var verifiedStock = await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1);
            var verifiedTransfer = await verifyContext.StockTransfers.FirstAsync(t => t.Id == transfer.Id);

            Assert.Equal(initialStock - 50, verifiedStock.Quantity);
            Assert.Equal(TransferStatus.Dispatched, verifiedTransfer.Status);
            Assert.NotNull(verifiedTransfer.DispatchedAtUtc);
        }
    }

    // 3. Receive_AfterDispatch_AddsStockToDestination
    [Fact]
    public async Task Receive_AfterDispatch_AddsStockToDestination()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            var destStockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 40) }
            ));

            await service.DispatchTransferAsync(transfer.Id);

            // Act: Receive transfer at destination branch
            var receivedResult = await service.ReceiveTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Received, receivedResult.Status);
            Assert.NotNull(receivedResult.ReceivedAtUtc);

            // Assert: Verify destination stock and transfer state in database
            await using var verifyContext = CreateVerificationContext();
            var verifiedDestStock = await verifyContext.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1);
            var verifiedTransfer = await verifyContext.StockTransfers.FirstAsync(t => t.Id == transfer.Id);

            Assert.Equal(destStockBefore + 40, verifiedDestStock.Quantity);
            Assert.Equal(TransferStatus.Received, verifiedTransfer.Status);
            Assert.NotNull(verifiedTransfer.ReceivedAtUtc);
        }
    }

    // 4. Receive_WhenRepeated_DoesNotAddStockTwice
    [Fact]
    public async Task Receive_WhenRepeated_DoesNotAddStockTwice()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            var destStockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 35) }
            ));

            await service.DispatchTransferAsync(transfer.Id);

            // Act 1: First receive
            var firstReceive = await service.ReceiveTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Received, firstReceive.Status);

            // Act 2: Second receive (idempotency check)
            var secondReceive = await service.ReceiveTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Received, secondReceive.Status);

            // Assert: Verify stock was incremented only once in database
            await using var verifyContext = CreateVerificationContext();
            var verifiedDestStock = await verifyContext.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1);
            Assert.Equal(destStockBefore + 35, verifiedDestStock.Quantity);
        }
    }

    // 5. Receive_WhenTransferIsRequested_IsRejected
    [Fact]
    public async Task Receive_WhenTransferIsRequested_IsRejected()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            var destStockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 20) }
            ));

            // Act: Attempt to receive directly while still in Requested status
            var ex = await Assert.ThrowsAsync<InvalidTransferStateException>(() => service.ReceiveTransferAsync(transfer.Id));
            Assert.Contains("Only transfers in 'Dispatched' status can be received", ex.Message);

            // Assert: Verify transfer and stock remain unmodified in database
            await using var verifyContext = CreateVerificationContext();
            var verifiedTransfer = await verifyContext.StockTransfers.FirstAsync(t => t.Id == transfer.Id);
            var verifiedDestStock = await verifyContext.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1);

            Assert.Equal(TransferStatus.Requested, verifiedTransfer.Status);
            Assert.Null(verifiedTransfer.ReceivedAtUtc);
            Assert.Equal(destStockBefore, verifiedDestStock.Quantity);
        }
    }

    // 6. Cancel_WhenTransferIsDispatched_IsRejected
    [Fact]
    public async Task Cancel_WhenTransferIsDispatched_IsRejected()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 15) }
            ));

            await service.DispatchTransferAsync(transfer.Id);

            // Act: Attempt to cancel a dispatched transfer
            var ex = await Assert.ThrowsAsync<InvalidTransferStateException>(() => service.CancelTransferAsync(transfer.Id));
            Assert.Contains("Transfers can only be cancelled while in 'Requested' status", ex.Message);

            // Assert: Verify transfer remains Dispatched in database
            await using var verifyContext = CreateVerificationContext();
            var verifiedTransfer = await verifyContext.StockTransfers.FirstAsync(t => t.Id == transfer.Id);

            Assert.Equal(TransferStatus.Dispatched, verifiedTransfer.Status);
            Assert.Null(verifiedTransfer.CancelledAtUtc);
        }
    }

    // 6. Dispatch_WhenOneOfSeveralLinesHasInsufficientStock_RollsBackAllLines
    [Fact]
    public async Task Dispatch_WhenOneOfSeveralLinesHasInsufficientStock_RollsBackAllLines()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            // Record initial stock quantities for products 1, 2, and 3 at warehouse
            var p1StockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;
            var p2StockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 2)).Quantity;
            var p3StockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 3)).Quantity;

            // Line 1: valid (25), Line 2: INSUFFICIENT (9999), Line 3: valid (20)
            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[]
                {
                    new CreateStockTransferLineRequest(1, 25),
                    new CreateStockTransferLineRequest(2, 9999),
                    new CreateStockTransferLineRequest(3, 20)
                }
            ));

            // Act: Dispatch must abort and roll back all changes
            var ex = await Assert.ThrowsAsync<InsufficientStockException>(() => service.DispatchTransferAsync(transfer.Id));
            Assert.Contains("Insufficient stock", ex.Message);

            // Assert: Invariant check - Verify ALL lines rolled back in database and stock is 100% intact
            await using var verifyContext = CreateVerificationContext();
            var p1StockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;
            var p2StockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 2)).Quantity;
            var p3StockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 3)).Quantity;
            var verifiedTransfer = await verifyContext.StockTransfers.FirstAsync(t => t.Id == transfer.Id);

            Assert.Equal(p1StockBefore, p1StockAfter);
            Assert.Equal(p2StockBefore, p2StockAfter);
            Assert.Equal(p3StockBefore, p3StockAfter);
            Assert.Equal(TransferStatus.Requested, verifiedTransfer.Status);
            Assert.Null(verifiedTransfer.DispatchedAtUtc);
        }
    }
}
