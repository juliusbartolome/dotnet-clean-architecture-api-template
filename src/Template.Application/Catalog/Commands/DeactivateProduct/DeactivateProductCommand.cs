using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Abstractions;
using Template.Application.Common.Extensions;
using Template.Application.Common.Models;

namespace Template.Application.Catalog.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest<Result>;

public sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public sealed class DeactivateProductCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ICacheVersionService cacheVersionService)
    : IRequestHandler<DeactivateProductCommand, Result>
{
    public async Task<Result> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        product.Deactivate(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.ProductById(product.Id), cancellationToken);
        await cacheVersionService.BumpVersionAsync(CacheKeys.CatalogSearchVersion, cancellationToken);

        return Result.Success();
    }
}
