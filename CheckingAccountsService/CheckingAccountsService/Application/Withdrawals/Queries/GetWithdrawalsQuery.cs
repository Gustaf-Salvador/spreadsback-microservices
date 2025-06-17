using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using MediatR;

namespace CheckingAccountsService.Application.Withdrawals.Queries;

public record GetWithdrawalsQuery : IRequest<ApiResponse<PaginatedResult<WithdrawalDto>>>
{
    public string UserId { get; init; } = string.Empty;
    public string CurrencyId { get; init; } = string.Empty;
    public int Skip { get; init; } = 0;
    public int Top { get; init; } = 10;
}