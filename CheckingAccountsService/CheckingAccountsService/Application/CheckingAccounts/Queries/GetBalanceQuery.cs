using CheckingAccountsService.Application.Common.DTOs;
using SpreadsBack.CommonServices.Application.Queries;

namespace CheckingAccountsService.Application.CheckingAccounts.Queries;

public record GetBalanceQuery : IQuery<BalanceDto>
{
    public string UserId { get; init; } = string.Empty;
    public string CurrencyId { get; init; } = string.Empty;
}