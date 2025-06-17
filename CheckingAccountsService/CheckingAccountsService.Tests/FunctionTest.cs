using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Net;
using Moq;
using CheckingAccountsService.Infrastructure.Authentication;
using Microsoft.Extensions.DependencyInjection;
using CheckingAccountsService.Domain.Repositories;
using CheckingAccountsService.Domain.Entities;
using CheckingAccountsService.Application.Common.Models;

namespace CheckingAccountsService.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestGetTransactions_ReturnsOkResponse()
    {
        // Arrange
        var function = new ApiGatewayHandler();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            Path = "/users/test-user/transactions",
            HttpMethod = "GET",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "top", "10" },
                { "skip", "0" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Headers["Content-Type"]);
        
        // Parse response to verify structure
        var responseObj = JsonSerializer.Deserialize<JsonElement>(response.Body);
        Assert.True(responseObj.TryGetProperty("success", out _));
    }

    [Fact]
    public async Task TestGetBalance_ReturnsOkResponse()
    {
        // Arrange
        var function = new ApiGatewayHandler();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            Path = "/users/test-user/currencies/USD/balances",
            HttpMethod = "GET"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Headers["Content-Type"]);
    }

    [Fact]
    public async Task TestCreateWithdrawal_ReturnsOkResponse()
    {
        // Arrange
        var function = new ApiGatewayHandler();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            Path = "/users/test-user/currencies/USD/withdrawals",
            HttpMethod = "POST",
            Body = JsonSerializer.Serialize(new { Amount = 100.00m, Description = "Test withdrawal" })
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Headers["Content-Type"]);
    }

    [Fact]
    public async Task TestGetWithdrawals_ReturnsOkResponse()
    {
        // Arrange
        var function = new ApiGatewayHandler();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            Path = "/users/test-user/currencies/USD/withdrawals",
            HttpMethod = "GET"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Headers["Content-Type"]);
    }

    [Fact]
    public async Task TestGetWithdrawalLimits_ReturnsOkResponse()
    {
        // Arrange
        var function = new ApiGatewayHandler();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            Path = "/users/test-user/currencies/USD/withdrawals/limits",
            HttpMethod = "GET"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Headers["Content-Type"]);
    }

    [Fact]
    public async Task TestInvalidEndpoint_ReturnsNotFoundResponse()
    {
        // Arrange
        var function = new ApiGatewayHandler();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            Path = "/invalid-path",
            HttpMethod = "GET"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/json", response.Headers["Content-Type"]);
    }

    // Example of how a proper test would look with mocked dependencies
    [Fact(Skip = "Example of proper test with mocked dependencies")]
    public async Task TestGetTransactionsWithMockedDependencies()
    {
        // Mock token service
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(s => s.ExtractToken(It.IsAny<APIGatewayProxyRequest>()))
            .Returns("valid-token");
        mockTokenService.Setup(s => s.ValidateToken("valid-token"))
            .Returns((true, new System.Security.Claims.ClaimsPrincipal()));

        // Mock Cognito service
        var mockCognitoService = new Mock<ICognitoService>();
        mockCognitoService.Setup(s => s.ValidateUserIdAgainstTokenAsync("test-user", "valid-token"))
            .ReturnsAsync(true);

        // Mock transaction repository
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        mockTransactionRepository.Setup(r => r.GetByUserIdAsync(
                "test-user", 
                It.IsAny<string>(), 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>(), 
                It.IsAny<int?>(), 
                It.IsAny<int?>()))
            .ReturnsAsync(new List<Transaction>());

        // Mock service provider
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ITokenService)))
            .Returns(mockTokenService.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ICognitoService)))
            .Returns(mockCognitoService.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ITransactionRepository)))
            .Returns(mockTransactionRepository.Object);

        // Create request
        var request = new APIGatewayProxyRequest
        {
            Path = "/users/test-user/transactions",
            HttpMethod = "GET",
            Headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer valid-token" }
            }
        };

        // Execute and assert
        // Note: In a real test, we would need to inject the mocked service provider into the handler
        // This is just a sketch of how it would look
    }
}
