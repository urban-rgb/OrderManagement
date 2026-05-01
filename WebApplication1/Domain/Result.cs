namespace WebApplication1.Domain;
using System.Diagnostics.CodeAnalysis;

public enum ErrorType
{
    None = 0,
    Validation = 400,
    NotFound = 404,
    Conflict = 409,
    Failure = 500
}

[ExcludeFromCodeCoverage]
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string ErrorMessage { get; }
    public ErrorType ErrorType { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        ErrorMessage = string.Empty;
        ErrorType = ErrorType.None;
    }

    private Result(string error, ErrorType errorType)
    {
        IsSuccess = false;
        Value = default;
        ErrorMessage = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, ErrorType type = ErrorType.Failure) => new(error, type);
}