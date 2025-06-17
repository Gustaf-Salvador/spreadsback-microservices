namespace CheckingAccountsService.Infrastructure.Authentication;

public class CognitoSettings
{
    public string UserPoolId { get; set; } = string.Empty;
    public string AppClientId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}