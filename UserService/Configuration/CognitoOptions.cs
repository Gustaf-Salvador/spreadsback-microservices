namespace UserService.Configuration;

public class CognitoOptions
{
    public const string SectionName = "AWS:Cognito";
    
    public string UserPoolId { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string ClientId { get; set; } = string.Empty;
}