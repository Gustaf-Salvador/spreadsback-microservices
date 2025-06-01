using HotChocolate;
using Microsoft.Extensions.Logging;

namespace UserService.GraphQL.Filters;

public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        // Log the error for monitoring
        _logger.LogError(error.Exception, "GraphQL Error: {Message}", error.Message);

        // In production, you might want to hide sensitive error details
        // and return generic error messages for security
        if (error.Exception != null)
        {
            return error.WithMessage("An internal server error occurred.")
                       .WithCode("INTERNAL_ERROR")
                       .RemoveException();
        }

        return error;
    }
}