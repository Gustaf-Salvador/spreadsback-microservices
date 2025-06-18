namespace CheckingAccountsService.Domain.Events;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent
{
    public Guid Id { get; }
    public DateTime Timestamp { get; }
    public string EventType { get; }

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        EventType = GetType().Name;
    }
}