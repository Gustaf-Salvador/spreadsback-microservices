using Microsoft.EntityFrameworkCore;
using SpreadsBack.CommonServices.Domain.Entities;
using SpreadsBack.CommonServices.Infrastructure.Persistence;

namespace SpreadsBack.CommonServices.Infrastructure.EventTracking;

/// <summary>
/// Interface para tracking de eventos
/// </summary>
public interface IEventTracker
{
    Task<bool> HasBeenProcessedAsync<T>(T domainEvent) where T : class;
    Task MarkAsProcessedAsync<T>(T domainEvent) where T : class;
    Task MarkAsFailedAsync<T>(T domainEvent, string errorMessage) where T : class;
}

/// <summary>
/// Implementação base do event tracker usando Entity Framework
/// </summary>
public class EfEventTracker : IEventTracker
{
    private readonly BaseDbContext _context;

    public EfEventTracker(BaseDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasBeenProcessedAsync<T>(T domainEvent) where T : class
    {
        var eventId = GetEventId(domainEvent);
        if (string.IsNullOrEmpty(eventId))
            return false;

        return await _context.ProcessedEvents
            .AnyAsync(e => e.EventId == eventId);
    }

    public async Task MarkAsProcessedAsync<T>(T domainEvent) where T : class
    {
        var eventId = GetEventId(domainEvent);
        if (string.IsNullOrEmpty(eventId))
            return;

        var processedEvent = new ProcessedEvent
        {
            EventId = eventId,
            EventType = typeof(T).Name,
            ProcessedAt = DateTime.UtcNow
        };

        _context.ProcessedEvents.Add(processedEvent);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsFailedAsync<T>(T domainEvent, string errorMessage) where T : class
    {
        var eventId = GetEventId(domainEvent);
        if (string.IsNullOrEmpty(eventId))
            return;

        var existingFailedEvent = await _context.FailedEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (existingFailedEvent != null)
        {
            existingFailedEvent.RetryCount++;
            existingFailedEvent.LastRetryAt = DateTime.UtcNow;
            existingFailedEvent.ErrorMessage = errorMessage;
        }
        else
        {
            var failedEvent = new FailedEvent
            {
                EventId = eventId,
                EventType = typeof(T).Name,
                ErrorMessage = errorMessage,
                EventData = System.Text.Json.JsonSerializer.Serialize(domainEvent),
                RetryCount = 1,
                FailedAt = DateTime.UtcNow
            };

            _context.FailedEvents.Add(failedEvent);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Extrai o ID do evento usando reflexão
    /// </summary>
    private string GetEventId<T>(T domainEvent) where T : class
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(domainEvent);
            return value?.ToString() ?? string.Empty;
        }

        var eventIdProperty = typeof(T).GetProperty("EventId");
        if (eventIdProperty != null)
        {
            var value = eventIdProperty.GetValue(domainEvent);
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}
