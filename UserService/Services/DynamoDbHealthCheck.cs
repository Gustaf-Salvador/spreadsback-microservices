using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace UserService.Services;

public class DynamoDbHealthCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ILogger<DynamoDbHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public DynamoDbHealthCheck(
        IAmazonDynamoDB dynamoDbClient, 
        ILogger<DynamoDbHealthCheck> logger,
        IConfiguration configuration)
    {
        _dynamoDbClient = dynamoDbClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing DynamoDB health check");

            var tableName = _configuration.GetValue<string>("DynamoDB:UserTableName") ?? "Users";
            
            var request = new DescribeTableRequest
            {
                TableName = tableName
            };

            var response = await _dynamoDbClient.DescribeTableAsync(request, cancellationToken);
            
            var isHealthy = response.Table.TableStatus == TableStatus.ACTIVE;
            
            if (isHealthy)
            {
                _logger.LogDebug("DynamoDB health check passed. Table status: {Status}", response.Table.TableStatus);
                
                return HealthCheckResult.Healthy($"DynamoDB table '{tableName}' is active", new Dictionary<string, object>
                {
                    ["TableName"] = tableName,
                    ["TableStatus"] = response.Table.TableStatus.Value,
                    ["ItemCount"] = response.Table.ItemCount,
                    ["TableSizeBytes"] = response.Table.TableSizeBytes
                });
            }
            else
            {
                _logger.LogWarning("DynamoDB health check failed. Table status: {Status}", response.Table.TableStatus);
                
                return HealthCheckResult.Unhealthy($"DynamoDB table '{tableName}' is not active. Status: {response.Table.TableStatus}", 
                    data: new Dictionary<string, object>
                    {
                        ["TableName"] = tableName,
                        ["TableStatus"] = response.Table.TableStatus.Value
                    });
            }
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogError(ex, "DynamoDB table not found during health check");
            return HealthCheckResult.Unhealthy("DynamoDB table not found", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DynamoDB health check failed with exception");
            return HealthCheckResult.Unhealthy("DynamoDB health check failed", ex);
        }
    }
}