using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SpreadsBack.CommonServices.Infrastructure.Messaging;

/// <summary>
/// Configurações do SNS
/// </summary>
public class SnsSettings
{
    public string TopicArn { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

/// <summary>
/// Interface para publicação de eventos
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData, string eventType) where T : class;
    Task PublishAsync<T>(T eventData) where T : class;
}

/// <summary>
/// Implementação base do event publisher usando SNS
/// </summary>
public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly SnsSettings _settings;
    private readonly ILogger<SnsEventPublisher> _logger;

    public SnsEventPublisher(
        IAmazonSimpleNotificationService snsClient,
        SnsSettings settings,
        ILogger<SnsEventPublisher> logger)
    {
        _snsClient = snsClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T eventData, string eventType) where T : class
    {
        try
        {
            var message = JsonSerializer.Serialize(eventData);

            var request = new PublishRequest
            {
                TopicArn = _settings.TopicArn,
                Message = message,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "EventType", new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = eventType
                        }
                    }
                }
            };

            var response = await _snsClient.PublishAsync(request);
            
            _logger.LogInformation("Published event {EventType} with MessageId {MessageId}", 
                eventType, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", eventType);
            throw;
        }
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        var eventType = typeof(T).Name;
        await PublishAsync(eventData, eventType);
    }
}
