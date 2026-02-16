using MediatR;
using LaunchpadStarter.Api.Common;
using LaunchpadStarter.Application.Catalog.Commands.CreateProduct;
using LaunchpadStarter.Application.Catalog.Commands.DeactivateProduct;
using LaunchpadStarter.Application.Catalog.Commands.UpdateProduct;
using LaunchpadStarter.Application.Catalog.Queries.GetProductById;
using LaunchpadStarter.Application.Catalog.Queries.SearchProducts;

namespace LaunchpadStarter.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/products").WithTags("Products");

        group.MapPost("", CreateProduct)
            .WithSummary("Create a product")
            .RequireAuthorization("CatalogWrite");

        group.MapGet("/{id:guid}", GetProductById)
            .WithSummary("Get a product by id")
            .AllowAnonymous();

        group.MapGet("", SearchProducts)
            .WithSummary("Search products")
            .AllowAnonymous();

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithSummary("Update a product")
            .RequireAuthorization("CatalogWrite");

        group.MapDelete("/{id:guid}", DeactivateProduct)
            .WithSummary("Deactivate a product")
            .RequireAuthorization("CatalogWrite");

        return endpoints;
    }

    private static async Task<IResult> CreateProduct(CreateProductCommand command, ISender sender, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Results.Created($"/api/v1/products/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> GetProductById(Guid id, ISender sender, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        httpContext.Response.Headers["X-Cache"] = result.Value.CacheHit ? "HIT" : "MISS";
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> SearchProducts(
        bool? isActive,
        decimal? minPrice,
        decimal? maxPrice,
        string? q,
        int? page,
        int? pageSize,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var resolvedPage = page.GetValueOrDefault(1);
        var resolvedPageSize = pageSize.GetValueOrDefault(20);
        var result = await sender.Send(new SearchProductsQuery(isActive, minPrice, maxPrice, q, resolvedPage, resolvedPageSize), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        httpContext.Response.Headers["X-Cache"] = result.Value.CacheHit ? "HIT" : "MISS";
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateProduct(Guid id, UpdateProductRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Currency), cancellationToken);
        return result.IsFailure ? result.ToProblem() : Results.Ok(result.Value);
    }

    private static async Task<IResult> DeactivateProduct(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateProductCommand(id), cancellationToken);
        return result.IsFailure ? result.ToProblem() : Results.NoContent();
    }

    private sealed record UpdateProductRequest(string Name, string? Description, decimal Price, string Currency);
}
