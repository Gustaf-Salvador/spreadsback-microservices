using FluentValidation;

namespace CheckingAccountsService.Application.CheckingAccounts.Queries;

public class GetBalanceQueryValidator : AbstractValidator<GetBalanceQuery>
{
    public GetBalanceQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.CurrencyId)
            .NotEmpty()
            .WithMessage("Currency ID is required");
    }
}
