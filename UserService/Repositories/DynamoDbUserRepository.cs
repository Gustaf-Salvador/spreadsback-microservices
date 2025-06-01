using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UserService.Models;

namespace UserService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<bool> EmailExistsAsync(string email);
}

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

    public async Task<User?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving user by ID: {UserId}", id);
            
            var user = await _dynamoDbContext.LoadAsync<User>(id);
            
            if (user != null)
            {
                _logger.LogInformation("User found: {UserId}, Email: {Email}, Version: {Version}", 
                    user.Id, user.Email, user.Version);
            }
            else
            {
                _logger.LogWarning("User not found: {UserId}", id);
            }
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Retrieving user by email: {Email}", email);
            
            var queryConfig = new DynamoDBOperationConfig
            {
                IndexName = "EmailIndex"
            };
            
            var search = _dynamoDbContext.QueryAsync<User>(email.ToLowerInvariant(), queryConfig);
            var users = await search.GetRemainingAsync();
            var user = users.FirstOrDefault();
            
            if (user != null)
            {
                _logger.LogInformation("User found by email: {UserId}, Email: {Email}, Version: {Version}", 
                    user.Id, user.Email, user.Version);
            }
            else
            {
                _logger.LogWarning("User not found by email: {Email}", email);
            }
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all users");
            
            var search = _dynamoDbContext.ScanAsync<User>(new List<ScanCondition>());
            var users = await search.GetRemainingAsync();
            
            _logger.LogInformation("Retrieved {UserCount} users", users.Count);
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            throw;
        }
    }

    public async Task<User> CreateAsync(User user)
    {
        try
        {
            _logger.LogInformation("Creating new user: {Email}, CreatedBy: {CreatedBy}", 
                user.Email, user.CreatedBy);
            
            // Check if email already exists
            var existingUser = await GetByEmailAsync(user.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Attempt to create user with existing email: {Email}", user.Email);
                throw new InvalidOperationException($"User with email {user.Email} already exists");
            }
            
            // Ensure audit fields are set (should already be set by domain service)
            if (user.CreatedAtUtc == default)
            {
                user.Create("Repository");
            }
            
            await _dynamoDbContext.SaveAsync(user);
            
            _logger.LogInformation("User created successfully: {UserId}, Email: {Email}, Version: {Version}, CreatedBy: {CreatedBy}", 
                user.Id, user.Email, user.Version, user.CreatedBy);
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user.Email);
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        try
        {
            _logger.LogInformation("Updating user: {UserId}, ModifiedBy: {ModifiedBy}, Version: {Version}", 
                user.Id, user.ModifiedBy, user.Version);
            
            // Verify user exists
            var existingUser = await GetByIdAsync(user.Id);
            if (existingUser == null)
            {
                _logger.LogWarning("Attempt to update non-existent user: {UserId}", user.Id);
                throw new InvalidOperationException($"User with ID {user.Id} not found");
            }
            
            if (!existingUser.CanBeModified)
            {
                _logger.LogWarning("Attempt to update deactivated user: {UserId}, Status: {Status}", 
                    user.Id, existingUser.Status);
                throw new InvalidOperationException($"User {user.Id} cannot be modified in status: {existingUser.Status}");
            }
            
            // Version check for optimistic concurrency (optional enhancement)
            if (user.Version <= existingUser.Version)
            {
                _logger.LogWarning("Version conflict detected for user: {UserId}, Current: {CurrentVersion}, Attempted: {AttemptedVersion}", 
                    user.Id, existingUser.Version, user.Version);
            }
            
            await _dynamoDbContext.SaveAsync(user);
            
            _logger.LogInformation("User updated successfully: {UserId}, Version: {Version}, ModifiedBy: {ModifiedBy}, LastOperation: {LastOperation}", 
                user.Id, user.Version, user.ModifiedBy, user.LastOperation);
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting user: {UserId}", id);
            
            var user = await GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Attempt to delete non-existent user: {UserId}", id);
                return false;
            }
            
            await _dynamoDbContext.DeleteAsync<User>(id);
            
            _logger.LogInformation("User deleted successfully: {UserId}, Email: {Email}, FinalVersion: {Version}", 
                id, user.Email, user.Version);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        try
        {
            _logger.LogDebug("Checking if user exists: {UserId}", id);
            
            var user = await _dynamoDbContext.LoadAsync<User>(id);
            var exists = user != null;
            
            _logger.LogDebug("User exists check: {UserId} = {Exists}", id, exists);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user existence: {UserId}", id);
            throw;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            _logger.LogDebug("Checking if email exists: {Email}", email);
            
            var user = await GetByEmailAsync(email);
            var exists = user != null;
            
            _logger.LogDebug("Email exists check: {Email} = {Exists}", email, exists);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence: {Email}", email);
            throw;
        }
    }
}