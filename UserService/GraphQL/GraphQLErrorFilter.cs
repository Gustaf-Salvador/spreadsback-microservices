using HotChocolate;
using Microsoft.Extensions.Logging;

namespace UserService.GraphQL;

public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        // Log the error for debugging
        _logger.LogError(error.Exception, "GraphQL error occurred: {Message}", error.Message);

        // Don't expose internal exception details in production
        if (error.Exception != null)
        {
            return error.WithMessage("An internal server error occurred.")
                       .RemoveException();
        }

        return error;
    }
}