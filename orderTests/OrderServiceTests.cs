using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using backend.Data;
using backend.Domain;
using backend.Services;
using backend.Services.DTOs;
using Mapster;
using MapsterMapper;
using Xunit;

namespace orderTests;

public class OrderServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<OrderService>> _loggerMock = new();
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _cacheMock.Setup(x => x.GetAsync<IEnumerable<OrderResponse>>(It.IsAny<string>()))
                  .ReturnsAsync((IEnumerable<OrderResponse>)null!);

        var config = new TypeAdapterConfig();
        new OrderMapper().Register(config);
        _mapper = new Mapper(config);
    }

    private OrderService CreateService() =>
        new(_context, _cacheMock.Object, _loggerMock.Object, _timeProvider, _mapper);

    [Fact]
    public async Task GetOrder_IfInCache_ShouldReturnFromCache()
    {
        var orderId = Guid.NewGuid();
        var cachedOrder = new OrderResponse(orderId, "Pending", "Phone", "Street", 100m, DateTime.UtcNow);
        _cacheMock.Setup(c => c.GetAsync<OrderResponse>(It.IsAny<string>())).ReturnsAsync(cachedOrder);

        var result = await CreateService().GetOrderAsync(orderId);

        Assert.True(result.IsSuccess);
        Assert.Equal(cachedOrder.Products, result.Value!.Products);
    }

    [Fact]
    public async Task GetOrder_NotInCache_ShouldFetchFromDbAndCacheIt()
    {
        var order = new Order { Id = Guid.NewGuid(), Products = "P", ShippingAddress = "A", UserId = Guid.NewGuid() };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await CreateService().GetOrderAsync(order.Id);

        Assert.True(result.IsSuccess);
        _cacheMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<OrderResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetOrder_WhenCacheFails_ReturnsFailure()
    {
        _cacheMock.Setup(c => c.GetAsync<OrderResponse>(It.IsAny<string>())).ThrowsAsync(new Exception("Redis down"));

        var result = await CreateService().GetOrderAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ShouldSaveToDbAndInvalidateList()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), "Laptop", "NY", 1500m);
        _cacheMock.Setup(x => x.GetStringAsync(It.IsAny<string>())).ReturnsAsync("1");

        var result = await CreateService().CreateOrderAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await _context.Orders.CountAsync());

        _cacheMock.Verify(x => x.SetRawAsync(It.IsAny<string>(), "2", null), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAddress_WhenStatusIsPending_ShouldSucceedAndInvalidateCache()
    {
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending, ShippingAddress = "Old", Products = "P", UserId = userId };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        _cacheMock.Setup(x => x.GetStringAsync(It.IsAny<string>())).ReturnsAsync("1");

        var result = await CreateService().UpdateAddressAsync(order.Id, new UpdateOrderAddressRequest("New"));

        Assert.True(result.IsSuccess);
        _cacheMock.Verify(x => x.SetRawAsync(It.IsAny<string>(), "2", null), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAddress_WhenStatusIsPending_ShouldSucceed()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending, ShippingAddress = "Old", Products = "P" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await CreateService().UpdateAddressAsync(order.Id, new UpdateOrderAddressRequest("New"));

        Assert.True(result.IsSuccess);
        var updated = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("New", updated!.ShippingAddress);
    }

    [Fact]
    public async Task UpdateAddress_OrderNotFound_ReturnsNotFound()
    {
        var result = await CreateService().UpdateAddressAsync(Guid.NewGuid(), new UpdateOrderAddressRequest("New"));
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CancelOrder_WhenStatusIsDelivered_ReturnsConflict()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Delivered, Products = "P", ShippingAddress = "A" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await CreateService().CancelOrderAsync(order.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CancelOrder_WhenAlreadyCancelled_ReturnsConflict()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Cancelled, Products = "P", ShippingAddress = "A" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await CreateService().CancelOrderAsync(order.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task GetOrders_SortByAmountDescending_ReturnsCorrectSequence()
    {
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), TotalAmount = 10, Products = "P", ShippingAddress = "A", CreatedAt = DateTime.UtcNow });
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), TotalAmount = 100, Products = "P", ShippingAddress = "A", CreatedAt = DateTime.UtcNow.AddMinutes(1) });
        await _context.SaveChangesAsync();

        var result = await CreateService().GetOrdersAsync(1, 10, sortBy: "amount", isDescending: true);

        Assert.Equal(100, result.Value!.First().TotalAmount);
    }

    [Fact]
    public async Task GetOrders_FilterByUserId_ReturnsOnlyUserOrders()
    {
        var userId = Guid.NewGuid();
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), UserId = userId, Products = "P", ShippingAddress = "A", CreatedAt = DateTime.UtcNow });
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Products = "P", ShippingAddress = "A", CreatedAt = DateTime.UtcNow.AddMinutes(1) });
        await _context.SaveChangesAsync();

        var result = await CreateService().GetOrdersAsync(1, 10, userId: userId);

        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetOrders_WithPagination_ReturnsCorrectCount()
    {
        for (int i = 0; i < 15; i++)
        {
            _context.Orders.Add(new Order { Id = Guid.NewGuid(), Products = "P", ShippingAddress = "A", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
        }
        await _context.SaveChangesAsync();

        var result = await CreateService().GetOrdersAsync(page: 2, limit: 10);

        Assert.Equal(5, result.Value!.Count());
    }

    [Fact]
    public async Task UpdateAddress_WhenStatusIsAtLimit_ReturnsConflict()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.InTransit, Products = "P", ShippingAddress = "A" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await CreateService().UpdateAddressAsync(order.Id, new UpdateOrderAddressRequest("New"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task GetOrders_SortByStatusAscending_ReturnsCorrectSequence()
    {
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), Status = OrderStatus.Delivered, Products = "P", ShippingAddress = "A" });
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending, Products = "P", ShippingAddress = "A" });
        await _context.SaveChangesAsync();

        var result = await CreateService().GetOrdersAsync(1, 10, sortBy: "status", isDescending: false);

        Assert.Equal("Pending", result.Value!.First().Status);
    }

    [Fact]
    public async Task CancelOrder_OrderNotFound_ReturnsNotFound()
    {
        var result = await CreateService().CancelOrderAsync(Guid.NewGuid());
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetOrders_DefaultSort_ReturnsByDateDescending()
    {
        var oldDate = DateTime.UtcNow.AddDays(-1);
        var newDate = DateTime.UtcNow;
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), CreatedAt = oldDate, Products = "P", ShippingAddress = "A" });
        _context.Orders.Add(new Order { Id = Guid.NewGuid(), CreatedAt = newDate, Products = "P", ShippingAddress = "A" });
        await _context.SaveChangesAsync();

        var result = await CreateService().GetOrdersAsync(1, 10);

        Assert.Equal(newDate, result.Value!.First().CreatedAt);
    }

    public void Dispose() => _context.Dispose();
}