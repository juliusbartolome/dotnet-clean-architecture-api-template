namespace Template.Application.Catalog.Dtos;

public sealed record GetProductResponse(ProductDto Product, bool CacheHit);
