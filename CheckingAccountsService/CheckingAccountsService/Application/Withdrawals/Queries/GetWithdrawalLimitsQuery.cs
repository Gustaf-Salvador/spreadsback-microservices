using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using MediatR;

namespace CheckingAccountsService.Application.Withdrawals.Queries;

public record GetWithdrawalLimitsQuery : IRequest<ApiResponse<WithdrawalLimitDto>>
{
    public string UserId { get; init; } = string.Empty;
    public string CurrencyId { get; init; } = string.Empty;
}