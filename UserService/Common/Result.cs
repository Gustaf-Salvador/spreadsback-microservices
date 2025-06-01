// Create type alias for cleaner usage
using Result = UserService.Common.OperationResult;

namespace UserService.Common;

public class OperationResult
{
    protected OperationResult(bool success, string error)
    {
        Success = success;
        Error = error;
    }

    public bool Success { get; }
    public string Error { get; } = string.Empty;
    public bool IsFailure => !Success;

    public static OperationResult Ok() => new(true, string.Empty);
    public static OperationResult Fail(string error) => new(false, error);
    public static OperationResult<T> Ok<T>(T value) => new(value, true, string.Empty);
    public static OperationResult<T> Fail<T>(string error) => new(default, false, error);
}

public class OperationResult<T> : OperationResult
{
    private readonly T? _value;

    protected internal OperationResult(T? value, bool success, string error) : base(success, error)
    {
        _value = value;
    }

    public T Value => Success ? _value! : throw new InvalidOperationException("Cannot access value of failed result");

    public static implicit operator OperationResult<T>(T value) => Ok(value);
}

public static class ResultExtensions
{
    public static OperationResult<TOut> Map<TIn, TOut>(this OperationResult<TIn> result, Func<TIn, TOut> mapper)
    {
        return result.Success ? OperationResult.Ok(mapper(result.Value)) : OperationResult.Fail<TOut>(result.Error);
    }

    public static async Task<OperationResult<TOut>> MapAsync<TIn, TOut>(this OperationResult<TIn> result, Func<TIn, Task<TOut>> mapper)
    {
        return result.Success ? OperationResult.Ok(await mapper(result.Value)) : OperationResult.Fail<TOut>(result.Error);
    }

    public static OperationResult<TOut> Bind<TIn, TOut>(this OperationResult<TIn> result, Func<TIn, OperationResult<TOut>> binder)
    {
        return result.Success ? binder(result.Value) : OperationResult.Fail<TOut>(result.Error);
    }

    public static async Task<OperationResult<TOut>> BindAsync<TIn, TOut>(this OperationResult<TIn> result, Func<TIn, Task<OperationResult<TOut>>> binder)
    {
        return result.Success ? await binder(result.Value) : OperationResult.Fail<TOut>(result.Error);
    }
}