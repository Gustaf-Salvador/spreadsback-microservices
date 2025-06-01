using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UserService.Models;
using UserService.Common;

namespace UserService.Repositories;

public class DynamoDbUserRepository : IUserRepository
{
    private readonly DynamoDBContext _dynamoDbContext;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ILogger<DynamoDbUserRepository> _logger;
    private readonly string _tableName;

    public DynamoDbUserRepository(
        DynamoDBContext dynamoDbContext, 
        IAmazonDynamoDB dynamoDbClient,
        ILogger<DynamoDbUserRepository> logger,
        IConfiguration configuration)
    {
        _dynamoDbContext = dynamoDbContext;
        _dynamoDbClient = dynamoDbClient;
        _logger = logger;
        _tableName = configuration.GetValue<string>("DynamoDB:UserTableName") ?? "Users";
    }

    public async Task<OperationResult<User>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving user by ID: {UserId}", id);
            
            var user = await _dynamoDbContext.LoadAsync<User>(id, cancellationToken);
            
            if (user != null)
            {
                _logger.LogInformation("User found: {UserId}, Email: {Email}", user.Id, user.Email);
                return OperationResult.Ok(user);
            }
            else
            {
                _logger.LogWarning("User not found: {UserId}", id);
                return OperationResult.Fail<User>($"User with ID {id} not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
            return OperationResult.Fail<User>($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<OperationResult<User>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving user by email: {Email}", email);
            
            var queryConfig = new DynamoDBOperationConfig
            {
                IndexName = "EmailIndex"
            };
            
            var search = _dynamoDbContext.QueryAsync<User>(email.ToLowerInvariant(), queryConfig);
            var users = await search.GetRemainingAsync(cancellationToken);
            var user = users.FirstOrDefault();
            
            if (user != null)
            {
                _logger.LogInformation("User found by email: {UserId}, Email: {Email}", user.Id, user.Email);
                return OperationResult.Ok(user);
            }
            else
            {
                _logger.LogWarning("User not found by email: {Email}", email);
                return OperationResult.Fail<User>($"User with email {email} not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            return OperationResult.Fail<User>($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<User>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving all users");
            
            var search = _dynamoDbContext.ScanAsync<User>(new List<ScanCondition>());
            var users = await search.GetRemainingAsync(cancellationToken);
            
            _logger.LogInformation("Retrieved {UserCount} users", users.Count);
            
            return OperationResult.Ok<IEnumerable<User>>(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return OperationResult.Fail<IEnumerable<User>>($"Error retrieving users: {ex.Message}");
        }
    }

    public async Task<OperationResult<bool>> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if user exists: {UserId}", id);
            
            var user = await _dynamoDbContext.LoadAsync<User>(id, cancellationToken);
            var exists = user != null;
            
            _logger.LogDebug("User exists check: {UserId} = {Exists}", id, exists);
            
            return OperationResult.Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user existence: {UserId}", id);
            return OperationResult.Fail<bool>($"Error checking user existence: {ex.Message}");
        }
    }

    public async Task<OperationResult<bool>> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if email exists: {Email}", email);
            
            var user = await GetByEmailAsync(email, cancellationToken);
            var exists = user.Success && user.Value != null;
            
            _logger.LogDebug("Email exists check: {Email} = {Exists}", email, exists);
            
            return OperationResult.Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence: {Email}", email);
            return OperationResult.Fail<bool>($"Error checking email existence: {ex.Message}");
        }
    }

    public async Task<OperationResult> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new user: {Email}", user.Email);
            
            // Check if email already exists
            var emailExistsResult = await EmailExistsAsync(user.Email, cancellationToken);
            if (emailExistsResult.Success && emailExistsResult.Value)
            {
                _logger.LogWarning("Attempt to create user with existing email: {Email}", user.Email);
                return OperationResult.Fail($"User with email {user.Email} already exists");
            }
            
            await _dynamoDbContext.SaveAsync(user, cancellationToken);
            
            _logger.LogInformation("User created successfully: {UserId}, Email: {Email}", user.Id, user.Email);
            
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user.Email);
            return OperationResult.Fail($"Error creating user: {ex.Message}");
        }
    }

    public async Task<OperationResult> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating user: {UserId}", user.Id);
            
            // Verify user exists
            var existingUserResult = await GetByIdAsync(user.Id, cancellationToken);
            if (!existingUserResult.Success)
            {
                _logger.LogWarning("Attempt to update non-existent user: {UserId}", user.Id);
                return OperationResult.Fail($"User with ID {user.Id} not found");
            }
            
            await _dynamoDbContext.SaveAsync(user, cancellationToken);
            
            _logger.LogInformation("User updated successfully: {UserId}", user.Id);
            
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            return OperationResult.Fail($"Error updating user: {ex.Message}");
        }
    }

    public async Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting user: {UserId}", id);
            
            var userResult = await GetByIdAsync(id, cancellationToken);
            if (!userResult.Success)
            {
                _logger.LogWarning("Attempt to delete non-existent user: {UserId}", id);
                return OperationResult.Fail($"User with ID {id} not found");
            }
            
            await _dynamoDbContext.DeleteAsync<User>(id, cancellationToken);
            
            _logger.LogInformation("User deleted successfully: {UserId}", id);
            
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return OperationResult.Fail($"Error deleting user: {ex.Message}");
        }
    }
}