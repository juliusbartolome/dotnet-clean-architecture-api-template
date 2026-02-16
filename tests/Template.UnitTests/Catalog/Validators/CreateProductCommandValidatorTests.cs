using FluentAssertions;
using Template.Application.Catalog.Commands.CreateProduct;

namespace Template.UnitTests.Catalog.Validators;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldFail_WhenSkuIsInvalid()
    {
        var command = new CreateProductCommand("bad sku", "Product", "desc", 10m, "USD");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "Sku");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPriceIsNotPositive()
    {
        var command = new CreateProductCommand("SKU_1", "Product", "desc", 0m, "USD");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "Price");
    }
}
