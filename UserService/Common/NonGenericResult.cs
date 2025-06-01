using System;

namespace UserService.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail with an error message.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    
    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new Result(true, string.Empty);
    public static Result Failure(string error) => new Result(false, error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}