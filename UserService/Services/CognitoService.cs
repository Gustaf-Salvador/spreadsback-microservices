using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace UserService.Services
{
    public interface ICognitoService
    {
        Task<bool> DisableUserAsync(string email);
        Task<bool> EnableUserAsync(string email);
        Task<bool> ConfirmUserEmailAsync(string email);
        Task<bool> SetUserAttributeAsync(string email, string attributeName, string attributeValue);
    }

    public class CognitoService : ICognitoService
    {
        private readonly IAmazonCognitoIdentityProvider _cognitoClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CognitoService> _logger;
        private readonly string _userPoolId;

        public CognitoService(
            IAmazonCognitoIdentityProvider cognitoClient,
            IConfiguration configuration,
            ILogger<CognitoService> logger)
        {
            _cognitoClient = cognitoClient;
            _configuration = configuration;
            _logger = logger;
            _userPoolId = _configuration["AWS:Cognito:UserPoolId"];
        }

        public async Task<bool> DisableUserAsync(string email)
        {
            try
            {
                _logger.LogInformation("Disabling Cognito user with email: {Email}", email);

                var request = new AdminDisableUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };

                await _cognitoClient.AdminDisableUserAsync(request);
                
                _logger.LogInformation("Successfully disabled Cognito user: {Email}", email);
                return true;
            }
            catch (UserNotFoundException)
            {
                _logger.LogWarning("User not found in Cognito: {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling Cognito user: {Email}", email);
                return false;
            }
        }

        public async Task<bool> EnableUserAsync(string email)
        {
            try
            {
                _logger.LogInformation("Enabling Cognito user with email: {Email}", email);

                var request = new AdminEnableUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };

                await _cognitoClient.AdminEnableUserAsync(request);
                
                _logger.LogInformation("Successfully enabled Cognito user: {Email}", email);
                return true;
            }
            catch (UserNotFoundException)
            {
                _logger.LogWarning("User not found in Cognito: {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling Cognito user: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ConfirmUserEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Confirming email for Cognito user: {Email}", email);

                var request = new AdminConfirmSignUpRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };

                await _cognitoClient.AdminConfirmSignUpAsync(request);
                
                _logger.LogInformation("Successfully confirmed email for Cognito user: {Email}", email);
                return true;
            }
            catch (UserNotFoundException)
            {
                _logger.LogWarning("User not found in Cognito: {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for Cognito user: {Email}", email);
                return false;
            }
        }

        public async Task<bool> SetUserAttributeAsync(string email, string attributeName, string attributeValue)
        {
            try
            {
                _logger.LogInformation("Setting attribute {AttributeName} for Cognito user: {Email}", attributeName, email);

                var request = new AdminUpdateUserAttributesRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType
                        {
                            Name = attributeName,
                            Value = attributeValue
                        }
                    }
                };

                await _cognitoClient.AdminUpdateUserAttributesAsync(request);
                
                _logger.LogInformation("Successfully set attribute {AttributeName} for Cognito user: {Email}", attributeName, email);
                return true;
            }
            catch (UserNotFoundException)
            {
                _logger.LogWarning("User not found in Cognito: {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting attribute {AttributeName} for Cognito user: {Email}", attributeName, email);
                return false;
            }
        }
    }
}