using UserService.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        _logger.LogInformation("Publishing domain event {EventType} with ID {EventId}", 
            domainEvent.EventType, domainEvent.EventId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<T>>();

            var tasks = handlers.Select(handler => HandleSafely(handler, domainEvent, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully published domain event {EventType} to {HandlerCount} handlers", 
                domainEvent.EventType, handlers.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event {EventType} with ID {EventId}", 
                domainEvent.EventType, domainEvent.EventId);
            throw;
        }
    }

    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        if (!events.Any()) return;

        _logger.LogInformation("Publishing {EventCount} domain events", events.Count);

        try
        {
            var tasks = events.Select(evt => PublishDynamicAsync(evt, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully published all {EventCount} domain events", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain events batch");
            throw;
        }
    }

    private async Task PublishDynamicAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var method = typeof(DomainEventDispatcher).GetMethod(nameof(PublishAsync), new[] { eventType, typeof(CancellationToken) });
        var genericMethod = method?.MakeGenericMethod(eventType);
        
        if (genericMethod != null)
        {
            var task = (Task?)genericMethod.Invoke(this, new object[] { domainEvent, cancellationToken });
            if (task != null)
                await task;
        }
    }

    private async Task HandleSafely<T>(IDomainEventHandler<T> handler, T domainEvent, CancellationToken cancellationToken) where T : DomainEvent
    {
        try
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
            _logger.LogDebug("Handler {HandlerType} successfully processed event {EventType}", 
                handler.GetType().Name, domainEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler {HandlerType} failed to process event {EventType} with ID {EventId}", 
                handler.GetType().Name, domainEvent.EventType, domainEvent.EventId);
            // Don't rethrow - let other handlers continue
        }
    }
}
