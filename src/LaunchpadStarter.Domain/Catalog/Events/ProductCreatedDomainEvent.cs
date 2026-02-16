using LaunchpadStarter.Domain.Common;

namespace LaunchpadStarter.Domain.Catalog.Events;

public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, DateTimeOffset OccurredAt)
    : DomainEvent(OccurredAt);
