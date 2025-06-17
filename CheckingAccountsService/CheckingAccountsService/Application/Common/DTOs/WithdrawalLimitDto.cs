namespace CheckingAccountsService.Application.Common.DTOs;

public class WithdrawalLimitDto
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal DailyUsed { get; set; }
    public decimal MonthlyUsed { get; set; }
    public decimal DailyRemaining => DailyLimit - DailyUsed;
    public decimal MonthlyRemaining => MonthlyLimit - MonthlyUsed;
}