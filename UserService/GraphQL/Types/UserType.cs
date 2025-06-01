using HotChocolate;
using UserService.Models;

namespace UserService.GraphQL.Types;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>();
        descriptor.Field(f => f.Email).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.FirstName).Type<StringType>();
        descriptor.Field(f => f.LastName).Type<StringType>();
        descriptor.Field(f => f.Status).Type<NonNullType<EnumType<UserStatus>>>();
        descriptor.Field(f => f.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(f => f.UpdatedAt).Type<DateTimeType>();
        descriptor.Field(f => f.LastLoginAt).Type<DateTimeType>();
    }
}

// Payload types for mutations
public class CreateUserPayload
{
    public User? User { get; set; }
    public IReadOnlyList<UserError>? Errors { get; set; }
}

public class UpdateUserPayload
{
    public User? User { get; set; }
    public IReadOnlyList<UserError>? Errors { get; set; }
}

public class UserStatusPayload
{
    public User? User { get; set; }
    public IReadOnlyList<UserError>? Errors { get; set; }
}

public class DeleteUserPayload
{
    public string? UserId { get; set; }
    public bool Success { get; set; }
    public IReadOnlyList<UserError>? Errors { get; set; }
}

public class UserError
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}