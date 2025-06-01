using UserService.Common;
using UserService.Models;
using UserService.Services;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly List<DomainEvent> _domainEvents = new();
    private bool _disposed;

    public UnitOfWork(
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher,
        ILogger<UnitOfWork> logger)
    {
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public IUserRepository Users => _userRepository;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In DynamoDB, we don't have traditional transactions like SQL
            // But we can use the domain events to maintain consistency
            await _eventDispatcher.PublishAsync(_domainEvents, cancellationToken);
            
            _logger.LogInformation("Successfully saved changes and published {EventCount} domain events", 
                _domainEvents.Count);
            
            var eventCount = _domainEvents.Count;
            ClearDomainEvents();
            
            return eventCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes and publish domain events");
            throw;
        }
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // DynamoDB doesn't support traditional transactions in the same way
        // This is mainly for interface compatibility
        _logger.LogDebug("Beginning unit of work transaction context");
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(cancellationToken).ContinueWith(t => t.Result, cancellationToken);
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Rolling back unit of work - clearing domain events");
        ClearDomainEvents();
        return Task.CompletedTask;
    }

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
        _logger.LogDebug("Added domain event {EventType} with ID {EventId}", 
            domainEvent.EventType, domainEvent.EventId);
    }

    public IReadOnlyList<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void Dispose()
    {
        if (!_disposed)
        {
            ClearDomainEvents();
            _disposed = true;
        }
    }
}
