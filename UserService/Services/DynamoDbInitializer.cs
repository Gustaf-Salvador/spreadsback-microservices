using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace UserService.Services;

public interface IDynamoDbInitializer
{
    Task InitializeAsync();
}

public class DynamoDbInitializer : IDynamoDbInitializer, IHostedService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ILogger<DynamoDbInitializer> _logger;
    private readonly IConfiguration _configuration;

    public DynamoDbInitializer(
        IAmazonDynamoDB dynamoDbClient,
        ILogger<DynamoDbInitializer> logger,
        IConfiguration configuration)
    {
        _dynamoDbClient = dynamoDbClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var tableName = _configuration.GetValue<string>("DynamoDB:UserTableName") ?? "Users";
            
            _logger.LogInformation("Initializing DynamoDB table: {TableName}", tableName);

            // Check if table exists
            var tableExists = await CheckTableExistsAsync(tableName);
            
            if (!tableExists)
            {
                await CreateTableAsync(tableName);
                await WaitForTableToBeActiveAsync(tableName);
                _logger.LogInformation("DynamoDB table created successfully: {TableName}", tableName);
            }
            else
            {
                _logger.LogInformation("DynamoDB table already exists: {TableName}", tableName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize DynamoDB table");
            throw;
        }
    }

    private async Task<bool> CheckTableExistsAsync(string tableName)
    {
        try
        {
            var response = await _dynamoDbClient.DescribeTableAsync(tableName);
            return response.Table != null;
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    private async Task CreateTableAsync(string tableName)
    {
        _logger.LogInformation("Creating DynamoDB table: {TableName}", tableName);

        var createTableRequest = new CreateTableRequest
        {
            TableName = tableName,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("Id", KeyType.HASH)
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("Id", ScalarAttributeType.S),
                new AttributeDefinition("EmailGSI", ScalarAttributeType.S)
            },
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new GlobalSecondaryIndex
                {
                    IndexName = "EmailIndex",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement("EmailGSI", KeyType.HASH)
                    },
                    Projection = new Projection
                    {
                        ProjectionType = ProjectionType.ALL
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 5
                    }
                }
            },
            ProvisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 5,
                WriteCapacityUnits = 5
            },
            Tags = new List<Amazon.DynamoDBv2.Model.Tag>
            {
                new Amazon.DynamoDBv2.Model.Tag { Key = "Service", Value = "UserService" },
                new Amazon.DynamoDBv2.Model.Tag { Key = "Environment", Value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }
            }
        };

        await _dynamoDbClient.CreateTableAsync(createTableRequest);
    }

    private async Task WaitForTableToBeActiveAsync(string tableName)
    {
        _logger.LogInformation("Waiting for table to become active: {TableName}", tableName);

        var maxWaitTime = TimeSpan.FromMinutes(5);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            try
            {
                var response = await _dynamoDbClient.DescribeTableAsync(tableName);
                
                if (response.Table.TableStatus == TableStatus.ACTIVE)
                {
                    _logger.LogInformation("Table is now active: {TableName}", tableName);
                    return;
                }

                _logger.LogDebug("Table status: {Status}, waiting...", response.Table.TableStatus);
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking table status: {TableName}", tableName);
                throw;
            }
        }

        throw new TimeoutException($"Table {tableName} did not become active within the expected time");
    }
}