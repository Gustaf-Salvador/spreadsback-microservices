using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public record CreateUserInput(
    [Required][EmailAddress] string Email,
    [Required] string FirstName,
    [Required] string LastName,
    string? PhoneNumber,
    string? CognitoUserId,
    Dictionary<string, object>? Metadata
);

public record UpdateUserInput(
    string Id,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? ProfilePictureUrl
);

public record UserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    UserStatus Status,
    bool EmailVerified,
    string? ProfilePictureUrl,
    Dictionary<string, object> Metadata
);

public enum UserStatus
{
    Pending,
    Active,
    Suspended,
    Deactivated
}