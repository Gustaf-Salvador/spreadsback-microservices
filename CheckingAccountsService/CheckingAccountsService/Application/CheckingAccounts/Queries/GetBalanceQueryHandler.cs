using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using CheckingAccountsService.Domain.Repositories;
using MediatR;

namespace CheckingAccountsService.Application.CheckingAccounts.Queries;

public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, ApiResponse<BalanceDto>>
{
    private readonly ICheckingAccountRepository _checkingAccountRepository;

    public GetBalanceQueryHandler(ICheckingAccountRepository checkingAccountRepository)
    {
        _checkingAccountRepository = checkingAccountRepository;
    }

    public async Task<ApiResponse<BalanceDto>> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _checkingAccountRepository.GetByUserIdAndCurrencyAsync(request.UserId, request.CurrencyId);

            if (account == null)
            {
                return ApiResponse<BalanceDto>.ErrorResponse($"No account found for user {request.UserId} with currency {request.CurrencyId}");
            }

            var balanceDto = new BalanceDto
            {
                CurrencyId = account.CurrencyId,
                Balance = account.Balance,
                UpdatedAt = account.UpdatedAt
            };

            return ApiResponse<BalanceDto>.SuccessResponse(balanceDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<BalanceDto>.ErrorResponse($"Error retrieving balance: {ex.Message}");
        }
    }
}