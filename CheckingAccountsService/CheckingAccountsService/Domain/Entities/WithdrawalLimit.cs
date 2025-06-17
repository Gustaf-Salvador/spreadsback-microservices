namespace CheckingAccountsService.Domain.Entities;

public class WithdrawalLimit
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string CurrencyId { get; private set; } = string.Empty;
    public decimal DailyLimit { get; private set; }
    public decimal MonthlyLimit { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // For deserialization
    private WithdrawalLimit() { }

    public WithdrawalLimit(string userId, string currencyId, decimal dailyLimit, decimal monthlyLimit)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CurrencyId = currencyId;
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLimits(decimal dailyLimit, decimal monthlyLimit)
    {
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
        UpdatedAt = DateTime.UtcNow;
    }
}