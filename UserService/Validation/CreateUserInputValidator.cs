using FluentValidation;
using UserService.DTOs;

namespace UserService.Validators;

public class CreateUserInputValidator : AbstractValidator<CreateUserInput>
{
    public CreateUserInputValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email address is required");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .Length(1, 50)
            .WithMessage("First name must be between 1 and 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .Length(1, 50)
            .WithMessage("Last name must be between 1 and 50 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number must be in valid international format");

        RuleFor(x => x.CognitoUserId)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.CognitoUserId))
            .WithMessage("Cognito User ID cannot be empty when provided");
    }
}