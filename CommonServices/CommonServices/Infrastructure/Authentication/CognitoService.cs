using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Logging;

namespace SpreadsBack.CommonServices.Infrastructure.Authentication;

/// <summary>
/// Configurações do Cognito
/// </summary>
public class CognitoSettings
{
    public string UserPoolId { get; set; } = string.Empty;
    public string AppClientId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

/// <summary>
/// Interface para serviços Cognito
/// </summary>
public interface ICognitoService
{
    Task<bool> ValidateUserIdAgainstTokenAsync(string userId, string jwtToken);
    Task<string?> GetUserIdFromTokenAsync(string jwtToken);
}

/// <summary>
/// Implementação base do serviço Cognito
/// </summary>
public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoProvider;
    private readonly CognitoSettings _settings;
    private readonly ILogger<CognitoService> _logger;

    public CognitoService(
        IAmazonCognitoIdentityProvider cognitoProvider, 
        CognitoSettings settings,
        ILogger<CognitoService> logger)
    {
        _cognitoProvider = cognitoProvider;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> ValidateUserIdAgainstTokenAsync(string userId, string jwtToken)
    {
        try
        {
            var tokenUserId = await GetUserIdFromTokenAsync(jwtToken);
            return tokenUserId == userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user ID against token");
            return false;
        }
    }

    public async Task<string?> GetUserIdFromTokenAsync(string jwtToken)
    {
        try
        {
            var request = new GetUserRequest
            {
                AccessToken = jwtToken
            };

            var response = await _cognitoProvider.GetUserAsync(request);
            
            var subAttribute = response.UserAttributes.FirstOrDefault(a => a.Name == "sub");
            return subAttribute?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user ID from token");
            return null;
        }
    }
}
