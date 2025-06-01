using System.ComponentModel.DataAnnotations;
using Amazon.DynamoDBv2.DataModel;
using System.Text.Json;

namespace UserService.Models;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey]
    public string Id { get; set; } = string.Empty; // Will be set to Cognito User ID
    
    [DynamoDBProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [DynamoDBGlobalSecondaryIndexHashKey("EmailIndex")]
    public string EmailGSI => Email.ToLowerInvariant();
    
    [DynamoDBProperty]
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [DynamoDBProperty]
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [DynamoDBProperty]
    public string? PhoneNumber { get; set; }
    
    // Auditing fields
    [DynamoDBProperty]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    [DynamoDBProperty]
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    
    [DynamoDBProperty]
    public string CreatedBy { get; set; } = "System";
    
    [DynamoDBProperty]
    public string ModifiedBy { get; set; } = "System";
    
    [DynamoDBProperty]
    public int Version { get; set; } = 1;
    
    [DynamoDBProperty]
    public string? LastOperation { get; set; }
    
    [DynamoDBProperty]
    public string? LastOperationReason { get; set; }
    
    // Legacy compatibility - will be removed in future version
    [DynamoDBProperty]
    public DateTime CreatedAt 
    { 
        get => CreatedAtUtc; 
        set => CreatedAtUtc = value; 
    }
    
    [DynamoDBProperty]
    public DateTime? UpdatedAt 
    { 
        get => ModifiedAtUtc == CreatedAtUtc ? null : ModifiedAtUtc; 
        set => ModifiedAtUtc = value ?? ModifiedAtUtc; 
    }
    
    [DynamoDBProperty]
    public UserStatus Status { get; set; } = UserStatus.Pending;
    
    [DynamoDBProperty]
    public bool EmailVerified { get; set; } = false;
    
    [DynamoDBProperty]
    public string? ProfilePictureUrl { get; set; }
    
    [DynamoDBProperty]
    public string MetadataJson { get; set; } = "{}";
    
    [DynamoDBIgnore]
    public Dictionary<string, object> Metadata
    {
        get => string.IsNullOrEmpty(MetadataJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson) ?? new Dictionary<string, object>();
        set => MetadataJson = JsonSerializer.Serialize(value);
    }

    // Domain logic methods with auditing
    public void UpdateProfile(string firstName, string lastName, string? phoneNumber = null, string modifiedBy = "System")
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        UpdateAuditFields("ProfileUpdate", modifiedBy);
    }

    public void VerifyEmail(string modifiedBy = "System", string? reason = null)
    {
        EmailVerified = true;
        UpdateAuditFields("EmailVerification", modifiedBy, reason);
    }

    public void Activate(string modifiedBy = "System", string? reason = null)
    {
        Status = UserStatus.Active;
        UpdateAuditFields("Activation", modifiedBy, reason);
    }

    public void Suspend(string modifiedBy = "System", string? reason = null)
    {
        Status = UserStatus.Suspended;
        UpdateAuditFields("Suspension", modifiedBy, reason);
    }

    public void Deactivate(string modifiedBy = "System", string? reason = null)
    {
        Status = UserStatus.Deactivated;
        UpdateAuditFields("Deactivation", modifiedBy, reason);
    }

    public void Create(string createdBy = "System", string? cognitoUserId = null)
    {
        var now = DateTime.UtcNow;
        CreatedAtUtc = now;
        ModifiedAtUtc = now;
        CreatedBy = createdBy;
        ModifiedBy = createdBy;
        Version = 1;
        LastOperation = "Creation";
        
        // If Cognito User ID is provided, use it as the primary key
        if (!string.IsNullOrEmpty(cognitoUserId))
        {
            Id = cognitoUserId;
        }
    }

    private void UpdateAuditFields(string operation, string modifiedBy, string? reason = null)
    {
        ModifiedAtUtc = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
        Version++;
        LastOperation = operation;
        LastOperationReason = reason;
    }

    public bool IsActive => Status == UserStatus.Active;
    public bool CanBeModified => Status != UserStatus.Deactivated;
}

public enum UserStatus
{
    Pending,
    Active,
    Suspended,
    Deactivated
}