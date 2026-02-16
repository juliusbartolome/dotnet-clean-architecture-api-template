namespace Template.Domain.Common;

public abstract record DomainEvent(DateTimeOffset OccurredAt);
