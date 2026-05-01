using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Net.Http.Json;
using WebApplication1.Data;
using WebApplication1.Domain;
using WebApplication1.Services.DTOs;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace WebApplication1.Tests;

[ExcludeFromCodeCoverage]
public class OrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _dbName = Guid.NewGuid().ToString();

    public OrderApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType.FullName!.StartsWith("Microsoft.EntityFrameworkCore") ||
                    d.ServiceType == typeof(IDistributedCache)).ToList();

                foreach (var descriptor in descriptors) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
                services.AddDistributedMemoryCache();
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_EmptyAddress_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), "Products", string.Empty, 100m);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_EmptyProducts_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), string.Empty, "Address", 100m);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_NegativeAmount_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), "Item", "Address", -10m);
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAddress_ExistingOrder_ReturnsNoContent()
    {
        var orderId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Pending, ShippingAddress = "Old", Products = "P", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var response = await _client.HttpPatchAsync($"/api/orders/{orderId}/address",
            JsonContent.Create(new UpdateOrderAddressRequest("New St.")));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_WhenOrderIsDelivered_ReturnsConflict()
    {
        var orderId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Delivered, Products = "P", ShippingAddress = "A", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_ReturnsListWithOk()
    {
        var response = await _client.GetAsync("/api/orders?page=1&limit=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAddress_NonExistentOrder_ReturnsNotFound()
    {
        var response = await _client.HttpPatchAsync($"/api/orders/{Guid.NewGuid()}/address",
            JsonContent.Create(new UpdateOrderAddressRequest("New St.")));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_NonExistentOrder_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithInvalidPage_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/orders?page=999&limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<IEnumerable<OrderResponse>>();
        Assert.Empty(content!);
    }
}

[ExcludeFromCodeCoverage]
public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> HttpPatchAsync(this HttpClient client, string requestUri, HttpContent content)
    {
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
        return await client.SendAsync(request);
    }
}