using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Microsoft.Extensions.Logging;
using FluentValidation;
using HotChocolate;

namespace UserService.GraphQL;

public class Mutation
{
    private readonly ILogger<Mutation> _logger;

    public Mutation(ILogger<Mutation> logger)
    {
        _logger = logger;
    }

    public async Task<UserResponse> CreateUser(CreateUserInput input, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Mutation: CreateUser called with email: {Email}", input.Email);
        
        try
        {
            // Extract Cognito User ID from input metadata or require it as a separate parameter
            var cognitoUserId = input.Metadata?.ContainsKey("cognitoUserId") == true 
                ? input.Metadata["cognitoUserId"]?.ToString() 
                : throw new ArgumentException("Cognito User ID is required in metadata");
            if (string.IsNullOrEmpty(cognitoUserId))
            {
                throw new ArgumentException("Cognito User ID cannot be empty");
            }
            
            var result = await userService.CreateUserAsync(
                input.Email,
                input.FirstName,
                input.LastName,
                input.PhoneNumber,
                "GraphQL", // CreatedBy - could be enhanced to include actual user context
                cognitoUserId
            );
            
            if (!result.Success)
            {
                throw new GraphQLException($"Failed to create user: {result.Error}");
            }
            
            _logger.LogInformation("GraphQL Mutation: User created successfully: {UserId}", result.Value.Id);
            
            return MapToResponse(result.Value);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("GraphQL Mutation: Missing required parameter in CreateUser: {Error}", ex.Message);
            throw new GraphQLException($"Missing required parameter: {ex.Message}");
        }
        catch (ValidationException ex)
        {
            _logger.LogError("GraphQL Mutation: Validation failed for CreateUser: {Errors}", ex.Message);
            throw new GraphQLException($"Validation failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("GraphQL Mutation: Business rule violation in CreateUser: {Error}", ex.Message);
            throw new GraphQLException($"Business rule violation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL Mutation: Unexpected error in CreateUser");
            throw new GraphQLException("An unexpected error occurred while creating the user");
        }
    }

    public async Task<UserResponse> UpdateUser(UpdateUserInput input, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Mutation: UpdateUser called for user: {UserId}", input.Id);
        
        try
        {
            var result = await userService.UpdateUserProfileAsync(
                input.Id,
                input.FirstName,
                input.LastName,
                input.PhoneNumber,
                "GraphQL" // ModifiedBy - could be enhanced to include actual user context
            );
            
            if (!result.Success)
            {
                throw new GraphQLException($"Failed to update user: {result.Error}");
            }
            
            _logger.LogInformation("GraphQL Mutation: User updated successfully: {UserId}", result.Value.Id);
            
            return MapToResponse(result.Value);
        }
        catch (ValidationException ex)
        {
            _logger.LogError("GraphQL Mutation: Validation failed for UpdateUser: {Errors}", ex.Message);
            throw new GraphQLException($"Validation failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("GraphQL Mutation: Business rule violation in UpdateUser: {Error}", ex.Message);
            throw new GraphQLException($"Business rule violation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL Mutation: Unexpected error in UpdateUser");
            throw new GraphQLException("An unexpected error occurred while updating the user");
        }
    }

    public async Task<UserResponse> ActivateUser(string id, string? reason, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Mutation: ActivateUser called for user: {UserId}, Reason: {Reason}", id, reason);
        
        try
        {
            var result = await userService.ActivateUserAsync(
                id, 
                "GraphQL", // ModifiedBy - could be enhanced to include actual user context
                reason
            );
            
            if (!result.Success)
            {
                throw new GraphQLException($"Failed to activate user: {result.Error}");
            }
            
            _logger.LogInformation("GraphQL Mutation: User activated successfully: {UserId}", result.Value.Id);
            
            return MapToResponse(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("GraphQL Mutation: Business rule violation in ActivateUser: {Error}", ex.Message);
            throw new GraphQLException($"Business rule violation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL Mutation: Unexpected error in ActivateUser");
            throw new GraphQLException("An unexpected error occurred while activating the user");
        }
    }

    public async Task<UserResponse> SuspendUser(string id, string? reason, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Mutation: SuspendUser called for user: {UserId}, Reason: {Reason}", id, reason);
        
        try
        {
            var result = await userService.SuspendUserAsync(
                id, 
                "GraphQL", // ModifiedBy - could be enhanced to include actual user context
                reason
            );
            
            if (!result.Success)
            {
                throw new GraphQLException($"Failed to suspend user: {result.Error}");
            }
            
            _logger.LogInformation("GraphQL Mutation: User suspended successfully: {UserId}", result.Value.Id);
            
            return MapToResponse(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("GraphQL Mutation: Business rule violation in SuspendUser: {Error}", ex.Message);
            throw new GraphQLException($"Business rule violation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL Mutation: Unexpected error in SuspendUser");
            throw new GraphQLException("An unexpected error occurred while suspending the user");
        }
    }

    public async Task<UserResponse> DeactivateUser(string id, string? reason, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Mutation: DeactivateUser called for user: {UserId}, Reason: {Reason}", id, reason);
        
        try
        {
            var result = await userService.DeactivateUserAsync(
                id, 
                "GraphQL", // ModifiedBy - could be enhanced to include actual user context
                reason
            );
            
            if (!result.Success)
            {
                throw new GraphQLException($"Failed to deactivate user: {result.Error}");
            }
            
            _logger.LogInformation("GraphQL Mutation: User deactivated successfully: {UserId}", result.Value.Id);
            
            return MapToResponse(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("GraphQL Mutation: Business rule violation in DeactivateUser: {Error}", ex.Message);
            throw new GraphQLException($"Business rule violation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL Mutation: Unexpected error in DeactivateUser");
            throw new GraphQLException("An unexpected error occurred while deactivating the user");
        }
    }

    public async Task<UserResponse> VerifyEmail(string id, string? reason, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Mutation: VerifyEmail called for user: {UserId}, Reason: {Reason}", id, reason);
        
        try
        {
            var result = await userService.VerifyEmailAsync(
                id, 
                "GraphQL", // ModifiedBy - could be enhanced to include actual user context
                reason
            );
            
            if (!result.Success)
            {
                throw new GraphQLException($"Failed to verify email: {result.Error}");
            }
            
            _logger.LogInformation("GraphQL Mutation: Email verified successfully: {UserId}", result.Value.Id);
            
            return MapToResponse(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("GraphQL Mutation: Business rule violation in VerifyEmail: {Error}", ex.Message);
            throw new GraphQLException($"Business rule violation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL Mutation: Unexpected error in VerifyEmail");
            throw new GraphQLException("An unexpected error occurred while verifying email");
        }
    }

    public async Task<bool> DeleteUser(string id, [Service] IUserDomainService userService)
    {
        _logger.LogInformation("GraphQL Mutation: DeleteUser called for user: {UserId}", id);
        
        try
        {
            var result = await userService.DeleteUserAsync(id);
            
            if (result.Success)
            {
                _logger.LogInformation("GraphQL Mutation: User deleted successfully: {UserId}", id);
                return true;
            }
            else
            {
                _logger.LogWarning("GraphQL Mutation: Failed to delete user: {UserId}, Error: {Error}", id, result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL Mutation: Unexpected error in DeleteUser");
            throw new GraphQLException("An unexpected error occurred while deleting the user");
        }
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