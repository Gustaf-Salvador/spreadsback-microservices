using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SpreadsBack.CommonServices.Core.Utils;

/// <summary>
/// Utilitários para JWT
/// </summary>
public static class JwtUtils
{
    /// <summary>
    /// Extrai o User ID do token JWT
    /// </summary>
    public static string? ExtractUserIdFromToken(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
            return null;

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorizationHeader.Substring("Bearer ".Length);
        
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            // Procurar pelo claim do usuário (pode variar dependendo do provider)
            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.NameIdentifier || 
                c.Type == "sub" || 
                c.Type == "cognito:username");

            return userIdClaim?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extrai claims específicos do token
    /// </summary>
    public static string? ExtractClaimFromToken(string? authorizationHeader, string claimType)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
            return null;

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorizationHeader.Substring("Bearer ".Length);
        
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            var claim = jsonToken.Claims.FirstOrDefault(c => c.Type == claimType);
            return claim?.Value;
        }
        catch
        {
            return null;
        }
    }
}
