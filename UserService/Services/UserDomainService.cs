using Microsoft.Extensions.Logging;
using UserService.Models;
using UserService.Repositories;
using FluentValidation;

namespace UserService.Services;

public interface IUserDomainService
{
    Task<User> CreateUserAsync(string cognitoUserId, string email, string firstName, string lastName, string? phoneNumber = null, Dictionary<string, object>? metadata = null, string createdBy = "System");
    Task<User> UpdateUserProfileAsync(string userId, string firstName, string lastName, string? phoneNumber = null, string modifiedBy = "System");
    Task<User> ActivateUserAsync(string userId, string modifiedBy = "System", string? reason = null);
    Task<User> SuspendUserAsync(string userId, string modifiedBy = "System", string? reason = null);
    Task<User> DeactivateUserAsync(string userId, string modifiedBy = "System", string? reason = null);
    Task<User> VerifyEmailAsync(string userId, string modifiedBy = "System", string? reason = null);
    Task<bool> IsEmailAvailableAsync(string email);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(string userId, string deletedBy = "System");
}

public class UserDomainService : IUserDomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<User> _userValidator;
    private readonly ICognitoService _cognitoService;
    private readonly ILogger<UserDomainService> _logger;

    public UserDomainService(
        IUserRepository userRepository,
        IValidator<User> userValidator,
        ICognitoService cognitoService,
        ILogger<UserDomainService> logger)
    {
        _userRepository = userRepository;
        _userValidator = userValidator;
        _cognitoService = cognitoService;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(string cognitoUserId, string email, string firstName, string lastName, string? phoneNumber = null, Dictionary<string, object>? metadata = null, string createdBy = "System")
    {
        _logger.LogInformation("Creating user record for Cognito user: {CognitoUserId}, Email: {Email}, CreatedBy: {CreatedBy}", cognitoUserId, email, createdBy);

        // Validate that Cognito User ID is provided
        if (string.IsNullOrEmpty(cognitoUserId))
        {
            _logger.LogError("Cognito User ID is required for user creation");
            throw new ArgumentException("Cognito User ID is required", nameof(cognitoUserId));
        }

        // Domain validation - check if user record already exists for this Cognito user
        var existingUserById = await _userRepository.GetByIdAsync(cognitoUserId);
        if (existingUserById != null)
        {
            _logger.LogWarning("Attempt to create user record for existing Cognito user: {CognitoUserId}", cognitoUserId);
            throw new InvalidOperationException($"User record for Cognito user {cognitoUserId} already exists");
        }

        // Domain validation - check if email already exists in our records
        if (await _userRepository.EmailExistsAsync(email))
        {
            _logger.LogWarning("Attempt to create user with existing email: {Email}", email);
            throw new InvalidOperationException($"User with email {email} already exists");
        }

        var user = new User
        {
            Id = cognitoUserId, // Use Cognito User ID as our primary key
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            Metadata = metadata ?? new Dictionary<string, object>(),
            Status = UserStatus.Pending,
            EmailVerified = false
        };

        // Set audit fields for creation
        user.Create(createdBy, cognitoUserId);

        // Validate domain object
        var validationResult = await _userValidator.ValidateAsync(user);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogError("User validation failed: {Errors}", errors);
            throw new ValidationException($"User validation failed: {errors}");
        }

        // Create user record in our repository (DynamoDB)
        var createdUser = await _userRepository.CreateAsync(user);
        
        _logger.LogInformation("User record created successfully for Cognito user: {CognitoUserId}, Email: {Email}, Version: {Version}", 
            createdUser.Id, createdUser.Email, createdUser.Version);
        
        return createdUser;
    }

    public async Task<User> UpdateUserProfileAsync(string userId, string firstName, string lastName, string? phoneNumber = null, string modifiedBy = "System")
    {
        _logger.LogInformation("Updating user profile: {UserId}, ModifiedBy: {ModifiedBy}", userId, modifiedBy);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for update: {UserId}", userId);
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        if (!user.CanBeModified)
        {
            _logger.LogWarning("Attempt to update non-modifiable user: {UserId}, Status: {Status}", userId, user.Status);
            throw new InvalidOperationException($"User {userId} cannot be modified in current status: {user.Status}");
        }

        // Apply domain logic with auditing
        user.UpdateProfile(firstName, lastName, phoneNumber, modifiedBy);

        // Validate updated user
        var validationResult = await _userValidator.ValidateAsync(user);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogError("User validation failed during update: {Errors}", errors);
            throw new ValidationException($"User validation failed: {errors}");
        }

        var updatedUser = await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("User profile updated successfully: {UserId}, Version: {Version}", userId, updatedUser.Version);
        
        return updatedUser;
    }

    public async Task<User> ActivateUserAsync(string userId, string modifiedBy = "System", string? reason = null)
    {
        _logger.LogInformation("Activating user: {UserId}, ModifiedBy: {ModifiedBy}, Reason: {Reason}", userId, modifiedBy, reason);

        // Get user record from our repository
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User record not found for activation: {UserId}", userId);
            throw new InvalidOperationException($"User record with ID {userId} not found");
        }

        try
        {
            // Step 1: Enable user in Cognito (external service)
            _logger.LogInformation("Enabling user in Cognito: {Email}", user.Email);
            var cognitoSuccess = await _cognitoService.EnableUserAsync(user.Email);
            
            if (!cognitoSuccess)
            {
                _logger.LogError("Failed to enable user in Cognito: {Email}", user.Email);
                throw new InvalidOperationException($"Failed to enable user {user.Email} in Cognito");
            }

            // Step 2: Update our user record if Cognito operation succeeded
            user.Activate(modifiedBy, reason);
            var updatedUser = await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("User activated successfully: {UserId}, Version: {Version}", userId, updatedUser.Version);
            
            return updatedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user activation: {UserId}", userId);
            
            // If our repository update failed but Cognito succeeded, log critical error for manual intervention
            if (ex.Message.Contains("Cognito"))
            {
                throw; // Cognito failed, safe to rethrow
            }
            else
            {
                _logger.LogCritical("User enabled in Cognito but repository update failed for user: {UserId}, Email: {Email}. Manual intervention required.", userId, user.Email);
                throw new InvalidOperationException($"Partial activation failure for user {userId}. Manual intervention required.");
            }
        }
    }

    public async Task<User> SuspendUserAsync(string userId, string modifiedBy = "System", string? reason = null)
    {
        _logger.LogInformation("Suspending user: {UserId}, ModifiedBy: {ModifiedBy}, Reason: {Reason}", userId, modifiedBy, reason);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User record not found for suspension: {UserId}", userId);
            throw new InvalidOperationException($"User record with ID {userId} not found");
        }

        try
        {
            // Step 1: Disable user in Cognito (external service)
            _logger.LogInformation("Disabling user in Cognito: {Email}", user.Email);
            var cognitoSuccess = await _cognitoService.DisableUserAsync(user.Email);
            
            if (!cognitoSuccess)
            {
                _logger.LogError("Failed to disable user in Cognito: {Email}", user.Email);
                throw new InvalidOperationException($"Failed to disable user {user.Email} in Cognito");
            }

            // Step 2: Update our user record if Cognito operation succeeded
            user.Suspend(modifiedBy, reason);
            var updatedUser = await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("User suspended successfully: {UserId}, Version: {Version}", userId, updatedUser.Version);
            
            return updatedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user suspension: {UserId}", userId);
            
            // If our repository update failed but Cognito succeeded, log critical error for manual intervention
            if (ex.Message.Contains("Cognito"))
            {
                throw; // Cognito failed, safe to rethrow
            }
            else
            {
                _logger.LogCritical("User disabled in Cognito but repository update failed for user: {UserId}, Email: {Email}. Manual intervention required.", userId, user.Email);
                throw new InvalidOperationException($"Partial suspension failure for user {userId}. Manual intervention required.");
            }
        }
    }

    public async Task<User> DeactivateUserAsync(string userId, string modifiedBy = "System", string? reason = null)
    {
        _logger.LogInformation("Deactivating user: {UserId}, ModifiedBy: {ModifiedBy}, Reason: {Reason}", userId, modifiedBy, reason);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User record not found for deactivation: {UserId}", userId);
            throw new InvalidOperationException($"User record with ID {userId} not found");
        }

        try
        {
            // Step 1: Disable user in Cognito (external service)
            _logger.LogInformation("Disabling user in Cognito: {Email}", user.Email);
            var cognitoSuccess = await _cognitoService.DisableUserAsync(user.Email);
            
            if (!cognitoSuccess)
            {
                _logger.LogError("Failed to disable user in Cognito: {Email}", user.Email);
                throw new InvalidOperationException($"Failed to disable user {user.Email} in Cognito");
            }

            // Step 2: Update our user record if Cognito operation succeeded
            user.Deactivate(modifiedBy, reason);
            var updatedUser = await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("User deactivated successfully: {UserId}, Version: {Version}", userId, updatedUser.Version);
            
            return updatedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user deactivation: {UserId}", userId);
            
            // If our repository update failed but Cognito succeeded, log critical error for manual intervention
            if (ex.Message.Contains("Cognito"))
            {
                throw; // Cognito failed, safe to rethrow
            }
            else
            {
                _logger.LogCritical("User disabled in Cognito but repository update failed for user: {UserId}, Email: {Email}. Manual intervention required.", userId, user.Email);
                throw new InvalidOperationException($"Partial deactivation failure for user {userId}. Manual intervention required.");
            }
        }
    }

    public async Task<User> VerifyEmailAsync(string userId, string modifiedBy = "System", string? reason = null)
    {
        _logger.LogInformation("Verifying email for user: {UserId}, ModifiedBy: {ModifiedBy}, Reason: {Reason}", userId, modifiedBy, reason);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User record not found for email verification: {UserId}", userId);
            throw new InvalidOperationException($"User record with ID {userId} not found");
        }

        try
        {
            // Step 1: Confirm user email in Cognito (external service)
            _logger.LogInformation("Confirming user email in Cognito: {Email}", user.Email);
            var cognitoSuccess = await _cognitoService.ConfirmUserEmailAsync(user.Email);
            
            if (!cognitoSuccess)
            {
                _logger.LogError("Failed to confirm user email in Cognito: {Email}", user.Email);
                throw new InvalidOperationException($"Failed to confirm user email {user.Email} in Cognito");
            }

            // Step 2: Update our user record if Cognito operation succeeded
            user.VerifyEmail(modifiedBy, reason);
            var updatedUser = await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("Email verified successfully for user: {UserId}, Version: {Version}", userId, updatedUser.Version);
            
            return updatedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification: {UserId}", userId);
            
            // If our repository update failed but Cognito succeeded, log critical error for manual intervention
            if (ex.Message.Contains("Cognito"))
            {
                throw; // Cognito failed, safe to rethrow
            }
            else
            {
                _logger.LogCritical("User email confirmed in Cognito but repository update failed for user: {UserId}, Email: {Email}. Manual intervention required.", userId, user.Email);
                throw new InvalidOperationException($"Partial email verification failure for user {userId}. Manual intervention required.");
            }
        }
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        _logger.LogDebug("Checking email availability: {Email}", email);
        
        var exists = await _userRepository.EmailExistsAsync(email);
        var available = !exists;
        
        _logger.LogDebug("Email availability check: {Email} = {Available}", email, available);
        
        return available;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        _logger.LogDebug("Retrieving user by ID: {UserId}", userId);
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        _logger.LogDebug("Retrieving user by email: {Email}", email);
        return await _userRepository.GetByEmailAsync(email);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        _logger.LogDebug("Retrieving all users");
        return await _userRepository.GetAllAsync();
    }

    public async Task<bool> DeleteUserAsync(string userId, string deletedBy = "System")
    {
        _logger.LogInformation("Deleting user: {UserId}, DeletedBy: {DeletedBy}", userId, deletedBy);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for deletion: {UserId}", userId);
            return false;
        }

        // Note: For user deletion, we might want to implement soft delete instead
        // or ensure Cognito user is also deleted. This depends on business requirements.
        var deleted = await _userRepository.DeleteAsync(userId);
        
        if (deleted)
        {
            _logger.LogInformation("User deleted successfully: {UserId}, Email: {Email}, DeletedBy: {DeletedBy}", 
                userId, user.Email, deletedBy);
        }
        else
        {
            _logger.LogWarning("Failed to delete user: {UserId}", userId);
        }
        
        return deleted;
    }
}