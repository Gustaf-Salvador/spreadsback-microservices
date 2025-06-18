using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SpreadsBack.CommonServices.Infrastructure.Logging;

/// <summary>
/// Logger adapter para Lambda que implementa ILogger do Microsoft.Extensions.Logging
/// </summary>
public class LambdaLoggerAdapter<T> : ILogger<T>
{
    private readonly ILambdaLogger _lambdaLogger;

    public LambdaLoggerAdapter(ILambdaLogger lambdaLogger)
    {
        _lambdaLogger = lambdaLogger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null; // Lambda doesn't support scopes
    }

    public bool IsEnabled(MsLogLevel logLevel)
    {
        return true; // Lambda logs everything
    }

    public void Log<TState>(MsLogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var logLevelString = logLevel switch
        {
            MsLogLevel.Trace => "TRACE",
            MsLogLevel.Debug => "DEBUG",
            MsLogLevel.Information => "INFO",
            MsLogLevel.Warning => "WARN",
            MsLogLevel.Error => "ERROR",
            MsLogLevel.Critical => "CRITICAL",
            _ => "INFO"
        };

        var formattedMessage = $"[{logLevelString}] [{typeof(T).Name}] {message}";
        
        if (exception != null)
        {
            formattedMessage += $" Exception: {exception}";
        }

        _lambdaLogger.LogInformation(formattedMessage);
    }
}

/// <summary>
/// Factory para criar loggers Lambda
/// </summary>
public class LambdaLoggerFactory : ILoggerFactory
{
    private readonly ILambdaLogger _lambdaLogger;

    public LambdaLoggerFactory(ILambdaLogger lambdaLogger)
    {
        _lambdaLogger = lambdaLogger;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // Not supported in Lambda
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new LambdaLoggerGeneric(_lambdaLogger, categoryName);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Logger genérico para Lambda
/// </summary>
public class LambdaLoggerGeneric : ILogger
{
    private readonly ILambdaLogger _lambdaLogger;
    private readonly string _categoryName;

    public LambdaLoggerGeneric(ILambdaLogger lambdaLogger, string categoryName)
    {
        _lambdaLogger = lambdaLogger;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(MsLogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(MsLogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var logLevelString = logLevel switch
        {
            MsLogLevel.Trace => "TRACE",
            MsLogLevel.Debug => "DEBUG",
            MsLogLevel.Information => "INFO",
            MsLogLevel.Warning => "WARN",
            MsLogLevel.Error => "ERROR",
            MsLogLevel.Critical => "CRITICAL",
            _ => "INFO"
        };

        var formattedMessage = $"[{logLevelString}] [{_categoryName}] {message}";
        
        if (exception != null)
        {
            formattedMessage += $" Exception: {exception}";
        }

        _lambdaLogger.LogInformation(formattedMessage);
    }
}
