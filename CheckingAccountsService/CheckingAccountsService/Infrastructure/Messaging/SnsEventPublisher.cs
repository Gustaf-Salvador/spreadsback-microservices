using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CheckingAccountsService.Domain.Events;
using CheckingAccountsService.Domain.Services;
using CheckingAccountsService.Infrastructure.Logging;
using System.Text.Json;

namespace CheckingAccountsService.Infrastructure.Messaging;

/// <summary>
/// Settings for the SNS Event Publisher
/// </summary>
public class SnsSettings
{
    public string TopicArn { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}

/// <summary>
/// Implementation of IEventPublisher that publishes events to an SNS topic
/// </summary>
public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _topicArn;
    private readonly ILogger<SnsEventPublisher> _logger;

    public SnsEventPublisher(
        IAmazonSimpleNotificationService snsClient, 
        SnsSettings settings,
        ILogger<SnsEventPublisher> logger)
    {
        _snsClient = snsClient;
        _topicArn = settings.TopicArn;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a domain event to the SNS topic
    /// </summary>
    public async Task PublishAsync(DomainEvent domainEvent)
    {
        try
        {
            var eventJson = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new PublishRequest
            {
                TopicArn = _topicArn,
                Message = eventJson,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "EventType", new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = domainEvent.EventType
                        }
                    }
                }
            };

            var response = await _snsClient.PublishAsync(request);
            _logger.LogInformation($"Published event {domainEvent.EventType} with message ID: {response.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing event {domainEvent.EventType}: {ex.Message}");
            throw;
        }
    }
}