namespace Template.Application.Catalog.Dtos;

public sealed record ProductSearchResponse(
    IReadOnlyCollection<ProductDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool CacheHit);
