namespace DevSweep.Domain.Common;

public readonly record struct Result<TValue, TError>
{
    private readonly TValue? value;
    private readonly TError? error;

    private Result(TValue value)
    {
        this.value = value;
        error = default;
        IsSuccess = true;
    }

    private Result(TError error)
    {
        value = default;
        this.error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access Value of failed result");

    public TError Error => !IsSuccess
        ? error!
        : throw new InvalidOperationException("Cannot access Error of successful result");

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    public Result<TOut, TError> Map<TOut>(Func<TValue, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess
            ? Result<TOut, TError>.Success(mapper(value!))
            : Result<TOut, TError>.Failure(error!);
    }

    public Result<TOut, TError> Bind<TOut>(Func<TValue, Result<TOut, TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess
            ? binder(value!)
            : Result<TOut, TError>.Failure(error!);
    }

    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<TError, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(value!) : onFailure(error!);
    }
}
