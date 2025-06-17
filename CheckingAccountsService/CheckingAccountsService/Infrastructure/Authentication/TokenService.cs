using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Amazon.Lambda.APIGatewayEvents;

namespace CheckingAccountsService.Infrastructure.Authentication;

public interface ITokenService
{
    string ExtractToken(APIGatewayProxyRequest request);
    (bool isValid, ClaimsPrincipal? principal) ValidateToken(string token);
    string? GetClaim(ClaimsPrincipal principal, string claimType);
}

public class TokenService : ITokenService
{
    private readonly CognitoSettings _cognitoSettings;

    public TokenService(CognitoSettings cognitoSettings)
    {
        _cognitoSettings = cognitoSettings;
    }

    public string ExtractToken(APIGatewayProxyRequest request)
    {
        // Try to get the token from the Authorization header
        if (request.Headers != null && 
            request.Headers.TryGetValue("Authorization", out var authHeader) && 
            !string.IsNullOrEmpty(authHeader))
        {
            // Bearer token format: "Bearer {token}"
            var parts = authHeader.Split(' ');
            if (parts.Length == 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                return parts[1];
            }
        }

        // If no token in Authorization header, check for a token in the query string
        if (request.QueryStringParameters != null && 
            request.QueryStringParameters.TryGetValue("token", out var tokenParam) && 
            !string.IsNullOrEmpty(tokenParam))
        {
            return tokenParam;
        }

        // If still no token, return empty string
        return string.Empty;
    }

    public (bool isValid, ClaimsPrincipal? principal) ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return (false, null);
            }

            // For this example, we're just reading the claims from the token
            // In a production environment, you would validate the token signature, issuer, audience, etc.
            var tokenHandler = new JwtSecurityTokenHandler();
            
            if (!tokenHandler.CanReadToken(token))
            {
                return (false, null);
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);
            var claims = jwtToken.Claims;
            
            // Create claims identity
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
            
            return (true, principal);
        }
        catch
        {
            return (false, null);
        }
    }

    public string? GetClaim(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }
}