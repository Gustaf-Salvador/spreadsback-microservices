using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using MediatR;

namespace CheckingAccountsService.Application.Withdrawals.Commands;

public record CreateWithdrawalCommand : IRequest<ApiResponse<WithdrawalDto>>
{
    public string UserId { get; init; } = string.Empty;
    public string CurrencyId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
}