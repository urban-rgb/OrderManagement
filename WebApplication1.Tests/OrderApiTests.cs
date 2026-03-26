using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using WebApplication1.Services.DTOs;
using Xunit;

public class OrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public OrderApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_NegativeAmount_ReturnsError()
    {
        // Arrange
        var badRequest = new CreateOrderRequest("Товар", "Адрес", -10m);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", badRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}