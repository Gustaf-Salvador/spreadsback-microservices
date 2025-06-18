namespace SpreadsBack.CommonServices.Core.Models;

/// <summary>
/// Resposta padrão para APIs dos microserviços
/// </summary>
/// <typeparam name="T">Tipo de dados da resposta</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    public static ApiResponse<T> ValidationErrorResponse(List<string> validationErrors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "Validation failed",
            Errors = validationErrors
        };
    }

    public static ApiResponse<T> NotFoundResponse(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message ?? "Resource not found"
        };
    }

    public static ApiResponse<T> UnauthorizedResponse(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message ?? "Unauthorized access"
        };
    }
}
