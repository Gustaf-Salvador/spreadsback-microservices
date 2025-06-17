using CheckingAccountsService.Application.Common.DTOs;
using CheckingAccountsService.Application.Common.Models;
using CheckingAccountsService.Domain.Repositories;
using MediatR;

namespace CheckingAccountsService.Application.Withdrawals.Queries;

public class GetWithdrawalLimitsQueryHandler : IRequestHandler<GetWithdrawalLimitsQuery, ApiResponse<WithdrawalLimitDto>>
{
    private readonly IWithdrawalLimitRepository _withdrawalLimitRepository;
    private readonly ITransactionRepository _transactionRepository;

    public GetWithdrawalLimitsQueryHandler(
        IWithdrawalLimitRepository withdrawalLimitRepository,
        ITransactionRepository transactionRepository)
    {
        _withdrawalLimitRepository = withdrawalLimitRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<ApiResponse<WithdrawalLimitDto>> Handle(GetWithdrawalLimitsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var limits = await _withdrawalLimitRepository.GetByUserIdAndCurrencyAsync(request.UserId, request.CurrencyId);

            if (limits == null)
            {
                return ApiResponse<WithdrawalLimitDto>.ErrorResponse($"No withdrawal limits found for user {request.UserId} with currency {request.CurrencyId}");
            }

            // Calculate used amounts for today
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var dailyUsed = await _transactionRepository.GetTotalWithdrawalsForPeriodAsync(
                request.UserId, request.CurrencyId, today, tomorrow);

            // Calculate used amounts for this month
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
            var monthlyUsed = await _transactionRepository.GetTotalWithdrawalsForPeriodAsync(
                request.UserId, request.CurrencyId, firstDayOfMonth, firstDayOfNextMonth);

            var limitDto = new WithdrawalLimitDto
            {
                CurrencyId = limits.CurrencyId,
                DailyLimit = limits.DailyLimit,
                MonthlyLimit = limits.MonthlyLimit,
                DailyUsed = dailyUsed,
                MonthlyUsed = monthlyUsed
            };

            return ApiResponse<WithdrawalLimitDto>.SuccessResponse(limitDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<WithdrawalLimitDto>.ErrorResponse($"Error retrieving withdrawal limits: {ex.Message}");
        }
    }
}