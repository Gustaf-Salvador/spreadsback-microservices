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
        _logger.LogError(error.Exception, "GraphQL Error: {Message}", error.Message);

        return error.Exception switch
        {
            FluentValidation.ValidationException validationEx => 
                ErrorBuilder.New()
                    .SetMessage($"Validation failed: {validationEx.Message}")
                    .SetCode("VALIDATION_ERROR")
                    .Build(),
            
            InvalidOperationException businessEx => 
                ErrorBuilder.New()
                    .SetMessage($"Business rule violation: {businessEx.Message}")
                    .SetCode("BUSINESS_RULE_VIOLATION")
                    .Build(),
            
            ArgumentException argEx => 
                ErrorBuilder.New()
                    .SetMessage($"Invalid argument: {argEx.Message}")
                    .SetCode("INVALID_ARGUMENT")
                    .Build(),
            
            _ => ErrorBuilder.New()
                    .SetMessage("An unexpected error occurred")
                    .SetCode("INTERNAL_ERROR")
                    .Build()
        };
    }
}