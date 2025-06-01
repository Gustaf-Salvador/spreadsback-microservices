using System;

namespace UserService.Common;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error message.
/// </summary>
/// <typeparam name="T">The type of the value returned in case of success</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<T> Success(T value) => new Result<T>(true, value, string.Empty);
    public static Result<T> Failure(string error) => new Result<T>(false, default, error);
    
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
    
    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<string, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(Value) : await onFailure(Error);
    }
}