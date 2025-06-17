using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using MediatR;

namespace CheckingAccountsService.Application.Transactions.Queries;

public record GetTransactionsQuery : IRequest<ApiResponse<PaginatedResult<TransactionDto>>>
{
    public string UserId { get; init; } = string.Empty;
    public string? CurrencyId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Skip { get; init; } = 0;
    public int Top { get; init; } = 10;
}