using UserService.Models;
using Microsoft.Extensions.Logging;

namespace UserService.Services;

public class DynamoDbEventStore : IEventStore
{
    private readonly IDynamoDbService _dynamoDbService;
    private readonly ILogger<DynamoDbEventStore> _logger;
    private const string EventTableName = "UserEvents";

    public DynamoDbEventStore(IDynamoDbService dynamoDbService, ILogger<DynamoDbEventStore> logger)
    {
        _dynamoDbService = dynamoDbService;
        _logger = logger;
    }

    public async Task StoreEventAsync(UserEvent userEvent)
    {
        try
        {
            var eventItem = new Dictionary<string, object>
            {
                ["PK"] = $"USER#{userEvent.UserId}",
                ["SK"] = $"EVENT#{userEvent.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}#{userEvent.Id}",
                ["EventId"] = userEvent.Id,
                ["UserId"] = userEvent.UserId,
                ["EventType"] = userEvent.EventType,
                ["Timestamp"] = userEvent.Timestamp.ToString("O"),
                ["Data"] = userEvent.Data,
                ["Version"] = userEvent.Version,
                ["CorrelationId"] = userEvent.CorrelationId,
                ["TTL"] = DateTimeOffset.UtcNow.AddYears(7).ToUnixTimeSeconds() // 7 year retention
            };

            await _dynamoDbService.PutItemAsync(EventTableName, eventItem);
            _logger.LogInformation("Event stored successfully: {EventType} for user {UserId}", 
                userEvent.EventType, userEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store event {EventType} for user {UserId}", 
                userEvent.EventType, userEvent.UserId);
            throw;
        }
    }

    public async Task<List<UserEvent>> GetEventsAsync(string userId)
    {
        try
        {
            var queryExpression = new Dictionary<string, object>
            {
                ["PK"] = $"USER#{userId}"
            };

            var items = await _dynamoDbService.QueryAsync(EventTableName, "PK = :pk", queryExpression);
            
            return items.Select(MapToUserEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve events for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserEvent>> GetEventsByTypeAsync(string eventType, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var filterExpression = "EventType = :eventType";
            var expressionValues = new Dictionary<string, object>
            {
                [":eventType"] = eventType
            };

            if (from.HasValue)
            {
                filterExpression += " AND #timestamp >= :from";
                expressionValues[":from"] = from.Value.ToString("O");
            }

            if (to.HasValue)
            {
                filterExpression += " AND #timestamp <= :to";
                expressionValues[":to"] = to.Value.ToString("O");
            }

            var items = await _dynamoDbService.ScanAsync(EventTableName, filterExpression, expressionValues);
            
            return items.Select(MapToUserEvent).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve events by type {EventType}", eventType);
            throw;
        }
    }

    private static UserEvent MapToUserEvent(Dictionary<string, object> item)
    {
        var eventType = item["EventType"].ToString()!;
        
        return eventType switch
        {
            "UserCreated" => new UserCreatedEvent(new User(), item["CorrelationId"].ToString()!)
            {
                Id = item["EventId"].ToString()!,
                UserId = item["UserId"].ToString()!,
                Timestamp = DateTime.Parse(item["Timestamp"].ToString()!),
                Data = item["Data"].ToString()!,
                Version = item["Version"].ToString()!
            },
            "UserUpdated" => new UserUpdatedEvent(new User(), new Dictionary<string, object>(), item["CorrelationId"].ToString()!)
            {
                Id = item["EventId"].ToString()!,
                UserId = item["UserId"].ToString()!,
                Timestamp = DateTime.Parse(item["Timestamp"].ToString()!),
                Data = item["Data"].ToString()!,
                Version = item["Version"].ToString()!
            },
            "UserStatusChanged" => new UserStatusChangedEvent(item["UserId"].ToString()!, UserStatus.Active, UserStatus.Active, item["CorrelationId"].ToString()!)
            {
                Id = item["EventId"].ToString()!,
                Timestamp = DateTime.Parse(item["Timestamp"].ToString()!),
                Data = item["Data"].ToString()!,
                Version = item["Version"].ToString()!
            },
            "UserEmailVerified" => new UserEmailVerifiedEvent(item["UserId"].ToString()!, item["CorrelationId"].ToString()!)
            {
                Id = item["EventId"].ToString()!,
                Timestamp = DateTime.Parse(item["Timestamp"].ToString()!),
                Data = item["Data"].ToString()!,
                Version = item["Version"].ToString()!
            },
            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
    }
}