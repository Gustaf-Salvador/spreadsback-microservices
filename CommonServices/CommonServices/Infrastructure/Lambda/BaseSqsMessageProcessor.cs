using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpreadsBack.CommonServices.Infrastructure.EventTracking;
using System.Text.Json;

namespace SpreadsBack.CommonServices.Infrastructure.Lambda;

/// <summary>
/// Processador base para mensagens SQS em Lambda
/// </summary>
public abstract class BaseSqsMessageProcessor
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;

    protected BaseSqsMessageProcessor(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Logger = serviceProvider.GetRequiredService<ILogger<BaseSqsMessageProcessor>>();
    }

    /// <summary>
    /// Handler principal para processar eventos SQS
    /// </summary>
    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        Logger.LogInformation("Received {MessageCount} messages to process", sqsEvent.Records.Count);
        
        var failedMessageIds = new List<string>();

        foreach (var message in sqsEvent.Records)
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                await ProcessMessageAsync(message, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing message {MessageId}: {Error}", 
                    message.MessageId, ex.Message);
                failedMessageIds.Add(message.MessageId);
            }
        }

        return new SQSBatchResponse
        {
            BatchItemFailures = failedMessageIds
                .Select(id => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = id })
                .ToList()
        };
    }

    /// <summary>
    /// Processa uma única mensagem SQS
    /// </summary>
    protected virtual async Task ProcessMessageAsync(SQSEvent.SQSMessage message, IServiceProvider scopedProvider)
    {
        var eventTracker = scopedProvider.GetRequiredService<IEventTracker>();
        
        // Extrair tipo do evento
        var eventType = ExtractEventType(message);
        Logger.LogInformation("Processing message of type {EventType}", eventType);

        // Verificar se já foi processado
        var eventId = message.MessageId;
        if (await IsAlreadyProcessedAsync(eventTracker, eventId))
        {
            Logger.LogInformation("Event {EventId} already processed, skipping", eventId);
            return;
        }

        try
        {
            // Processar o evento específico
            var success = await ProcessEventAsync(message, eventType, scopedProvider);

            if (success)
            {
                await MarkAsProcessedAsync(eventTracker, message, eventType);
                Logger.LogInformation("Successfully processed event {EventId}", eventId);
            }
            else
            {
                await MarkAsFailedAsync(eventTracker, message, eventType, "Processing returned false");
            }
        }
        catch (Exception ex)
        {
            await MarkAsFailedAsync(eventTracker, message, eventType, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Implementa o processamento específico do evento
    /// </summary>
    protected abstract Task<bool> ProcessEventAsync(
        SQSEvent.SQSMessage message, 
        string eventType, 
        IServiceProvider scopedProvider);

    /// <summary>
    /// Extrai o tipo do evento da mensagem
    /// </summary>
    protected virtual string ExtractEventType(SQSEvent.SQSMessage message)
    {
        if (message.MessageAttributes.TryGetValue("EventType", out var eventTypeAttribute))
        {
            return eventTypeAttribute.StringValue;
        }

        return "Unknown";
    }

    /// <summary>
    /// Verifica se o evento já foi processado
    /// </summary>
    protected virtual async Task<bool> IsAlreadyProcessedAsync(IEventTracker eventTracker, string eventId)
    {
        // Criar um objeto mock para verificação
        var mockEvent = new { Id = eventId };
        return await eventTracker.HasBeenProcessedAsync(mockEvent);
    }

    /// <summary>
    /// Marca evento como processado
    /// </summary>
    protected virtual async Task MarkAsProcessedAsync(
        IEventTracker eventTracker, 
        SQSEvent.SQSMessage message, 
        string eventType)
    {
        var mockEvent = new { Id = message.MessageId, EventType = eventType };
        await eventTracker.MarkAsProcessedAsync(mockEvent);
    }

    /// <summary>
    /// Marca evento como falhou
    /// </summary>
    protected virtual async Task MarkAsFailedAsync(
        IEventTracker eventTracker, 
        SQSEvent.SQSMessage message, 
        string eventType, 
        string errorMessage)
    {
        var mockEvent = new { Id = message.MessageId, EventType = eventType };
        await eventTracker.MarkAsFailedAsync(mockEvent, errorMessage);
    }

    /// <summary>
    /// Helper para deserializar eventos
    /// </summary>
    protected T? DeserializeEvent<T>(string eventData) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(eventData);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to deserialize event data as {EventType}", typeof(T).Name);
            return null;
        }
    }
}
