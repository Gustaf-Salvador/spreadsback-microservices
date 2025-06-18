using SpreadsBack.CommonServices.Domain.Entities;

namespace CheckingAccountsService.Domain.Entities;

public class WithdrawalLimit : FinancialEntity
{
    public decimal DailyLimit { get; private set; }
    public decimal MonthlyLimit { get; private set; }

    // For deserialization
    private WithdrawalLimit() { }

    public WithdrawalLimit(string userId, string currencyId, decimal dailyLimit, decimal monthlyLimit)
    {
        UserId = userId;
        CurrencyId = currencyId;
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
    }

    public void UpdateLimits(decimal dailyLimit, decimal monthlyLimit)
    {
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
        UpdatedAt = DateTime.UtcNow;
    }
}