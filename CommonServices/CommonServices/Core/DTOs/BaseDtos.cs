namespace SpreadsBack.CommonServices.Core.DTOs;

/// <summary>
/// DTO base com propriedades comuns
/// </summary>
public abstract class BaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO base para entidades de usuário
/// </summary>
public abstract class UserOwnedDto : BaseDto
{
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// DTO base para transações financeiras
/// </summary>
public abstract class FinancialDto : UserOwnedDto
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
