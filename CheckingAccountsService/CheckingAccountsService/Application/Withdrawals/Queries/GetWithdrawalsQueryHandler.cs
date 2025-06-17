using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using CheckingAccountsService.Domain.Repositories;
using MediatR;

namespace CheckingAccountsService.Application.Withdrawals.Queries;

public class GetWithdrawalsQueryHandler : IRequestHandler<GetWithdrawalsQuery, ApiResponse<PaginatedResult<WithdrawalDto>>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetWithdrawalsQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<ApiResponse<PaginatedResult<WithdrawalDto>>> Handle(GetWithdrawalsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _transactionRepository.GetByUserIdAsync(
                request.UserId,
                request.CurrencyId,
                null,
                null,
                request.Skip,
                request.Top);

            // Filter only withdrawals
            var withdrawals = transactions
                .Where(t => t.Type == Domain.Entities.TransactionType.Withdrawal)
                .Select(t => new WithdrawalDto
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    CurrencyId = t.CurrencyId,
                    Amount = t.Amount,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                }).ToList();

            var result = new PaginatedResult<WithdrawalDto>
            {
                Items = withdrawals,
                TotalCount = withdrawals.Count, // This should come from the repository in a real implementation
                PageSize = request.Top,
                CurrentPage = (request.Skip / request.Top) + 1
            };

            return ApiResponse<PaginatedResult<WithdrawalDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<PaginatedResult<WithdrawalDto>>.ErrorResponse($"Error retrieving withdrawals: {ex.Message}");
        }
    }
}