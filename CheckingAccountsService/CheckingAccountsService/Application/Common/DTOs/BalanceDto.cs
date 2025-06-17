namespace CheckingAccountsService.Application.Common.DTOs;

public class BalanceDto
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
}