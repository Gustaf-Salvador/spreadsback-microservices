using CheckingAccountsService.Domain.Events;

namespace CheckingAccountsService.Domain.Services;

/// <summary>
/// Interface for publishing domain events to external message brokers
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to the appropriate topic
    /// </summary>
    /// <param name="domainEvent">The domain event to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync(DomainEvent domainEvent);
}