using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpreadsBack.CommonServices.Core.Models;
using System.Net;
using System.Text.Json;

namespace SpreadsBack.CommonServices.Infrastructure.Lambda;

/// <summary>
/// Handler base para API Gateway com roteamento automático
/// </summary>
public abstract class BaseApiGatewayHandler
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;
    protected readonly IMediator Mediator;

    protected BaseApiGatewayHandler(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Logger = serviceProvider.GetRequiredService<ILogger<BaseApiGatewayHandler>>();
        Mediator = serviceProvider.GetRequiredService<IMediator>();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request, 
        ILambdaContext context)
    {
        try
        {
            Logger.LogInformation("Processing request: {Path}, Method: {Method}", 
                request.Path, request.HttpMethod);

            // Validação básica
            if (string.IsNullOrEmpty(request.Path) || string.IsNullOrEmpty(request.HttpMethod))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request");
            }

            // Autenticação/Autorização (se necessário)
            var authResult = await ValidateAuthenticationAsync(request);
            if (!authResult.IsSuccess)
            {
                return CreateErrorResponse(HttpStatusCode.Unauthorized, authResult.Message);
            }

            // Roteamento
            var routeResult = await RouteRequestAsync(request, context);
            return routeResult;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error processing request");
            return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Valida autenticação e autorização
    /// </summary>
    protected virtual async Task<AuthResult> ValidateAuthenticationAsync(APIGatewayProxyRequest request)
    {
        // Implementação padrão - sempre autorizado
        // Override em implementações específicas se necessário
        await Task.CompletedTask;
        return AuthResult.Success();
    }

    /// <summary>
    /// Roteia a requisição para o handler apropriado
    /// </summary>
    protected abstract Task<APIGatewayProxyResponse> RouteRequestAsync(
        APIGatewayProxyRequest request, 
        ILambdaContext context);    /// <summary>
    /// Cria uma resposta de sucesso
    /// </summary>
    protected APIGatewayProxyResponse CreateSuccessResponse<T>(T data, string message = "Operation completed successfully")
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(response),
            Headers = GetDefaultHeaders()
        };
    }

    /// <summary>
    /// Cria uma resposta de erro
    /// </summary>
    protected APIGatewayProxyResponse CreateErrorResponse(HttpStatusCode statusCode, string message, List<string>? errors = null)
    {
        var response = ApiResponse<object>.ErrorResponse(message, errors);
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(response),
            Headers = GetDefaultHeaders()
        };
    }

    /// <summary>
    /// Cria resposta a partir de ApiResponse
    /// </summary>
    protected APIGatewayProxyResponse CreateApiResponse<T>(ApiResponse<T> apiResponse)
    {
        var statusCode = apiResponse.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(apiResponse),
            Headers = GetDefaultHeaders()
        };
    }

    /// <summary>
    /// Headers padrão para todas as respostas
    /// </summary>
    protected virtual Dictionary<string, string> GetDefaultHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "Access-Control-Allow-Origin", "*" },
            { "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
            { "Access-Control-Allow-Headers", "Content-Type, Authorization" }
        };
    }

    /// <summary>
    /// Extrai parâmetros da URL
    /// </summary>
    protected Dictionary<string, string> ExtractPathParameters(string path, string template)
    {
        var parameters = new Dictionary<string, string>();
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var templateParts = template.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < Math.Min(pathParts.Length, templateParts.Length); i++)
        {
            if (templateParts[i].StartsWith("{") && templateParts[i].EndsWith("}"))
            {
                var paramName = templateParts[i].Trim('{', '}');
                parameters[paramName] = pathParts[i];
            }
        }

        return parameters;
    }
}

/// <summary>
/// Resultado da validação de autenticação
/// </summary>
public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }

    public static AuthResult Success(string? userId = null)
    {
        return new AuthResult { IsSuccess = true, UserId = userId };
    }

    public static AuthResult Failure(string message)
    {
        return new AuthResult { IsSuccess = false, Message = message };
    }
}
