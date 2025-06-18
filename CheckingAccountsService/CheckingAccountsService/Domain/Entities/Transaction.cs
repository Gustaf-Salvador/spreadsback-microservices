using SpreadsBack.CommonServices.Domain.Entities;

namespace CheckingAccountsService.Domain.Entities;

public class Transaction : FinancialEntity
{
    public Guid CheckingAccountId { get; private set; }
    public new decimal Amount { get; private set; } // Hide inherited Amount from FinancialEntity
    public TransactionType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;

    // For deserialization
    private Transaction() { }

    public Transaction(Guid checkingAccountId, string userId, string currencyId, decimal amount, TransactionType type, string description)
    {
        CheckingAccountId = checkingAccountId;
        UserId = userId;
        CurrencyId = currencyId;
        Amount = amount;
        Type = type;
        Description = description;
    }
}

public enum TransactionType
{
    Deposit,
    Withdrawal
}