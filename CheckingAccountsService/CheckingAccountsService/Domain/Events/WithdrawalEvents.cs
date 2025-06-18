using CheckingAccountsService.Domain.Entities;

namespace CheckingAccountsService.Domain.Events;

/// <summary>
/// Event triggered when a withdrawal is successfully processed
/// </summary>
public class WithdrawalCompletedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public string UserId { get; }
    public string CurrencyId { get; }
    public decimal Amount { get; }
    public DateTime CreatedAt { get; }
    public string Description { get; }

    public WithdrawalCompletedEvent(Transaction transaction)
    {
        TransactionId = transaction.Id;
        UserId = transaction.UserId;
        CurrencyId = transaction.CurrencyId;
        Amount = transaction.Amount;
        CreatedAt = transaction.CreatedAt;
        Description = transaction.Description;
    }
}

/// <summary>
/// Event triggered when a withdrawal is rejected due to limits or insufficient funds
/// </summary>
public class WithdrawalRejectedEvent : DomainEvent
{
    public string UserId { get; }
    public string CurrencyId { get; }
    public decimal Amount { get; }
    public string Reason { get; }

    public WithdrawalRejectedEvent(string userId, string currencyId, decimal amount, string reason)
    {
        UserId = userId;
        CurrencyId = currencyId;
        Amount = amount;
        Reason = reason;
    }
}