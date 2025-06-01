using System;

namespace UserService.Common;

/// <summary>
/// Base interface for all domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    string EventId { get; }
}