namespace SpreadsBack.CommonServices.Domain.Entities;

/// <summary>
/// Entidade para rastrear eventos processados (para evitar duplicação)
/// </summary>
public class ProcessedEvent : BaseEntity
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entidade para rastrear eventos que falharam no processamento
/// </summary>
public class FailedEvent : BaseEntity
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRetryAt { get; set; }
}
