namespace CheckingAccountsService.Domain.Entities;

public class CheckingAccount
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string CurrencyId { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // For deserialization
    private CheckingAccount() { }

    public CheckingAccount(string userId, string currencyId, decimal initialBalance = 0)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CurrencyId = currencyId;
        Balance = initialBalance;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
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