using Amazon.Lambda.APIGatewayEvents;
using CheckingAccountsService.Infrastructure.Authentication;
using System.Net;
using System.Text.Json;

namespace CheckingAccountsService.API.Middleware;

public class JwtValidationMiddleware
{
    private readonly Func<APIGatewayProxyRequest, Func<Task<APIGatewayProxyResponse>>, Task<APIGatewayProxyResponse>> _next;
    private readonly ITokenService _tokenService;
    private readonly ICognitoService _cognitoService;

    public JwtValidationMiddleware(
        Func<APIGatewayProxyRequest, Func<Task<APIGatewayProxyResponse>>, Task<APIGatewayProxyResponse>> next,
        ITokenService tokenService,
        ICognitoService cognitoService)
    {
        _next = next;
        _tokenService = tokenService;
        _cognitoService = cognitoService;
    }

    public async Task<APIGatewayProxyResponse> Invoke(APIGatewayProxyRequest request, Func<Task<APIGatewayProxyResponse>> handler)
    {
        // Skip authentication for non-user endpoints if needed
        if (!request.Path.StartsWith("/users/"))
        {
            return await _next(request, handler);
        }

        // Extract the token
        var token = _tokenService.ExtractToken(request);
        if (string.IsNullOrEmpty(token))
        {
            return CreateUnauthorizedResponse("No authentication token provided");
        }

        // Validate the token
        var (isValid, principal) = _tokenService.ValidateToken(token);
        if (!isValid || principal == null)
        {
            return CreateUnauthorizedResponse("Invalid authentication token");
        }

        // Extract user id from the path
        var parts = request.Path.Split('/');
        if (parts.Length < 3)
        {
            return CreateUnauthorizedResponse("Invalid request path");
        }

        var userId = parts[2];

        // Validate the user ID against the token
        var isUserValid = await _cognitoService.ValidateUserIdAgainstTokenAsync(userId, token);
        if (!isUserValid)
        {
            return CreateUnauthorizedResponse("User ID does not match token");
        }

        // Proceed with the request
        return await _next(request, handler);
    }

    private APIGatewayProxyResponse CreateUnauthorizedResponse(string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Body = JsonSerializer.Serialize(new { message }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}