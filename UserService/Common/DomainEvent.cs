using System;
using System.Text.Json;

namespace UserService.Common;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType { get; protected set; } = string.Empty;
    public string AggregateId { get; protected set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int Version { get; set; } = 1;

    // Legacy property for backward compatibility
    public string Id => EventId;
    public DateTime OccurredAt => OccurredOn;
    
    // Additional property for compatibility
    public string EntityId => AggregateId;

    protected DomainEvent()
    {
        EventType = GetType().Name;
    }

    public virtual string ToJson()
    {
        return JsonSerializer.Serialize(this, GetType());
    }
}

public interface IDomainEventHandler<in T> where T : DomainEvent
{
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default);
}

public interface IDomainEventDispatcher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent;
    Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}