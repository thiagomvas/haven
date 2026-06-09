using System.Text.Json.Serialization;

namespace Haven.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    [JsonIgnore] public int StatusCode { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot carry an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    protected Result(int statusCode)
    {
        IsSuccess = statusCode >= 200 && statusCode < 300;
        Error = Error.None;
        StatusCode = statusCode;
    }

    public static Result Success(int statusCode = 200) => new(statusCode);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value, Error error) : base(true, error) => _value = value;

    private Result(Error error) : base(false, error)
    {
    }

    private Result(int statusCode) : base(statusCode)
    {
    }

    private Result(TValue value, int statusCode) : base(statusCode) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public static Result<TValue> Success(TValue value, int statusCode = 200) => new(value, statusCode);
    public new static Result<TValue> Failure(Error error) => new(error);

    public static Result<TValue> CreatedFor(TValue value) => Success(value, 201);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure(error);
    public static implicit operator TValue(Result<TValue> result) => result.Value;
}