using UserService.Models;
using HotChocolate.Types;

namespace UserService.GraphQL;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Description("Represents a user in the system");

        descriptor
            .Field(u => u.Id)
            .Description("The unique identifier of the user");

        descriptor
            .Field(u => u.Email)
            .Description("The email address of the user");

        descriptor
            .Field(u => u.FirstName)
            .Description("The first name of the user");

        descriptor
            .Field(u => u.LastName)
            .Description("The last name of the user");

        descriptor
            .Field(u => u.PhoneNumber)
            .Description("The phone number of the user");

        descriptor
            .Field(u => u.Status)
            .Description("The current status of the user");

        descriptor
            .Field(u => u.EmailVerified)
            .Description("Whether the user's email has been verified");

        descriptor
            .Field(u => u.CreatedAt)
            .Description("When the user was created");

        descriptor
            .Field(u => u.UpdatedAt)
            .Description("When the user was last updated");

        descriptor
            .Field(u => u.CreatedBy)
            .Description("Who created the user");

        descriptor
            .Field(u => u.ModifiedBy)
            .Description("Who last modified the user");

        descriptor
            .Field(u => u.ProfilePictureUrl)
            .Description("URL to the user's profile picture");

        descriptor
            .Field(u => u.CognitoUserId)
            .Description("The user's Cognito ID");

        descriptor
            .Field(u => u.Metadata)
            .Description("Additional metadata for the user");

        // Computed fields
        descriptor
            .Field("fullName")
            .Description("The user's full name")
            .Resolve(context => 
            {
                var user = context.Parent<User>();
                return $"{user.FirstName} {user.LastName}".Trim();
            });

        descriptor
            .Field("isActive")
            .Description("Whether the user is currently active")
            .Resolve(context => 
            {
                var user = context.Parent<User>();
                return user.Status == UserStatus.Active;
            });
    }
}