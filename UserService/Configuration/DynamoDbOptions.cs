namespace UserService.Configuration;

public class DynamoDbOptions
{
    public const string SectionName = "DynamoDB";
    
    public string UserTableName { get; set; } = "Users";
    public string Region { get; set; } = "us-east-1";
    public int ReadCapacityUnits { get; set; } = 5;
    public int WriteCapacityUnits { get; set; } = 5;
}