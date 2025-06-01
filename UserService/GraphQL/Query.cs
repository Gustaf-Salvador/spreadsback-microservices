using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Microsoft.Extensions.Logging;

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
        
        var user = await userService.GetUserByIdAsync(id);
        
        if (user != null)
        {
            _logger.LogInformation("GraphQL Query: User found: {UserId}", user.Id);
            return MapToResponse(user);
        }
        
        _logger.LogWarning("GraphQL Query: User not found: {UserId}", id);
        return null;
    }

    public async Task<UserResponse?> GetUserByEmail(string email, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Query: GetUserByEmail called with email: {Email}", email);
        
        var user = await userService.GetUserByEmailAsync(email);
        
        if (user != null)
        {
            _logger.LogInformation("GraphQL Query: User found by email: {UserId}", user.Id);
            return MapToResponse(user);
        }
        
        _logger.LogWarning("GraphQL Query: User not found by email: {Email}", email);
        return null;
    }

    public async Task<IEnumerable<UserResponse>> GetUsers([Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Query: GetUsers called");
        
        var users = await userService.GetAllUsersAsync();
        var userList = users.ToList();
        
        _logger.LogInformation("GraphQL Query: Retrieved {UserCount} users", userList.Count);
        
        return userList.Select(MapToResponse);
    }

    public async Task<bool> IsEmailAvailable(string email, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Query: IsEmailAvailable called with email: {Email}", email);
        
        var available = await userService.IsEmailAvailableAsync(email);
        
        _logger.LogInformation("GraphQL Query: Email availability check: {Email} = {Available}", email, available);
        
        return available;
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