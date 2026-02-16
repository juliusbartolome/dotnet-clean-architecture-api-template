using Template.Domain.Common;

namespace Template.Domain.Catalog.Events;

public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, DateTimeOffset OccurredAt)
    : DomainEvent(OccurredAt);
