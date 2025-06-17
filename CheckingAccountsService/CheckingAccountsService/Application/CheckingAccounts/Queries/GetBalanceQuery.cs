using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using MediatR;

namespace CheckingAccountsService.Application.CheckingAccounts.Queries;

public record GetBalanceQuery : IRequest<ApiResponse<BalanceDto>>
{
    public string UserId { get; init; } = string.Empty;
    public string CurrencyId { get; init; } = string.Empty;
}