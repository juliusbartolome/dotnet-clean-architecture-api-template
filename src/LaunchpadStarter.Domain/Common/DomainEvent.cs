namespace LaunchpadStarter.Domain.Common;

public abstract record DomainEvent(DateTimeOffset OccurredAt);
