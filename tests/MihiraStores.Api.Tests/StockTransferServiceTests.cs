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

public class StockTransferServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public StockTransferServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_contextOptions);
        context.Database.EnsureCreated();
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

    [Fact]
    public async Task CreateTransferAsync_WithIdenticalBranches_ThrowsBusinessRuleException()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var request = new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 1,
                Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
            );

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateTransferAsync(request));
            Assert.Contains("cannot be the same", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CreateTransferAsync_WithEmptyLines_ThrowsBusinessRuleException()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var request = new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: Array.Empty<CreateStockTransferLineRequest>()
            );

            await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateTransferAsync(request));
        }
    }

    [Fact]
    public async Task CreateTransferAsync_WithZeroOrNegativeQuantity_ThrowsBusinessRuleException()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var request = new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 0) }
            );

            await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateTransferAsync(request));
        }
    }

    [Fact]
    public async Task CreateTransferAsync_WithNonExistentBranch_ThrowsEntityNotFoundException()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var request = new CreateStockTransferRequest(
                FromBranchId: 999,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 5) }
            );

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.CreateTransferAsync(request));
        }
    }

    [Fact]
    public async Task CreateTransferAsync_WithValidRequest_SetsStatusToRequested()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var request = new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 15) }
            );

            var result = await service.CreateTransferAsync(request);

            Assert.NotNull(result);
            Assert.Equal(TransferStatus.Requested, result.Status);
            Assert.Equal(1, result.FromBranchId);
            Assert.Equal(2, result.ToBranchId);
            Assert.Single(result.Lines);
            Assert.Equal(15, result.Lines[0].Quantity);
        }
    }

    [Fact]
    public async Task DispatchTransferAsync_WithInsufficientStockOnOneLine_AbortsAndRollsBackAllStockChanges()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            // Warehouse has 500 of product 1 and 500 of product 2
            var stock1Before = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;
            var stock2Before = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 2)).Quantity;

            // Request line 1: valid (50), line 2: excessive (9999)
            var createRequest = new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[]
                {
                    new CreateStockTransferLineRequest(1, 50),
                    new CreateStockTransferLineRequest(2, 9999)
                }
            );

            var transfer = await service.CreateTransferAsync(createRequest);

            // Act & Assert: Dispatch should fail
            var ex = await Assert.ThrowsAsync<InsufficientStockException>(() => service.DispatchTransferAsync(transfer.Id));
            Assert.Contains("Insufficient stock", ex.Message);

            // Verify: Invariant check - NO stock deducted on line 1, status remains Requested
            using var verifyContext = new AppDbContext(_contextOptions);
            var stock1After = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;
            var stock2After = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 2)).Quantity;
            var transferAfter = await verifyContext.StockTransfers.FirstAsync(t => t.Id == transfer.Id);

            Assert.Equal(stock1Before, stock1After);
            Assert.Equal(stock2Before, stock2After);
            Assert.Equal(TransferStatus.Requested, transferAfter.Status);
            Assert.Null(transferAfter.DispatchedAtUtc);
        }
    }

    [Fact]
    public async Task DispatchTransferAsync_WithSufficientStock_DeductsStockAndSetsDispatched()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var stockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;

            var createRequest = new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 40) }
            );

            var transfer = await service.CreateTransferAsync(createRequest);

            // Act
            var dispatched = await service.DispatchTransferAsync(transfer.Id);

            // Assert
            Assert.Equal(TransferStatus.Dispatched, dispatched.Status);
            Assert.NotNull(dispatched.DispatchedAtUtc);

            using var verifyContext = new AppDbContext(_contextOptions);
            var stockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;
            Assert.Equal(stockBefore - 40, stockAfter);
        }
    }

    [Fact]
    public async Task DispatchTransferAsync_IsIdempotent_SecondCallDoesNotDeductStockAgain()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var stockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 30) }
            ));

            // Act: First dispatch
            var firstDispatch = await service.DispatchTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Dispatched, firstDispatch.Status);

            // Re-dispatch
            var secondDispatch = await service.DispatchTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Dispatched, secondDispatch.Status);

            // Verify stock deducted only once
            using var verifyContext = new AppDbContext(_contextOptions);
            var stockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;
            Assert.Equal(stockBefore - 30, stockAfter);
        }
    }

    [Fact]
    public async Task ReceiveTransferAsync_WhenDispatched_AddsStockAndSetsReceived()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var colomboStockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 25) }
            ));

            await service.DispatchTransferAsync(transfer.Id);

            // Act
            var received = await service.ReceiveTransferAsync(transfer.Id);

            // Assert
            Assert.Equal(TransferStatus.Received, received.Status);
            Assert.NotNull(received.ReceivedAtUtc);

            using var verifyContext = new AppDbContext(_contextOptions);
            var colomboStockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1)).Quantity;
            Assert.Equal(colomboStockBefore + 25, colomboStockAfter);
        }
    }

    [Fact]
    public async Task ReceiveTransferAsync_IsIdempotent_SecondCallDoesNotAddStockAgain()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var colomboStockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 25) }
            ));

            await service.DispatchTransferAsync(transfer.Id);
            await service.ReceiveTransferAsync(transfer.Id);

            // Act: Re-receive
            var secondReceive = await service.ReceiveTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Received, secondReceive.Status);

            // Assert: Stock incremented only once
            using var verifyContext = new AppDbContext(_contextOptions);
            var colomboStockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 2 && s.ProductId == 1)).Quantity;
            Assert.Equal(colomboStockBefore + 25, colomboStockAfter);
        }
    }

    [Fact]
    public async Task ReceiveTransferAsync_WhenInRequestedStatus_ThrowsInvalidTransferStateException()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
            ));

            // Cannot receive directly from Requested
            await Assert.ThrowsAsync<InvalidTransferStateException>(() => service.ReceiveTransferAsync(transfer.Id));
        }
    }

    [Fact]
    public async Task CancelTransferAsync_WhenRequested_CancelsWithoutChangingStock()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var stockBefore = (await context.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 20) }
            ));

            // Act
            var cancelled = await service.CancelTransferAsync(transfer.Id);

            // Assert
            Assert.Equal(TransferStatus.Cancelled, cancelled.Status);
            Assert.NotNull(cancelled.CancelledAtUtc);

            using var verifyContext = new AppDbContext(_contextOptions);
            var stockAfter = (await verifyContext.Stocks.FirstAsync(s => s.BranchId == 1 && s.ProductId == 1)).Quantity;
            Assert.Equal(stockBefore, stockAfter);
        }
    }

    [Fact]
    public async Task CancelTransferAsync_WhenDispatched_ThrowsInvalidTransferStateException()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 20) }
            ));

            await service.DispatchTransferAsync(transfer.Id);

            // Act & Assert: Invariant rule - Dispatched transfers cannot be cancelled
            var ex = await Assert.ThrowsAsync<InvalidTransferStateException>(() => service.CancelTransferAsync(transfer.Id));
            Assert.Contains("cannot be cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CancelTransferAsync_IsIdempotent_SecondCallReturnsCurrentState()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var transfer = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
            ));

            await service.CancelTransferAsync(transfer.Id);

            // Act: Second cancel
            var secondCancel = await service.CancelTransferAsync(transfer.Id);
            Assert.Equal(TransferStatus.Cancelled, secondCancel.Status);
        }
    }

    [Fact]
    public async Task GetTransfersAsync_WithFilters_ReturnsFilteredTransfers()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var t1 = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 2,
                Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
            ));

            var t2 = await service.CreateTransferAsync(new CreateStockTransferRequest(
                FromBranchId: 1,
                ToBranchId: 3,
                Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
            ));

            await service.DispatchTransferAsync(t1.Id);

            // Filter by branch 2
            var branch2Transfers = await service.GetTransfersAsync(branchId: 2);
            Assert.Single(branch2Transfers);
            Assert.Equal(t1.Id, branch2Transfers[0].Id);

            // Filter by status Dispatched
            var dispatchedTransfers = await service.GetTransfersAsync(status: TransferStatus.Dispatched);
            Assert.Single(dispatchedTransfers);
            Assert.Equal(t1.Id, dispatchedTransfers[0].Id);

            // Filter by status Requested
            var requestedTransfers = await service.GetTransfersAsync(status: TransferStatus.Requested);
            Assert.Single(requestedTransfers);
            Assert.Equal(t2.Id, requestedTransfers[0].Id);
        }
    }

    [Fact]
    public async Task GetBranchStockAsync_ReturnsStockRecordsForBranch()
    {
        var (context, service) = CreateServiceContext();
        await using (context)
        {
            await DataSeeder.SeedAsync(context);

            var stock = await service.GetBranchStockAsync(branchId: 1);
            Assert.Equal(5, stock.Count);
            Assert.All(stock, s => Assert.Equal(1, s.BranchId));
        }
    }
}

