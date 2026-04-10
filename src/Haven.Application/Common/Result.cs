namespace Haven.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot carry an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must carry an error.");

        IsSuccess = isSuccess;
        Error     = error;
    }

    public static Result Success()              => new(true, Error.None);
    public static Result Failure(Error error)   => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value, Error error) : base(true, error)  => _value = value;
    private Result(Error error)               : base(false, error) { }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public static Result<TValue> Success(TValue value) => new(value, Error.None);
    public new static Result<TValue> Failure(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error)  => Failure(error);
}