using Template.Domain.Catalog;

namespace Template.Application.Catalog.Dtos;

internal static class ProductMappings
{
    public static ProductDto ToDto(this Product product)
        => new(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Price,
            product.Currency,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
}
