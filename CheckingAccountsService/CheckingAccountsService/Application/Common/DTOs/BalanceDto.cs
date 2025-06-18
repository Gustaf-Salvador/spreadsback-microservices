using SpreadsBack.CommonServices.Core.DTOs;

namespace CheckingAccountsService.Application.Common.DTOs;

public class BalanceDto : BaseDto
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public new DateTime UpdatedAt { get; set; } // Hide inherited UpdatedAt to provide custom behavior
}