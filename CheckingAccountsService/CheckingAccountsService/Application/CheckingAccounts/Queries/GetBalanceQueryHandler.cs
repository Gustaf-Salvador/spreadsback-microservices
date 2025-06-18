using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SpreadsBack.CommonServices.Application.Handlers;
using SpreadsBack.CommonServices.Core.Models;

namespace CheckingAccountsService.Application.CheckingAccounts.Queries;

public class GetBalanceQueryHandler : BaseHandler<GetBalanceQuery, BalanceDto>
{
    private readonly ICheckingAccountRepository _checkingAccountRepository;

    public GetBalanceQueryHandler(
        ILogger<GetBalanceQueryHandler> logger,
        IValidator<GetBalanceQuery> validator,
        ICheckingAccountRepository checkingAccountRepository)
        : base(logger, validator)
    {
        _checkingAccountRepository = checkingAccountRepository;
    }

    protected override async Task<ApiResponse<BalanceDto>> ExecuteAsync(
        GetBalanceQuery request, 
        CancellationToken cancellationToken)
    {
        var account = await _checkingAccountRepository.GetByUserIdAndCurrencyAsync(request.UserId, request.CurrencyId);

        if (account == null)
        {
            return ApiResponse<BalanceDto>.NotFoundResponse($"No account found for user {request.UserId} with currency {request.CurrencyId}");
        }

        var balanceDto = new BalanceDto
        {
            CurrencyId = account.CurrencyId,
            Balance = account.Balance,
            UpdatedAt = account.UpdatedAt
        };

        return ApiResponse<BalanceDto>.SuccessResponse(balanceDto);
    }
}