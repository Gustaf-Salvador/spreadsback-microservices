namespace CheckingAccountsService.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid CheckingAccountId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string CurrencyId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // For deserialization
    private Transaction() { }

    public Transaction(Guid checkingAccountId, string userId, string currencyId, decimal amount, TransactionType type, string description)
    {
        Id = Guid.NewGuid();
        CheckingAccountId = checkingAccountId;
        UserId = userId;
        CurrencyId = currencyId;
        Amount = amount;
        Type = type;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}

public enum TransactionType
{
    Deposit,
    Withdrawal
}