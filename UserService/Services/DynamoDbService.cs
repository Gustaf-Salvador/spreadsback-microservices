using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace UserService.Services;

public interface IDynamoDbService
{
    Task PutItemAsync(string tableName, Dictionary<string, object> item);
    Task<Dictionary<string, object>?> GetItemAsync(string tableName, Dictionary<string, object> key);
    Task<List<Dictionary<string, object>>> QueryAsync(string tableName, string keyConditionExpression, Dictionary<string, object> expressionAttributeValues);
    Task<List<Dictionary<string, object>>> ScanAsync(string tableName, string filterExpression, Dictionary<string, object> expressionAttributeValues);
    Task UpdateItemAsync(string tableName, Dictionary<string, object> key, string updateExpression, Dictionary<string, object> expressionAttributeValues);
    Task DeleteItemAsync(string tableName, Dictionary<string, object> key);
}

public class DynamoDbService : IDynamoDbService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ILogger<DynamoDbService> _logger;

    public DynamoDbService(IAmazonDynamoDB dynamoDbClient, ILogger<DynamoDbService> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _logger = logger;
    }

    public async Task PutItemAsync(string tableName, Dictionary<string, object> item)
    {
        try
        {
            var request = new PutItemRequest
            {
                TableName = tableName,
                Item = ConvertToAttributeValueMap(item)
            };

            await _dynamoDbClient.PutItemAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put item in table {TableName}", tableName);
            throw;
        }
    }

    public async Task<Dictionary<string, object>?> GetItemAsync(string tableName, Dictionary<string, object> key)
    {
        try
        {
            var request = new GetItemRequest
            {
                TableName = tableName,
                Key = ConvertToAttributeValueMap(key)
            };

            var response = await _dynamoDbClient.GetItemAsync(request);
            
            return response.Item?.Count > 0 ? ConvertFromAttributeValueMap(response.Item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get item from table {TableName}", tableName);
            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> QueryAsync(string tableName, string keyConditionExpression, Dictionary<string, object> expressionAttributeValues)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = keyConditionExpression,
                ExpressionAttributeValues = ConvertToAttributeValueMap(expressionAttributeValues)
            };

            var response = await _dynamoDbClient.QueryAsync(request);
            
            return response.Items.Select(ConvertFromAttributeValueMap).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query table {TableName}", tableName);
            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> ScanAsync(string tableName, string filterExpression, Dictionary<string, object> expressionAttributeValues)
    {
        try
        {
            var request = new ScanRequest
            {
                TableName = tableName,
                FilterExpression = filterExpression,
                ExpressionAttributeValues = ConvertToAttributeValueMap(expressionAttributeValues)
            };

            var response = await _dynamoDbClient.ScanAsync(request);
            
            return response.Items.Select(ConvertFromAttributeValueMap).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan table {TableName}", tableName);
            throw;
        }
    }

    public async Task UpdateItemAsync(string tableName, Dictionary<string, object> key, string updateExpression, Dictionary<string, object> expressionAttributeValues)
    {
        try
        {
            var request = new UpdateItemRequest
            {
                TableName = tableName,
                Key = ConvertToAttributeValueMap(key),
                UpdateExpression = updateExpression,
                ExpressionAttributeValues = ConvertToAttributeValueMap(expressionAttributeValues)
            };

            await _dynamoDbClient.UpdateItemAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update item in table {TableName}", tableName);
            throw;
        }
    }

    public async Task DeleteItemAsync(string tableName, Dictionary<string, object> key)
    {
        try
        {
            var request = new DeleteItemRequest
            {
                TableName = tableName,
                Key = ConvertToAttributeValueMap(key)
            };

            await _dynamoDbClient.DeleteItemAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete item from table {TableName}", tableName);
            throw;
        }
    }

    private static Dictionary<string, AttributeValue> ConvertToAttributeValueMap(Dictionary<string, object> item)
    {
        var attributeValueMap = new Dictionary<string, AttributeValue>();

        foreach (var kvp in item)
        {
            attributeValueMap[kvp.Key] = kvp.Value switch
            {
                string s => new AttributeValue { S = s },
                int i => new AttributeValue { N = i.ToString() },
                long l => new AttributeValue { N = l.ToString() },
                bool b => new AttributeValue { BOOL = b },
                DateTime dt => new AttributeValue { S = dt.ToString("O") },
                DateTimeOffset dto => new AttributeValue { S = dto.ToString("O") },
                null => new AttributeValue { NULL = true },
                _ => new AttributeValue { S = kvp.Value.ToString() }
            };
        }

        return attributeValueMap;
    }

    private static Dictionary<string, object> ConvertFromAttributeValueMap(Dictionary<string, AttributeValue> item)
    {
        var objectMap = new Dictionary<string, object>();

        foreach (var kvp in item)
        {
            if (kvp.Value.NULL)
            {
                objectMap[kvp.Key] = null!;
            }
            else if (!string.IsNullOrEmpty(kvp.Value.S))
            {
                objectMap[kvp.Key] = kvp.Value.S;
            }
            else if (!string.IsNullOrEmpty(kvp.Value.N))
            {
                objectMap[kvp.Key] = kvp.Value.N;
            }
            else if (kvp.Value.BOOL)
            {
                objectMap[kvp.Key] = kvp.Value.BOOL;
            }
        }

        return objectMap;
    }
}