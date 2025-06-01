using System;

namespace UserService.Common;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventId { get; } = Guid.NewGuid().ToString();
}