using UserService.Common;
using UserService.Models;

namespace UserService.Events;

public class UserCreatedEvent : DomainEvent
{
    public string UserId { get; }
    public string Email { get; }
    public string CreatedBy { get; }

    public UserCreatedEvent(string userId, string email, string createdBy)
    {
        UserId = userId;
        Email = email;
        CreatedBy = createdBy;
    }
}

public class UserActivatedEvent : DomainEvent
{
    public string UserId { get; }
    public string Email { get; }
    public string ModifiedBy { get; }
    public string? Reason { get; }

    public UserActivatedEvent(string userId, string email, string modifiedBy, string? reason = null)
    {
        UserId = userId;
        Email = email;
        ModifiedBy = modifiedBy;
        Reason = reason;
    }
}

public class UserDeactivatedEvent : DomainEvent
{
    public string UserId { get; }
    public string Email { get; }
    public string ModifiedBy { get; }
    public string? Reason { get; }

    public UserDeactivatedEvent(string userId, string email, string modifiedBy, string? reason = null)
    {
        UserId = userId;
        Email = email;
        ModifiedBy = modifiedBy;
        Reason = reason;
    }
}

public class UserSuspendedEvent : DomainEvent
{
    public string UserId { get; }
    public string Email { get; }
    public string ModifiedBy { get; }
    public string? Reason { get; }

    public UserSuspendedEvent(string userId, string email, string modifiedBy, string? reason = null)
    {
        UserId = userId;
        Email = email;
        ModifiedBy = modifiedBy;
        Reason = reason;
    }
}

public class UserEmailVerifiedEvent : DomainEvent
{
    public string UserId { get; }
    public string Email { get; }
    public string ModifiedBy { get; }

    public UserEmailVerifiedEvent(string userId, string email, string modifiedBy)
    {
        UserId = userId;
        Email = email;
        ModifiedBy = modifiedBy;
    }
}