namespace SpreadsBack.CommonServices.Core.Utils;

/// <summary>
/// Utilitários para validação
/// </summary>
public static class ValidationUtils
{
    /// <summary>
    /// Valida se um GUID é válido
    /// </summary>
    public static bool IsValidGuid(string? value)
    {
        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Valida se um valor decimal é positivo
    /// </summary>
    public static bool IsPositiveAmount(decimal amount)
    {
        return amount > 0;
    }

    /// <summary>
    /// Valida se uma string não está vazia ou nula
    /// </summary>
    public static bool IsNotEmpty(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Valida formato de email básico
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Valida se uma data está no futuro
    /// </summary>
    public static bool IsFutureDate(DateTime date)
    {
        return date > DateTime.UtcNow;
    }

    /// <summary>
    /// Valida se uma data está no passado
    /// </summary>
    public static bool IsPastDate(DateTime date)
    {
        return date < DateTime.UtcNow;
    }
}
