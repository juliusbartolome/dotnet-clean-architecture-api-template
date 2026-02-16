using FluentAssertions;
using Moq;
using Template.Application.Catalog.Commands.CreateProduct;
using Template.Application.Common.Abstractions;
using Template.Application.Common.Extensions;
using Template.UnitTests.Common;

namespace Template.UnitTests.Catalog.Commands;

public sealed class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenRequestIsValid()
    {
        using var dbContext = new TestApplicationDbContext(Guid.NewGuid().ToString());

        var cacheService = new Mock<ICacheService>();
        var cacheVersionService = new Mock<ICacheVersionService>();

        cacheVersionService
            .Setup(x => x.BumpVersionAsync(CacheKeys.CatalogSearchVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync("v2");

        var handler = new CreateProductCommandHandler(dbContext, cacheService.Object, cacheVersionService.Object);
        var command = new CreateProductCommand("SKU_123", "Premium Plan", "SaaS tier", 49.99m, "USD");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().Be("SKU_123");
        result.Value.Name.Should().Be("Premium Plan");

        cacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cacheVersionService.Verify(x => x.BumpVersionAsync(CacheKeys.CatalogSearchVersion, It.IsAny<CancellationToken>()), Times.Once);
    }
}
