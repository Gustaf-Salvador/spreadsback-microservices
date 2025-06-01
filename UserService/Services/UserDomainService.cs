using UserService.Common;
using UserService.Models;
using UserService.Events;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace UserService.Services;

public interface IUserDomainService
{
    Task<OperationResult<User>> CreateUserAsync(string email, string firstName, string lastName, 
        string? phoneNumber = null, string createdBy = "System", 
        string? cognitoUserId = null, CancellationToken cancellationToken = default);
    
    Task<OperationResult<User>> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<OperationResult<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<OperationResult<IEnumerable<User>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<bool>> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default);
    
    Task<OperationResult<User>> UpdateUserProfileAsync(string id, string firstName, string lastName, 
        string? phoneNumber = null, string modifiedBy = "System", CancellationToken cancellationToken = default);
    
    Task<OperationResult<User>> ActivateUserAsync(string id, string modifiedBy = "System", 
        string? reason = null, CancellationToken cancellationToken = default);
    
    Task<OperationResult<User>> SuspendUserAsync(string id, string modifiedBy = "System", 
        string? reason = null, CancellationToken cancellationToken = default);
    
    Task<OperationResult<User>> DeactivateUserAsync(string id, string modifiedBy = "System", 
        string? reason = null, CancellationToken cancellationToken = default);
    
    Task<OperationResult<User>> VerifyEmailAsync(string id, string modifiedBy = "System", 
        string? reason = null, CancellationToken cancellationToken = default);
    
    Task<OperationResult> DeleteUserAsync(string id, CancellationToken cancellationToken = default);
}

public class UserDomainService : IUserDomainService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICognitoService _cognitoService;
    private readonly IValidator<User> _userValidator;
    private readonly ILogger<UserDomainService> _logger;

    public UserDomainService(
        IUnitOfWork unitOfWork,
        ICognitoService cognitoService,
        IValidator<User> userValidator,
        ILogger<UserDomainService> logger)
    {
        _unitOfWork = unitOfWork;
        _cognitoService = cognitoService;
        _userValidator = userValidator;
        _logger = logger;
    }

    public async Task<OperationResult<User>> CreateUserAsync(string email, string firstName, string lastName,
        string? phoneNumber = null, string createdBy = "System", string? cognitoUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating user with email {Email}", email);

            // Check if email already exists
            var emailExistsResult = await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken);
            if (!emailExistsResult.Success)
                return OperationResult.Fail<User>($"Failed to check email availability: {emailExistsResult.Error}");

            if (emailExistsResult.Value)
                return OperationResult.Fail<User>($"User with email {email} already exists");

            // Create user domain object
            var user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber
            };

            user.Create(createdBy, cognitoUserId);

            // Validate domain rules
            var validationResult = await _userValidator.ValidateAsync(user, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return OperationResult.Fail<User>($"Validation failed: {errors}");
            }

            // Persist user
            var addResult = await _unitOfWork.Users.AddAsync(user, cancellationToken);
            if (!addResult.Success)
                return OperationResult.Fail<User>($"Failed to persist user: {addResult.Error}");

            // Add domain event
            _unitOfWork.AddDomainEvent(new UserService.Events.UserCreatedEvent(user.Id, user.Email, createdBy));

            // Publish domain event
            await PublishEventAsync(new UserService.Events.UserCreatedEvent(user.Id, user.Email, createdBy));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created user {UserId} with email {Email}", user.Id, email);
            return OperationResult.Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email {Email}", email);
            return OperationResult.Fail<User>($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<OperationResult<User>> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (!result.Success)
                return OperationResult.Fail<User>($"Failed to retrieve user: {result.Error}");

            return OperationResult.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by ID {UserId}", id);
            return OperationResult.Fail<User>($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<OperationResult<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (!result.Success)
                return OperationResult.Fail<User>($"Failed to retrieve user: {result.Error}");

            return OperationResult.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by email {Email}", email);
            return OperationResult.Fail<User>($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<User>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unitOfWork.Users.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all users");
            return OperationResult.Fail<IEnumerable<User>>($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<OperationResult<bool>> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken);
            return result.Success ? OperationResult.Ok(!result.Value) : OperationResult.Fail<bool>(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check email availability for {Email}", email);
            return OperationResult.Fail<bool>($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<OperationResult<User>> ActivateUserAsync(string id, string modifiedBy = "System",
        string? reason = null, CancellationToken cancellationToken = default)
    {
        return await UpdateUserStatusAsync(id, UserStatus.Active, modifiedBy, reason, cancellationToken,
            async (user) =>
            {
                await _cognitoService.EnableUserAsync(user.Id);
                _unitOfWork.AddDomainEvent(new UserService.Events.UserActivatedEvent(user.Id, user.Email, modifiedBy, reason));
            });
    }

    public async Task<OperationResult<User>> SuspendUserAsync(string id, string modifiedBy = "System",
        string? reason = null, CancellationToken cancellationToken = default)
    {
        return await UpdateUserStatusAsync(id, UserStatus.Suspended, modifiedBy, reason, cancellationToken,
            async (user) =>
            {
                await _cognitoService.DisableUserAsync(user.Id);
                _unitOfWork.AddDomainEvent(new UserService.Events.UserSuspendedEvent(user.Id, user.Email, modifiedBy, reason));
            });
    }

    public async Task<OperationResult<User>> DeactivateUserAsync(string id, string modifiedBy = "System",
        string? reason = null, CancellationToken cancellationToken = default)
    {
        return await UpdateUserStatusAsync(id, UserStatus.Deactivated, modifiedBy, reason, cancellationToken,
            async (user) =>
            {
                await _cognitoService.DisableUserAsync(user.Id);
                _unitOfWork.AddDomainEvent(new UserService.Events.UserDeactivatedEvent(user.Id, user.Email, modifiedBy, reason));
            });
    }

    public async Task<OperationResult<User>> VerifyEmailAsync(string id, string modifiedBy = "System",
        string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying email for user {UserId}", id);

            var getUserResult = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (!getUserResult.Success)
                return OperationResult.Fail<User>($"User not found: {getUserResult.Error}");

            var user = getUserResult.Value;

            // External service operation first
            await _cognitoService.ConfirmUserEmailAsync(user.Id);

            // Update domain object
            user.VerifyEmail(modifiedBy, reason);

            // Persist changes
            var updateResult = await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            if (!updateResult.Success)
            {
                _logger.LogCritical("MANUAL INTERVENTION REQUIRED: Cognito email verified for user {UserId} but DynamoDB update failed: {Error}",
                    id, updateResult.Error);
                return OperationResult.Fail<User>($"Partial failure - manual intervention required: {updateResult.Error}");
            }

            // Add domain event
            _unitOfWork.AddDomainEvent(new UserService.Events.UserEmailVerifiedEvent(user.Id, user.Email, modifiedBy));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully verified email for user {UserId}", id);
            return OperationResult.Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify email for user {UserId}", id);
            return OperationResult.Fail<User>($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<OperationResult<User>> UpdateUserProfileAsync(string id, string firstName, string lastName,
        string? phoneNumber = null, string modifiedBy = "System", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating profile for user {UserId}", id);

            var getUserResult = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (!getUserResult.Success)
                return OperationResult.Fail<User>($"User not found: {getUserResult.Error}");

            var user = getUserResult.Value;

            if (!user.CanBeModified)
                return OperationResult.Fail<User>("User cannot be modified in current status");

            // Update profile
            user.UpdateProfile(firstName, lastName, phoneNumber, modifiedBy);

            // Validate
            var validationResult = await _userValidator.ValidateAsync(user, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return OperationResult.Fail<User>($"Validation failed: {errors}");
            }

            // Persist
            var updateResult = await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            if (!updateResult.Success)
                return OperationResult.Fail<User>($"Failed to update user: {updateResult.Error}");

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated profile for user {UserId}", id);
            return OperationResult.Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile for user {UserId}", id);
            return OperationResult.Fail<User>($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<OperationResult> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting user {UserId}", id);

            var result = await _unitOfWork.Users.DeleteAsync(id, cancellationToken);
            if (!result.Success)
                return OperationResult.Fail($"Failed to delete user: {result.Error}");

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted user {UserId}", id);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", id);
            return OperationResult.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<OperationResult<User>> UpdateUserStatusAsync(string id, UserStatus newStatus, string modifiedBy,
        string? reason, CancellationToken cancellationToken, Func<User, Task> cognitoOperation)
    {
        try
        {
            _logger.LogInformation("Updating user {UserId} status to {Status}", id, newStatus);

            var getUserResult = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            if (!getUserResult.Success)
                return OperationResult.Fail<User>($"User not found: {getUserResult.Error}");

            var user = getUserResult.Value;

            if (!user.CanBeModified)
                return OperationResult.Fail<User>("User cannot be modified in current status");

            // External service operation first
            await cognitoOperation(user);

            // Update domain object
            switch (newStatus)
            {
                case UserStatus.Active:
                    user.Activate(modifiedBy, reason);
                    break;
                case UserStatus.Suspended:
                    user.Suspend(modifiedBy, reason);
                    break;
                case UserStatus.Deactivated:
                    user.Deactivate(modifiedBy, reason);
                    break;
                default:
                    return OperationResult.Fail<User>($"Invalid status transition to {newStatus}");
            }

            // Persist changes
            var updateResult = await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            if (!updateResult.Success)
            {
                _logger.LogCritical("MANUAL INTERVENTION REQUIRED: Cognito user {UserId} status changed but DynamoDB update failed: {Error}",
                    id, updateResult.Error);
                return OperationResult.Fail<User>($"Partial failure - manual intervention required: {updateResult.Error}");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated user {UserId} status to {Status}", id, newStatus);
            return OperationResult.Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId} status to {Status}", id, newStatus);
            return OperationResult.Fail<User>($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task PublishEventAsync(DomainEvent domainEvent)
    {
        try
        {
            // This would integrate with your event publishing mechanism
            // For now, just log the event
            _logger.LogInformation("Publishing domain event: {EventType} for entity {EntityId}", 
                domainEvent.GetType().Name, domainEvent.EntityId);
            
            // TODO: Implement actual event publishing (e.g., to SNS, EventBridge, etc.)
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event: {EventType}", domainEvent.GetType().Name);
            // Don't fail the main operation for event publishing failures
        }
    }
}