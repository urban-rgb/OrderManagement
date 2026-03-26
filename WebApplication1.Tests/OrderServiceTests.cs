using Moq;
using Xunit;
using WebApplication1.Domain;
using WebApplication1.Services;
using Microsoft.Extensions.Caching.Distributed;

public class OrderServiceTests
{
    [Fact]
    public async Task GetOrder_IfNotFound_ShouldThrowException()
    {
        // Arrange
        var repoMock = new Mock<IOrderRepository>();
        var cacheMock = new Mock<IDistributedCache>();

        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Order)null!);

        var service = new OrderService(repoMock.Object, cacheMock.Object);

        // Act and Assert
        await Assert.ThrowsAsync<KeyNotFoundDomainException>(() =>
            service.GetOrderAsync(Guid.NewGuid()));
    }
}