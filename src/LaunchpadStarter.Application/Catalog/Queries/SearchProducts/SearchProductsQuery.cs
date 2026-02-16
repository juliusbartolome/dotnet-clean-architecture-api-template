using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LaunchpadStarter.Application.Catalog.Dtos;
using LaunchpadStarter.Application.Common.Abstractions;
using LaunchpadStarter.Application.Common.Extensions;
using LaunchpadStarter.Application.Common.Models;

namespace LaunchpadStarter.Application.Catalog.Queries.SearchProducts;

public sealed record SearchProductsQuery(
    bool? IsActive,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Query,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<ProductSearchResponse>>;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice.Value <= x.MaxPrice.Value)
            .WithMessage("minPrice must be less than or equal to maxPrice.");
    }
}

public sealed class SearchProductsQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ICacheVersionService cacheVersionService)
    : IRequestHandler<SearchProductsQuery, Result<ProductSearchResponse>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private sealed record CachedSearchResult(IReadOnlyCollection<ProductDto> Items, int TotalCount, int Page, int PageSize);

    public async Task<Result<ProductSearchResponse>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var version = await cacheVersionService.GetVersionAsync(CacheKeys.CatalogSearchVersion, cancellationToken);
        var cacheKey = CacheKeys.SearchProducts(version, request.IsActive, request.MinPrice, request.MaxPrice, request.Query, request.Page, request.PageSize);

        var cached = await cacheService.GetAsync<CachedSearchResult>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<ProductSearchResponse>.Success(new ProductSearchResponse(cached.Items, cached.TotalCount, cached.Page, cached.PageSize, true));
        }

        var query = dbContext.Products.AsNoTracking().AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= request.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(x => x.Name.Contains(q) || (x.Description != null && x.Description.Contains(q)) || x.Sku.Contains(q));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        var payload = new CachedSearchResult(items, totalCount, request.Page, request.PageSize);
        await cacheService.SetAsync(cacheKey, payload, CacheTtl, cancellationToken);

        return Result<ProductSearchResponse>.Success(new ProductSearchResponse(items, totalCount, request.Page, request.PageSize, false));
    }
}
