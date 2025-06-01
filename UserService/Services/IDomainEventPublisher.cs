using UserService.Common;

namespace UserService.Services;

/// <summary>
/// Defines a service for publishing domain events
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
}