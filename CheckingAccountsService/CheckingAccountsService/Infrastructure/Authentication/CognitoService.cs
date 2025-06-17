using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace CheckingAccountsService.Infrastructure.Authentication;

public interface ICognitoService
{
    Task<bool> ValidateUserIdAgainstTokenAsync(string userId, string jwtToken);
}

public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoProvider;
    private readonly CognitoSettings _settings;

    public CognitoService(IAmazonCognitoIdentityProvider cognitoProvider, CognitoSettings settings)
    {
        _cognitoProvider = cognitoProvider;
        _settings = settings;
    }

    public async Task<bool> ValidateUserIdAgainstTokenAsync(string userId, string jwtToken)
    {
        try
        {
            // Get user information from the token
            var request = new GetUserRequest
            {
                AccessToken = jwtToken
            };

            var response = await _cognitoProvider.GetUserAsync(request);
            
            // Find the "sub" attribute in the user attributes
            var subAttribute = response.UserAttributes.FirstOrDefault(a => a.Name == "sub");
            if (subAttribute == null)
            {
                return false;
            }

            // Verify if the sub attribute matches the provided userId
            return subAttribute.Value == userId;
        }
        catch (Exception)
        {
            // If any error occurs during validation, consider it invalid
            return false;
        }
    }
}