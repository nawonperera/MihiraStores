namespace MihiraStores.Api.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.Controllers;
using MihiraStores.Api.Data;
using MihiraStores.Api.DTOs;
using MihiraStores.Api.Models;
using MihiraStores.Api.Repositories;
using MihiraStores.Api.Services;

public class ControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public ControllerTests()
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

    private (BranchesController Branches, ProductsController Products, TransfersController Transfers) CreateControllers(AppDbContext context)
    {
        var branchRepo = new BranchRepository(context);
        var productRepo = new ProductRepository(context);
        var stockRepo = new StockRepository(context);
        var transferRepo = new StockTransferRepository(context);

        var branchService = new BranchService(branchRepo, stockRepo);
        var productService = new ProductService(productRepo);
        var transferService = new StockTransferService(transferRepo, stockRepo, branchRepo, productRepo);

        var httpContext = new DefaultHttpContext();

        var branchesCtrl = new BranchesController(branchService) { ControllerContext = new ControllerContext { HttpContext = httpContext } };
        var productsCtrl = new ProductsController(productService) { ControllerContext = new ControllerContext { HttpContext = httpContext } };
        var transfersCtrl = new TransfersController(transferService) { ControllerContext = new ControllerContext { HttpContext = httpContext } };

        return (branchesCtrl, productsCtrl, transfersCtrl);
    }

    [Fact]
    public async Task GetBranches_Returns200WithBranchList()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (branchesCtrl, _, _) = CreateControllers(context);

        var result = await branchesCtrl.GetBranches(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var branches = Assert.IsAssignableFrom<IReadOnlyList<BranchDto>>(okResult.Value);
        Assert.Equal(4, branches.Count);
    }

    [Fact]
    public async Task GetBranchStock_ExistingBranch_Returns200WithStock()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (branchesCtrl, _, _) = CreateControllers(context);

        var result = await branchesCtrl.GetBranchStock(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stock = Assert.IsAssignableFrom<IReadOnlyList<StockDto>>(okResult.Value);
        Assert.Equal(5, stock.Count);
    }

    [Fact]
    public async Task GetBranchStock_UnknownBranch_Returns404ProblemDetails()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (branchesCtrl, _, _) = CreateControllers(context);

        var result = await branchesCtrl.GetBranchStock(999, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("not found", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProducts_Returns200WithProductList()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (_, productsCtrl, _) = CreateControllers(context);

        var result = await productsCtrl.GetProducts(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductDto>>(okResult.Value);
        Assert.Equal(5, products.Count);
    }

    [Fact]
    public async Task CreateTransfer_ValidRequest_Returns201Created()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (_, _, transfersCtrl) = CreateControllers(context);

        var request = new CreateStockTransferRequest(
            FromBranchId: 1,
            ToBranchId: 2,
            Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
        );

        var result = await transfersCtrl.CreateTransfer(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        var transfer = Assert.IsType<StockTransferDto>(createdResult.Value);
        Assert.Equal(TransferStatus.Requested, transfer.Status);
    }

    [Fact]
    public async Task CreateTransfer_SameBranches_Returns400ProblemDetails()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (_, _, transfersCtrl) = CreateControllers(context);

        var request = new CreateStockTransferRequest(
            FromBranchId: 1,
            ToBranchId: 1,
            Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
        );

        var result = await transfersCtrl.CreateTransfer(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("cannot be the same", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateTransfer_NonExistentBranch_Returns404ProblemDetails()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (_, _, transfersCtrl) = CreateControllers(context);

        var request = new CreateStockTransferRequest(
            FromBranchId: 999,
            ToBranchId: 2,
            Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
        );

        var result = await transfersCtrl.CreateTransfer(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("does not exist", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DispatchTransfer_InsufficientStock_Returns400ProblemDetails()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (_, _, transfersCtrl) = CreateControllers(context);

        var createResult = await transfersCtrl.CreateTransfer(new CreateStockTransferRequest(
            FromBranchId: 1,
            ToBranchId: 2,
            Lines: new[] { new CreateStockTransferLineRequest(1, 9999) }
        ), CancellationToken.None);

        var created = Assert.IsType<StockTransferDto>(((CreatedAtActionResult)createResult.Result!).Value);

        var dispatchResult = await transfersCtrl.DispatchTransfer(created.Id, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(dispatchResult.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Insufficient stock", problem.Detail);
    }

    [Fact]
    public async Task ReceiveTransfer_FromRequestedStatus_Returns409ConflictProblemDetails()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (_, _, transfersCtrl) = CreateControllers(context);

        var createResult = await transfersCtrl.CreateTransfer(new CreateStockTransferRequest(
            FromBranchId: 1,
            ToBranchId: 2,
            Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
        ), CancellationToken.None);

        var created = Assert.IsType<StockTransferDto>(((CreatedAtActionResult)createResult.Result!).Value);

        // Attempt receive while in Requested
        var receiveResult = await transfersCtrl.ReceiveTransfer(created.Id, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(receiveResult.Result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Only transfers in 'Dispatched' status can be received", problem.Detail);
    }

    [Fact]
    public async Task CancelTransfer_WhenDispatched_Returns409ConflictProblemDetails()
    {
        using var context = new AppDbContext(_contextOptions);
        await DataSeeder.SeedAsync(context);
        var (_, _, transfersCtrl) = CreateControllers(context);

        var createResult = await transfersCtrl.CreateTransfer(new CreateStockTransferRequest(
            FromBranchId: 1,
            ToBranchId: 2,
            Lines: new[] { new CreateStockTransferLineRequest(1, 10) }
        ), CancellationToken.None);

        var created = Assert.IsType<StockTransferDto>(((CreatedAtActionResult)createResult.Result!).Value);
        await transfersCtrl.DispatchTransfer(created.Id, CancellationToken.None);

        // Attempt cancel while Dispatched
        var cancelResult = await transfersCtrl.CancelTransfer(created.Id, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(cancelResult.Result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("can only be cancelled while in 'Requested' status", problem.Detail);
    }
}
