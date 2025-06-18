using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using CheckingAccountsService.Domain.Events;
using CheckingAccountsService.Infrastructure.Logging;
using System.Text.Json;

namespace CheckingAccountsService.Infrastructure.Messaging;

/// <summary>
/// Handler for processing SQS messages
/// </summary>
public class SqsEventHandler
{
    private readonly ILogger<SqsEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventTracker _eventTracker;

    public SqsEventHandler(IServiceProvider serviceProvider, IEventTracker eventTracker, ILogger<SqsEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _eventTracker = eventTracker;
        _logger = logger;
    }

    /// <summary>
    /// Processes a batch of SQS messages
    /// </summary>
    public async Task<SQSBatchResponse> ProcessMessagesAsync(SQSEvent sqsEvent, ILambdaContext context)
    {
        _logger.LogInformation($"Received {sqsEvent.Records.Count} messages to process");
        
        var failedMessageIds = new List<string>();

        // Process each message
        foreach (var message in sqsEvent.Records)
        {
            try
            {
                await ProcessMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message {message.MessageId}: {ex.Message}");
                failedMessageIds.Add(message.MessageId);
            }
        }

        // Return the batch response with any failed message IDs
        return new SQSBatchResponse
        {
            BatchItemFailures = failedMessageIds.Select(id => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = id }).ToList()
        };
    }

    /// <summary>
    /// Processes a single SQS message
    /// </summary>
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message)
    {
        // Extract the event type from message attributes
        string eventType = "Unknown";
        if (message.MessageAttributes.TryGetValue("EventType", out var eventTypeAttribute))
        {
            eventType = eventTypeAttribute.StringValue;
        }

        _logger.LogInformation($"Processing message of type {eventType}");

        // Deserialize the event based on its type
        DomainEvent? domainEvent = null;
        
        try
        {
            // Process based on event type
            switch (eventType)
            {
                case nameof(WithdrawalCompletedEvent):
                    domainEvent = JsonSerializer.Deserialize<WithdrawalCompletedEvent>(message.Body);
                    break;
                    
                case nameof(WithdrawalRejectedEvent):
                    domainEvent = JsonSerializer.Deserialize<WithdrawalRejectedEvent>(message.Body);
                    break;
                    
                default:
                    _logger.LogWarning($"Unknown event type: {eventType}");
                    return;
            }

            if (domainEvent == null)
            {
                _logger.LogWarning($"Failed to deserialize event of type {eventType}");
                return;
            }

            // Check if the event has already been processed
            if (await _eventTracker.HasBeenProcessedAsync(domainEvent))
            {
                _logger.LogInformation($"Event {domainEvent.Id} has already been processed, skipping");
                return;
            }

            // Process the event based on its type
            switch (eventType)
            {
                case nameof(WithdrawalCompletedEvent):
                    await ProcessWithdrawalCompletedEventAsync((WithdrawalCompletedEvent)domainEvent);
                    break;
                    
                case nameof(WithdrawalRejectedEvent):
                    await ProcessWithdrawalRejectedEventAsync((WithdrawalRejectedEvent)domainEvent);
                    break;
            }

            // Mark the event as processed
            await _eventTracker.MarkAsProcessedAsync(domainEvent);
        }
        catch (Exception ex)
        {
            if (domainEvent != null)
            {
                // Record the failure
                await _eventTracker.RecordFailureAsync(domainEvent, ex.Message);
            }
            throw; // Rethrow to trigger SQS retry mechanism
        }
    }

    /// <summary>
    /// Processes a WithdrawalCompletedEvent
    /// </summary>
    private async Task ProcessWithdrawalCompletedEventAsync(WithdrawalCompletedEvent withdrawalEvent)
    {
        _logger.LogInformation($"Processing completed withdrawal for user {withdrawalEvent.UserId}, amount: {withdrawalEvent.Amount} {withdrawalEvent.CurrencyId}");
        
        // Here you would implement your business logic for processing a completed withdrawal
        // For example, you might:
        // - Send a notification to the user
        // - Update analytics data
        // - Trigger other downstream processes
        
        await Task.CompletedTask; // Placeholder for actual async operations
    }

    /// <summary>
    /// Processes a WithdrawalRejectedEvent
    /// </summary>
    private async Task ProcessWithdrawalRejectedEventAsync(WithdrawalRejectedEvent rejectedEvent)
    {
        _logger.LogInformation($"Processing rejected withdrawal for user {rejectedEvent.UserId}, amount: {rejectedEvent.Amount} {rejectedEvent.CurrencyId}, reason: {rejectedEvent.Reason}");
        
        // Here you would implement your business logic for handling a rejected withdrawal
        // For example, you might:
        // - Log the rejection for compliance purposes
        // - Send a notification to the user
        // - Alert administrators if there are unusual patterns
        
        await Task.CompletedTask; // Placeholder for actual async operations
    }
}