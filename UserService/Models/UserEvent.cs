using System.Text.Json;

namespace UserService.Models;

public abstract class UserEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Data { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string CorrelationId { get; set; } = string.Empty;
}

public class UserCreatedEvent : UserEvent
{
    public UserCreatedEvent(User user, string correlationId = "")
    {
        UserId = user.Id;
        EventType = "UserCreated";
        CorrelationId = correlationId;
        Data = JsonSerializer.Serialize(new
        {
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Status,
            user.Metadata
        });
    }
}

public class UserUpdatedEvent : UserEvent
{
    public UserUpdatedEvent(User user, Dictionary<string, object> changes, string correlationId = "")
    {
        UserId = user.Id;
        EventType = "UserUpdated";
        CorrelationId = correlationId;
        Data = JsonSerializer.Serialize(new
        {
            Changes = changes,
            UpdatedAt = DateTime.UtcNow
        });
    }
}

public class UserStatusChangedEvent : UserEvent
{
    public UserStatusChangedEvent(string userId, UserStatus oldStatus, UserStatus newStatus, string correlationId = "")
    {
        UserId = userId;
        EventType = "UserStatusChanged";
        CorrelationId = correlationId;
        Data = JsonSerializer.Serialize(new
        {
            OldStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            ChangedAt = DateTime.UtcNow
        });
    }
}

public class UserEmailVerifiedEvent : UserEvent
{
    public UserEmailVerifiedEvent(string userId, string correlationId = "")
    {
        UserId = userId;
        EventType = "UserEmailVerified";
        CorrelationId = correlationId;
        Data = JsonSerializer.Serialize(new
        {
            VerifiedAt = DateTime.UtcNow
        });
    }
}