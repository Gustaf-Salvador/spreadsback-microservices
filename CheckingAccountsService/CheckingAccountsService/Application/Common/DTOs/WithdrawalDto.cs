namespace CheckingAccountsService.Application.Common.DTOs;

public class WithdrawalDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}