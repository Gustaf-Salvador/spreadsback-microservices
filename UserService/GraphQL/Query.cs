using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Microsoft.Extensions.Logging;
using HotChocolate;

namespace UserService.GraphQL;

public class Query
{
    private readonly ILogger<Query> _logger;

    public Query(ILogger<Query> logger)
    {
        _logger = logger;
    }

    public async Task<UserResponse?> GetUser(string id, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Query: GetUser called with ID: {UserId}", id);
        
        var result = await userService.GetUserByIdAsync(id);
        
        if (result.Success && result.Value != null)
        {
            _logger.LogInformation("GraphQL Query: User found: {UserId}", result.Value.Id);
            return MapToResponse(result.Value);
        }
        
        _logger.LogWarning("GraphQL Query: User not found: {UserId}", id);
        return null;
    }

    public async Task<UserResponse?> GetUserByEmail(string email, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Query: GetUserByEmail called with email: {Email}", email);
        
        var result = await userService.GetUserByEmailAsync(email);
        
        if (result.Success && result.Value != null)
        {
            _logger.LogInformation("GraphQL Query: User found by email: {UserId}", result.Value.Id);
            return MapToResponse(result.Value);
        }
        
        _logger.LogWarning("GraphQL Query: User not found by email: {Email}", email);
        return null;
    }

    public async Task<IEnumerable<UserResponse>> GetUsers([Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Query: GetUsers called");
        
        var result = await userService.GetAllUsersAsync();
        if (result.Success)
        {
            var userList = result.Value.ToList();
            _logger.LogInformation("GraphQL Query: Retrieved {UserCount} users", userList.Count);
            return userList.Select(MapToResponse);
        }
        
        _logger.LogWarning("GraphQL Query: Failed to retrieve users: {Error}", result.Error);
        return Enumerable.Empty<UserResponse>();
    }

    public async Task<bool> IsEmailAvailable(string email, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Query: IsEmailAvailable called with email: {Email}", email);
        
        var result = await userService.IsEmailAvailableAsync(email);
        
        if (result.Success)
        {
            _logger.LogInformation("GraphQL Query: Email availability check: {Email} = {Available}", email, result.Value);
            return result.Value;
        }
        
        _logger.LogWarning("GraphQL Query: Failed to check email availability: {Error}", result.Error);
        return false;
    }

    private static UserResponse MapToResponse(User user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.PhoneNumber,
        user.CreatedAt,
        user.UpdatedAt,
        (DTOs.UserStatus)user.Status,
        user.EmailVerified,
        user.ProfilePictureUrl,
        user.Metadata
    );
}