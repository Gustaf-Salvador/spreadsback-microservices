using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;
using MediatR;

namespace CheckingAccountsService.Application.Transactions.Queries;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, ApiResponse<PaginatedResult<TransactionDto>>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionsQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<ApiResponse<PaginatedResult<TransactionDto>>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _transactionRepository.GetByUserIdAsync(
                request.UserId,
                request.CurrencyId,
                request.StartDate,
                request.EndDate,
                request.Skip,
                request.Top);

            // Convert to DTOs
            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                CurrencyId = t.CurrencyId,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList();

            // Create paginated result
            // Note: In a real implementation, you would also get the total count from the repository
            // This is a simplified version
            var result = new PaginatedResult<TransactionDto>
            {
                Items = transactionDtos,
                TotalCount = transactionDtos.Count, // This should come from the repository in a real implementation
                PageSize = request.Top,
                CurrentPage = (request.Skip / request.Top) + 1
            };

            return ApiResponse<PaginatedResult<TransactionDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<PaginatedResult<TransactionDto>>.ErrorResponse($"Error retrieving transactions: {ex.Message}");
        }
    }
}