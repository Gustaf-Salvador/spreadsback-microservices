using CheckingAccountsService.Domain.Events;

namespace CheckingAccountsService.Infrastructure.Messaging;

/// <summary>
/// Service for tracking processed events to avoid duplicates
/// </summary>
public interface IEventTracker
{
    /// <summary>
    /// Checks if an event has already been processed
    /// </summary>
    Task<bool> HasBeenProcessedAsync(DomainEvent domainEvent);
    
    /// <summary>
    /// Marks an event as processed
    /// </summary>
    Task MarkAsProcessedAsync(DomainEvent domainEvent);
    
    /// <summary>
    /// Records a failed event processing attempt
    /// </summary>
    Task RecordFailureAsync(DomainEvent domainEvent, string failureReason);
}