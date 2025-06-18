namespace CheckingAccountsService.Infrastructure.Logging;

/// <summary>
/// Interface for logging
/// </summary>
public interface ILogger<T>
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(Exception exception, string message);
}

/// <summary>
/// Lambda logger implementation
/// </summary>
public class LambdaLogger<T> : ILogger<T>
{
    private readonly Amazon.Lambda.Core.ILambdaLogger _lambdaLogger;

    public LambdaLogger(Amazon.Lambda.Core.ILambdaLogger lambdaLogger)
    {
        _lambdaLogger = lambdaLogger;
    }

    public void LogInformation(string message)
    {
        _lambdaLogger.LogInformation($"[INFO] [{typeof(T).Name}] {message}");
    }

    public void LogWarning(string message)
    {
        _lambdaLogger.LogWarning($"[WARN] [{typeof(T).Name}] {message}");
    }

    public void LogError(Exception exception, string message)
    {
        _lambdaLogger.LogError($"[ERROR] [{typeof(T).Name}] {message}. Exception: {exception}");
    }
}