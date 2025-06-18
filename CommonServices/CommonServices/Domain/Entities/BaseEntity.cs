namespace SpreadsBack.CommonServices.Domain.Entities;

/// <summary>
/// Entidade base com propriedades comuns
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entidade base com informações de usuário
/// </summary>
public abstract class UserOwnedEntity : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Entidade base para transações financeiras
/// </summary>
public abstract class FinancialEntity : UserOwnedEntity
{
    public string CurrencyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
