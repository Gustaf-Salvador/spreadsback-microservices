using UserService.Services;
using UserService.Models;
using UserService.DTOs;
using HotChocolate;

namespace UserService.GraphQL;

public class UserQueries
{
    public async Task<User?> GetUserAsync(
        string id,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetUserByIdAsync(id, cancellationToken);
        return result.Success ? result.Value : null;
    }

    public async Task<User?> GetUserByEmailAsync(
        string email,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetUserByEmailAsync(email, cancellationToken);
        return result.Success ? result.Value : null;
    }

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IEnumerable<User>> GetUsersAsync(
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetAllUsersAsync(cancellationToken);
        return result.Success ? result.Value : Enumerable.Empty<User>();
    }

    public async Task<bool> IsEmailAvailableAsync(
        string email,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.IsEmailAvailableAsync(email, cancellationToken);
        return result.Success && result.Value;
    }
}