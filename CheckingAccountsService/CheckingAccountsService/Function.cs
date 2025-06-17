using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CheckingAccountsService;

public class ApiGatewayHandler
{
    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Log the request
        context.Logger.LogInformation($"Processing request: {request.Path}, Method: {request.HttpMethod}");

        // Confirm that we will receive calls just from the API Gateway
        if (request.Path == null || request.HttpMethod == null)
        {
            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = JsonSerializer.Serialize(new { message = "Bad Request" }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            });
        }

        // Example of routing based on path and method
        if (request.Path.StartsWith("/users/"))
        {
            var parts = request.Path.Split('/');
            if (parts.Length >= 3)
            {
                var userId = parts[2];
                // TODO: Validate userId against JWT and Cognito

                if (request.HttpMethod == "GET" && request.Path.Contains("/transactions"))
                {
                    // GET /users/{userId}/transactions
                    return Task.FromResult(new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = JsonSerializer.Serialize(new { message = $"GET transactions for user {userId}" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    });
                }
                else if (request.HttpMethod == "GET" && request.Path.Contains("/balances"))
                {
                    // GET /users/{userId}/currencies/{currencyId}/balances
                    return Task.FromResult(new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = JsonSerializer.Serialize(new { message = $"GET balances for user {userId}" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    });
                }
                else if (request.HttpMethod == "POST" && request.Path.Contains("/withdrawals"))
                {
                    // POST /users/{userId}/currencies/{currencyId}/withdrawals
                    return Task.FromResult(new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = JsonSerializer.Serialize(new { message = $"POST withdrawal for user {userId}" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    });
                }
                else if (request.HttpMethod == "GET" && request.Path.Contains("/withdrawals") && !request.Path.Contains("/limits"))
                {
                    // GET /users/{userId}/currencies/{currencyId}/withdrawals
                    return Task.FromResult(new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = JsonSerializer.Serialize(new { message = $"GET withdrawals for user {userId}" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    });
                }
                else if (request.HttpMethod == "GET" && request.Path.Contains("/withdrawals/limits"))
                {
                    // GET /users/{userId}/currencies/{currencyId}/withdrawals/limits
                    return Task.FromResult(new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = JsonSerializer.Serialize(new { message = $"GET withdrawal limits for user {userId}" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    });
                }
            }
        }

        return Task.FromResult(new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.NotFound,
            Body = JsonSerializer.Serialize(new { message = "Not Found" }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        });
    }
}