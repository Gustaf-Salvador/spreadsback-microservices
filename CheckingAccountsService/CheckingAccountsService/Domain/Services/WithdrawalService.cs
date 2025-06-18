using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Domain.Events;
using CheckingAccountsService.Domain.Repositories;

namespace CheckingAccountsService.Domain.Services;

public class WithdrawalService
{
    private readonly ICheckingAccountRepository _checkingAccountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IWithdrawalLimitRepository _withdrawalLimitRepository;
    private readonly IEventPublisher _eventPublisher;

    public WithdrawalService(
        ICheckingAccountRepository checkingAccountRepository,
        ITransactionRepository transactionRepository,
        IWithdrawalLimitRepository withdrawalLimitRepository,
        IEventPublisher eventPublisher)
    {
        _checkingAccountRepository = checkingAccountRepository;
        _transactionRepository = transactionRepository;
        _withdrawalLimitRepository = withdrawalLimitRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<(bool CanWithdraw, string? ReasonIfNot)> CanWithdrawAsync(string userId, string currencyId, decimal amount)
    {
        var account = await _checkingAccountRepository.GetByUserIdAndCurrencyAsync(userId, currencyId);
        if (account == null)
            return (false, "Account not found");
            
        if (account.Balance < amount)
            return (false, "Insufficient funds");

        var limits = await _withdrawalLimitRepository.GetByUserIdAndCurrencyAsync(userId, currencyId);
        if (limits == null)
            return (true, null); // No limits set, withdrawal allowed

        // Check daily limit
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var dailyTotal = await _transactionRepository.GetTotalWithdrawalsForPeriodAsync(userId, currencyId, today, tomorrow);
        if (dailyTotal + amount > limits.DailyLimit)
            return (false, "Daily withdrawal limit exceeded");

        // Check monthly limit
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
        var monthlyTotal = await _transactionRepository.GetTotalWithdrawalsForPeriodAsync(userId, currencyId, firstDayOfMonth, firstDayOfNextMonth);
        if (monthlyTotal + amount > limits.MonthlyLimit)
            return (false, "Monthly withdrawal limit exceeded");

        return (true, null);
    }

    public async Task<Transaction> ProcessWithdrawalAsync(string userId, string currencyId, decimal amount, string description)
    {
        // Check if withdrawal is possible
        var (canWithdraw, reason) = await CanWithdrawAsync(userId, currencyId, amount);
        if (!canWithdraw)
        {
            // Publish withdrawal rejected event
            await _eventPublisher.PublishAsync(new WithdrawalRejectedEvent(userId, currencyId, amount, reason ?? "Unknown reason"));
            throw new InvalidOperationException($"Cannot withdraw: {reason}");
        }

        // Get the account
        var account = await _checkingAccountRepository.GetByUserIdAndCurrencyAsync(userId, currencyId);
        if (account == null)
        {
            // This shouldn't happen as we've already checked, but just in case
            await _eventPublisher.PublishAsync(new WithdrawalRejectedEvent(userId, currencyId, amount, "Account not found"));
            throw new InvalidOperationException($"Checking account not found for user {userId} and currency {currencyId}");
        }

        // Process the withdrawal
        account.Withdraw(amount);
        await _checkingAccountRepository.UpdateAsync(account);

        // Create and save transaction
        var transaction = new Transaction(
            account.Id,
            userId,
            currencyId,
            amount,
            TransactionType.Withdrawal,
            description
        );
        await _transactionRepository.CreateAsync(transaction);

        // Publish withdrawal completed event
        await _eventPublisher.PublishAsync(new WithdrawalCompletedEvent(transaction));

        return transaction;
    }
}