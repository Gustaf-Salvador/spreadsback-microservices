using FluentValidation;
using UserService.DTOs;

namespace UserService.Validators;

public class UpdateUserInputValidator : AbstractValidator<UpdateUserInput>
{
    public UpdateUserInputValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .Length(1, 50)
            .WithMessage("First name must be between 1 and 50 characters")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .NotEmpty()
            .Length(1, 50)
            .WithMessage("Last name must be between 1 and 50 characters")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number must be in valid international format");

        RuleFor(x => x.ProfilePictureUrl)
            .MaximumLength(500)
            .WithMessage("Profile picture URL must not exceed 500 characters")
            .Must(BeAValidUrl)
            .WithMessage("Profile picture URL must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ProfilePictureUrl));
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
