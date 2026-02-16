using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Template.Application.Catalog.Dtos;
using Template.Application.Common.Abstractions;
using Template.Application.Common.Extensions;
using Template.Application.Common.Models;

namespace Template.Application.Catalog.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid ProductId, string Name, string? Description, decimal Price, string Currency)
    : IRequest<Result<ProductDto>>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(2048);
        RuleFor(x => x.Price).GreaterThan(0m);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
    }
}

public sealed class UpdateProductCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ICacheVersionService cacheVersionService)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<ProductDto>.Failure(Error.NotFound($"Product '{request.ProductId}' was not found."));
        }

        product.Update(request.Name, request.Description, request.Price, request.Currency, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.ProductById(product.Id), cancellationToken);
        await cacheVersionService.BumpVersionAsync(CacheKeys.CatalogSearchVersion, cancellationToken);

        return Result<ProductDto>.Success(product.ToDto());
    }
}
