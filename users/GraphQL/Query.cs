using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;

namespace UserService.GraphQL;

public class Query
{
    public async Task<UserResponse?> GetUser(Guid id, [Service] IUserRepository userRepository)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user != null ? MapToResponse(user) : null;
    }

    public async Task<UserResponse?> GetUserByEmail(string email, [Service] IUserRepository userRepository)
    {
        var user = await userRepository.GetByEmailAsync(email);
        return user != null ? MapToResponse(user) : null;
    }

    public async Task<IEnumerable<UserResponse>> GetUsers([Service] IUserRepository userRepository)
    {
        var users = await userRepository.GetAllAsync();
        return users.Select(MapToResponse);
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