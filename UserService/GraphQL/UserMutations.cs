using HotChocolate;
using HotChocolate.Types;
using UserService.Services;
using UserService.DTOs;
using UserService.GraphQL.Types;

namespace UserService.GraphQL;

[ExtendObjectType("Mutation")]
public class UserMutations
{
    public async Task<CreateUserPayload> CreateUserAsync(
        CreateUserInput input,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.CreateUserAsync(
                input.Email,
                input.FirstName,
                input.LastName,
                input.PhoneNumber,
                "GraphQL",
                input.CognitoUserId,
                cancellationToken);
            
            if (result.Success)
            {
                return new CreateUserPayload
                {
                    User = result.Value
                };
            }
            
            return new CreateUserPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = result.Error ?? "Failed to create user",
                        Code = "CREATE_FAILED"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new CreateUserPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = ex.Message,
                        Code = "INTERNAL_ERROR"
                    }
                }
            };
        }
    }

    public async Task<UpdateUserPayload> UpdateUserAsync(
        string id,
        UpdateUserInput input,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.UpdateUserProfileAsync(
                id,
                input.FirstName,
                input.LastName,
                input.PhoneNumber,
                "GraphQL",
                cancellationToken);
            
            if (result.Success)
            {
                return new UpdateUserPayload
                {
                    User = result.Value
                };
            }
            
            return new UpdateUserPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = result.Error ?? "Failed to update user",
                        Code = "UPDATE_FAILED"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new UpdateUserPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = ex.Message,
                        Code = "INTERNAL_ERROR"
                    }
                }
            };
        }
    }

    public async Task<UserStatusPayload> ActivateUserAsync(
        string id,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.ActivateUserAsync(id, "GraphQL", cancellationToken: cancellationToken);
            
            if (result.Success)
            {
                return new UserStatusPayload
                {
                    User = result.Value
                };
            }
            
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = result.Error ?? "Failed to activate user",
                        Code = "ACTIVATION_FAILED"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = ex.Message,
                        Code = "INTERNAL_ERROR"
                    }
                }
            };
        }
    }

    public async Task<UserStatusPayload> DeactivateUserAsync(
        string id,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.DeactivateUserAsync(id, "GraphQL", cancellationToken: cancellationToken);
            
            if (result.Success)
            {
                return new UserStatusPayload
                {
                    User = result.Value
                };
            }
            
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = result.Error ?? "Failed to deactivate user",
                        Code = "DEACTIVATION_FAILED"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = ex.Message,
                        Code = "INTERNAL_ERROR"
                    }
                }
            };
        }
    }

    public async Task<UserStatusPayload> SuspendUserAsync(
        string id,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.SuspendUserAsync(id, "GraphQL", cancellationToken: cancellationToken);
            
            if (result.Success)
            {
                return new UserStatusPayload
                {
                    User = result.Value
                };
            }
            
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = result.Error ?? "Failed to suspend user",
                        Code = "SUSPENSION_FAILED"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = ex.Message,
                        Code = "INTERNAL_ERROR"
                    }
                }
            };
        }
    }

    public async Task<UserStatusPayload> ArchiveUserAsync(
        string id,
        [Service] IUserDomainService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await userService.DeleteUserAsync(id, cancellationToken);
            
            if (result.Success)
            {
                return new UserStatusPayload
                {
                    User = null // User is deleted, so no user to return
                };
            }
            
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = result.Error ?? "Failed to archive user",
                        Code = "ARCHIVE_FAILED"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new UserStatusPayload
            {
                Errors = new[]
                {
                    new UserError
                    {
                        Message = ex.Message,
                        Code = "INTERNAL_ERROR"
                    }
                }
            };
        }
    }
}