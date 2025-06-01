using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public string? PhoneNumber { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public UserStatus Status { get; set; } = UserStatus.Pending;
    
    public bool EmailVerified { get; set; } = false;
    
    public string? ProfilePictureUrl { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum UserStatus
{
    Pending,
    Active,
    Suspended,
    Deactivated
}