namespace CheckingAccountsService.Application.Common.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}