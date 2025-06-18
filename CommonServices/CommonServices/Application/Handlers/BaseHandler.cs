using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SpreadsBack.CommonServices.Core.Models;

namespace SpreadsBack.CommonServices.Application.Handlers;

/// <summary>
/// Handler base com funcionalidades comuns para validação e logging
/// </summary>
/// <typeparam name="TRequest">Tipo da requisição</typeparam>
/// <typeparam name="TResponse">Tipo da resposta</typeparam>
public abstract class BaseHandler<TRequest, TResponse> : IRequestHandler<TRequest, ApiResponse<TResponse>>
    where TRequest : IRequest<ApiResponse<TResponse>>
{
    protected readonly ILogger Logger;
    protected readonly IValidator<TRequest>? Validator;

    protected BaseHandler(ILogger logger, IValidator<TRequest>? validator = null)
    {
        Logger = logger;
        Validator = validator;
    }

    public async Task<ApiResponse<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Processing {RequestType} with data: {@Request}", 
                typeof(TRequest).Name, request);

            // Validação se um validador foi fornecido
            if (Validator != null)
            {
                var validationResult = await Validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    Logger.LogWarning("Validation failed for {RequestType}: {Errors}", 
                        typeof(TRequest).Name, string.Join(", ", errors));
                    return ApiResponse<TResponse>.ValidationErrorResponse(errors);
                }
            }

            // Executa a lógica específica
            var result = await ExecuteAsync(request, cancellationToken);
            
            Logger.LogInformation("Successfully processed {RequestType}", typeof(TRequest).Name);
            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access in {RequestType}", typeof(TRequest).Name);
            return ApiResponse<TResponse>.UnauthorizedResponse(ex.Message);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Invalid argument in {RequestType}", typeof(TRequest).Name);
            return ApiResponse<TResponse>.ErrorResponse("Invalid input", new List<string> { ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in {RequestType}", typeof(TRequest).Name);
            return ApiResponse<TResponse>.ErrorResponse("An unexpected error occurred");
        }
    }

    /// <summary>
    /// Implementa a lógica específica do handler
    /// </summary>
    protected abstract Task<ApiResponse<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}
