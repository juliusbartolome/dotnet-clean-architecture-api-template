using LaunchpadStarter.Domain.Catalog.Events;
using LaunchpadStarter.Domain.Common;

namespace LaunchpadStarter.Domain.Catalog;

public sealed class Product : Entity
{
    private Product()
    {
    }

    private Product(Guid id, string sku, string name, string? description, decimal price, string currency, bool isActive, DateTimeOffset createdAt, DateTimeOffset? updatedAt)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        Currency = currency;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Product Create(string sku, string name, string? description, decimal price, string currency, DateTimeOffset utcNow)
    {
        var product = new Product(Guid.NewGuid(), sku.Trim(), name.Trim(), description?.Trim(), price, currency.Trim().ToUpperInvariant(), true, utcNow, null);
        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id, product.Sku, utcNow));
        return product;
    }

    public void Update(string name, string? description, decimal price, string currency, DateTimeOffset utcNow)
    {
        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        Currency = currency.Trim().ToUpperInvariant();
        UpdatedAt = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = utcNow;
    }

    public static Product Rehydrate(Guid id, string sku, string name, string? description, decimal price, string currency, bool isActive, DateTimeOffset createdAt, DateTimeOffset? updatedAt)
    {
        return new Product(id, sku, name, description, price, currency, isActive, createdAt, updatedAt);
    }
}
