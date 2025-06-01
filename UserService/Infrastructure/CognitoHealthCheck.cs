using Microsoft.Extensions.Diagnostics.HealthChecks;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure;

public class CognitoHealthCheck : IHealthCheck
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly CognitoSettings _settings;
    private readonly ILogger<CognitoHealthCheck> _logger;

    public CognitoHealthCheck(
        IAmazonCognitoIdentityProvider cognitoClient,
        IOptions<CognitoSettings> settings,
        ILogger<CognitoHealthCheck> logger)
    {
        _cognitoClient = cognitoClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to describe the user pool to verify connectivity
            var request = new DescribeUserPoolRequest
            {
                UserPoolId = _settings.UserPoolId
            };

            var response = await _cognitoClient.DescribeUserPoolAsync(request, cancellationToken);
            
            if (response.UserPool != null)
            {
                return HealthCheckResult.Healthy("Cognito User Pool is accessible");
            }
            
            return HealthCheckResult.Unhealthy("Cognito User Pool description returned null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cognito health check failed");
            return HealthCheckResult.Unhealthy($"Cognito health check failed: {ex.Message}");
        }
    }
}