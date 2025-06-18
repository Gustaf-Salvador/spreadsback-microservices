using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;
using SpreadsBack.CommonServices.Domain.Entities;
using SpreadsBack.CommonServices.Infrastructure.Repositories;
using System.Text.Json;

namespace SpreadsBack.CommonServices.Infrastructure.Messaging;

/// <summary>
/// Interface para processamento de eventos
/// </summary>
public interface IEventProcessor
{
    Task<bool> ProcessEventAsync(string eventType, string eventData, string eventId);
}

/// <summary>
/// Processador base para eventos SQS
/// </summary>
public abstract class BaseSqsEventProcessor
{
    protected readonly ILogger Logger;
    protected readonly IBaseRepository<ProcessedEvent> ProcessedEventRepository;
    protected readonly IBaseRepository<FailedEvent> FailedEventRepository;

    protected BaseSqsEventProcessor(
        ILogger logger,
        IBaseRepository<ProcessedEvent> processedEventRepository,
        IBaseRepository<FailedEvent> failedEventRepository)
    {
        Logger = logger;
        ProcessedEventRepository = processedEventRepository;
        FailedEventRepository = failedEventRepository;
    }

    public async Task ProcessSqsEventAsync(SQSEvent sqsEvent)
    {
        foreach (var record in sqsEvent.Records)
        {
            try
            {
                var messageId = record.MessageId;
                
                // Verificar se o evento já foi processado
                var existingProcessedEvent = await ProcessedEventRepository
                    .FindAsync(e => e.EventId == messageId);
                
                if (existingProcessedEvent.Any())
                {
                    Logger.LogInformation("Event {EventId} already processed, skipping", messageId);
                    continue;
                }

                // Processar o evento
                var success = await ProcessSingleEventAsync(record);

                if (success)
                {
                    // Marcar como processado
                    await ProcessedEventRepository.AddAsync(new ProcessedEvent
                    {
                        EventId = messageId,
                        EventType = GetEventType(record),
                        ProcessedAt = DateTime.UtcNow
                    });

                    Logger.LogInformation("Successfully processed event {EventId}", messageId);
                }
                else
                {
                    await HandleFailedEventAsync(record, "Processing failed");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing SQS record {MessageId}", record.MessageId);
                await HandleFailedEventAsync(record, ex.Message);
            }
        }
    }

    /// <summary>
    /// Processa um único evento
    /// </summary>
    protected abstract Task<bool> ProcessSingleEventAsync(SQSEvent.SQSMessage message);

    /// <summary>
    /// Determina o tipo do evento
    /// </summary>
    protected virtual string GetEventType(SQSEvent.SQSMessage message)
    {
        // Implementação padrão - pode ser sobrescrita
        return message.Attributes?.GetValueOrDefault("MessageGroupId") ?? "Unknown";
    }

    /// <summary>
    /// Trata eventos que falharam
    /// </summary>
    private async Task HandleFailedEventAsync(SQSEvent.SQSMessage message, string errorMessage)
    {
        try
        {
            var existingFailedEvent = await FailedEventRepository
                .FindAsync(e => e.EventId == message.MessageId);

            if (existingFailedEvent.Any())
            {
                // Incrementar contador de retry
                var failedEvent = existingFailedEvent.First();
                failedEvent.RetryCount++;
                failedEvent.LastRetryAt = DateTime.UtcNow;
                failedEvent.ErrorMessage = errorMessage;
                
                await FailedEventRepository.UpdateAsync(failedEvent);
            }
            else
            {
                // Criar novo registro de falha
                await FailedEventRepository.AddAsync(new FailedEvent
                {
                    EventId = message.MessageId,
                    EventType = GetEventType(message),
                    ErrorMessage = errorMessage,
                    EventData = message.Body,
                    RetryCount = 1,
                    FailedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to record failed event {EventId}", message.MessageId);
        }
    }
}

/// <summary>
/// Helper para desserialização de eventos
/// </summary>
public static class EventDeserializer
{
    public static T? DeserializeEvent<T>(string eventData) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(eventData);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
