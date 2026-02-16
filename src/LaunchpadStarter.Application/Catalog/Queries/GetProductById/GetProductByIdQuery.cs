using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LaunchpadStarter.Application.Catalog.Dtos;
using LaunchpadStarter.Application.Common.Abstractions;
using LaunchpadStarter.Application.Common.Extensions;
using LaunchpadStarter.Application.Common.Models;

namespace LaunchpadStarter.Application.Catalog.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<Result<GetProductResponse>>;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public sealed class GetProductByIdQueryHandler(IApplicationDbContext dbContext, ICacheService cacheService)
    : IRequestHandler<GetProductByIdQuery, Result<GetProductResponse>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<Result<GetProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ProductById(request.ProductId);

        var cached = await cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<GetProductResponse>.Success(new GetProductResponse(cached, true));
        }

        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            return Result<GetProductResponse>.Failure(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        var dto = product.ToDto();
        await cacheService.SetAsync(cacheKey, dto, CacheTtl, cancellationToken);

        return Result<GetProductResponse>.Success(new GetProductResponse(dto, false));
    }
}
