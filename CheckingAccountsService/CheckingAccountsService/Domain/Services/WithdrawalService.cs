using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Repositories;

namespace CheckingAccountsService.Domain.Services;

public class WithdrawalService
{
    private readonly ICheckingAccountRepository _checkingAccountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IWithdrawalLimitRepository _withdrawalLimitRepository;

    public WithdrawalService(
        ICheckingAccountRepository checkingAccountRepository,
        ITransactionRepository transactionRepository,
        IWithdrawalLimitRepository withdrawalLimitRepository)
    {
        _checkingAccountRepository = checkingAccountRepository;
        _transactionRepository = transactionRepository;
        _withdrawalLimitRepository = withdrawalLimitRepository;
    }

    public async Task<bool> CanWithdrawAsync(string userId, string currencyId, decimal amount)
    {
        var account = await _checkingAccountRepository.GetByUserIdAndCurrencyAsync(userId, currencyId);
        if (account == null || account.Balance < amount)
            return false;

        var limits = await _withdrawalLimitRepository.GetByUserIdAndCurrencyAsync(userId, currencyId);
        if (limits == null)
            return true; // No limits set, withdrawal allowed

        // Check daily limit
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var dailyTotal = await _transactionRepository.GetTotalWithdrawalsForPeriodAsync(userId, currencyId, today, tomorrow);
        if (dailyTotal + amount > limits.DailyLimit)
            return false;

        // Check monthly limit
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
        var monthlyTotal = await _transactionRepository.GetTotalWithdrawalsForPeriodAsync(userId, currencyId, firstDayOfMonth, firstDayOfNextMonth);
        if (monthlyTotal + amount > limits.MonthlyLimit)
            return false;

        return true;
    }

    public async Task<Transaction> ProcessWithdrawalAsync(string userId, string currencyId, decimal amount, string description)
    {
        var account = await _checkingAccountRepository.GetByUserIdAndCurrencyAsync(userId, currencyId);
        if (account == null)
            throw new InvalidOperationException($"Checking account not found for user {userId} and currency {currencyId}");

        var canWithdraw = await CanWithdrawAsync(userId, currencyId, amount);
        if (!canWithdraw)
            throw new InvalidOperationException("Cannot withdraw: either insufficient funds or withdrawal limits exceeded");

        account.Withdraw(amount);
        await _checkingAccountRepository.UpdateAsync(account);

        var transaction = new Transaction(
            account.Id,
            userId,
            currencyId,
            amount,
            TransactionType.Withdrawal,
            description
        );

        await _transactionRepository.CreateAsync(transaction);
        return transaction;
    }
}