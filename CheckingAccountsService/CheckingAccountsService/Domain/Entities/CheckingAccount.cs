using SpreadsBack.CommonServices.Domain.Entities;

namespace CheckingAccountsService.Domain.Entities;

public class CheckingAccount : FinancialEntity
{
    public decimal Balance { get; private set; }

    // For deserialization
    private CheckingAccount() { }

    public CheckingAccount(string userId, string currencyId, decimal initialBalance = 0)
    {
        UserId = userId;
        CurrencyId = currencyId;
        Balance = initialBalance;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive");

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive");

        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}