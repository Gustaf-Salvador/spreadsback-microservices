using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using CheckingAccountsService.API;
using CheckingAccountsService.Infrastructure.Logging;
using CheckingAccountsService.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CheckingAccountsService;

public class SqsMessageProcessor
{
    private readonly IServiceProvider _serviceProvider;
    
    public SqsMessageProcessor()
    {
        // Configure services
        var services = DependencyInjection.ConfigureServices();
        _serviceProvider = services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Lambda function handler for processing SQS messages
    /// </summary>
    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        // Create a scope for this request to properly handle scoped services
        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        
        // Get services from the scoped provider
        var eventTracker = scopedProvider.GetRequiredService<IEventTracker>();
        var logger = scopedProvider.GetRequiredService<ILogger<SqsEventHandler>>();
        
        // Create the SQS event handler
        var sqsEventHandler = new SqsEventHandler(scopedProvider, eventTracker, logger);
        
        // Process the batch of messages
        return await sqsEventHandler.ProcessMessagesAsync(evnt, context);
    }
}