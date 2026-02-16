using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LaunchpadStarter.Application.Catalog.Dtos;
using LaunchpadStarter.Application.Common.Abstractions;
using LaunchpadStarter.Application.Common.Extensions;
using LaunchpadStarter.Application.Common.Models;
using LaunchpadStarter.Domain.Catalog;

namespace LaunchpadStarter.Application.Catalog.Commands.CreateProduct;

public sealed record CreateProductCommand(string Sku, string Name, string? Description, decimal Price, string Currency)
    : IRequest<Result<ProductDto>>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Z0-9_-]+$");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Description)
            .MaximumLength(2048);

        RuleFor(x => x.Price)
            .GreaterThan(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$");
    }
}

public sealed class CreateProductCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ICacheVersionService cacheVersionService)
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Products.AnyAsync(x => x.Sku == request.Sku, cancellationToken);
        if (exists)
        {
            return Result<ProductDto>.Failure(Error.Conflict($"Product with SKU '{request.Sku}' already exists."));
        }

        var product = Product.Create(request.Sku, request.Name, request.Description, request.Price, request.Currency, DateTimeOffset.UtcNow);

        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.ProductById(product.Id), cancellationToken);
        await cacheVersionService.BumpVersionAsync(CacheKeys.CatalogSearchVersion, cancellationToken);

        return Result<ProductDto>.Success(product.ToDto());
    }
}
