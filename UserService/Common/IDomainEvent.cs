using System;

namespace UserService.Common;

/// <summary>
/// Base interface for all domain events
/// </summary>
public interface IDomainEvent
{
    string EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
    string AggregateId { get; }
    string CorrelationId { get; set; }
    int Version { get; set; }
}