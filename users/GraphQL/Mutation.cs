using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;

namespace UserService.GraphQL;

public class Mutation
{
    public async Task<UserResponse> CreateUser(CreateUserInput input, [Service] IUserRepository userRepository)
    {
        // Check if user already exists
        var existingUser = await userRepository.GetByEmailAsync(input.Email);
        if (existingUser != null)
        {
            throw new GraphQLException("User with this email already exists");
        }

        var user = new User
        {
            Email = input.Email,
            FirstName = input.FirstName,
            LastName = input.LastName,
            PhoneNumber = input.PhoneNumber,
            Metadata = input.Metadata ?? new Dictionary<string, object>(),
            Status = Models.UserStatus.Pending
        };

        var createdUser = await userRepository.CreateAsync(user);
        
        return new UserResponse(
            createdUser.Id,
            createdUser.Email,
            createdUser.FirstName,
            createdUser.LastName,
            createdUser.PhoneNumber,
            createdUser.CreatedAt,
            createdUser.UpdatedAt,
            (DTOs.UserStatus)createdUser.Status,
            createdUser.EmailVerified,
            createdUser.ProfilePictureUrl,
            createdUser.Metadata
        );
    }

    public async Task<UserResponse?> UpdateUser(UpdateUserInput input, [Service] IUserRepository userRepository)
    {
        var user = await userRepository.GetByIdAsync(input.Id);
        if (user == null)
        {
            throw new GraphQLException("User not found");
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(input.FirstName))
            user.FirstName = input.FirstName;
        
        if (!string.IsNullOrEmpty(input.LastName))
            user.LastName = input.LastName;
        
        if (!string.IsNullOrEmpty(input.PhoneNumber))
            user.PhoneNumber = input.PhoneNumber;
        
        if (!string.IsNullOrEmpty(input.ProfilePictureUrl))
            user.ProfilePictureUrl = input.ProfilePictureUrl;
        
        if (input.Status.HasValue)
            user.Status = (Models.UserStatus)input.Status.Value;
        
        if (input.EmailVerified.HasValue)
            user.EmailVerified = input.EmailVerified.Value;
        
        if (input.Metadata != null)
        {
            foreach (var kvp in input.Metadata)
            {
                user.Metadata[kvp.Key] = kvp.Value;
            }
        }

        var updatedUser = await userRepository.UpdateAsync(user);
        
        return new UserResponse(
            updatedUser.Id,
            updatedUser.Email,
            updatedUser.FirstName,
            updatedUser.LastName,
            updatedUser.PhoneNumber,
            updatedUser.CreatedAt,
            updatedUser.UpdatedAt,
            (DTOs.UserStatus)updatedUser.Status,
            updatedUser.EmailVerified,
            updatedUser.ProfilePictureUrl,
            updatedUser.Metadata
        );
    }

    public async Task<bool> DeleteUser(Guid id, [Service] IUserRepository userRepository)
    {
        return await userRepository.DeleteAsync(id);
    }

    public async Task<UserResponse?> VerifyEmail(Guid userId, [Service] IUserRepository userRepository)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new GraphQLException("User not found");
        }

        user.EmailVerified = true;
        user.Status = Models.UserStatus.Active;
        
        var updatedUser = await userRepository.UpdateAsync(user);
        
        return new UserResponse(
            updatedUser.Id,
            updatedUser.Email,
            updatedUser.FirstName,
            updatedUser.LastName,
            updatedUser.PhoneNumber,
            updatedUser.CreatedAt,
            updatedUser.UpdatedAt,
            (DTOs.UserStatus)updatedUser.Status,
            updatedUser.EmailVerified,
            updatedUser.ProfilePictureUrl,
            updatedUser.Metadata
        );
    }
}