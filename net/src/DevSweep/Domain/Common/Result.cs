namespace DevSweep.Domain.Common;

public readonly record struct Result<TValue, TError>
{
    private readonly TValue? value;
    private readonly TError? error;
    private readonly bool isSuccess;

    private Result(TValue value)
    {
        this.value = value;
        error = default;
        isSuccess = true;
    }

    private Result(TError error)
    {
        value = default;
        this.error = error;
        isSuccess = false;
    }

    public bool IsSuccess => isSuccess;
    public bool IsFailure => !isSuccess;

    public TValue Value => isSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access Value of failed result");

    public TError Error => !isSuccess
        ? error!
        : throw new InvalidOperationException("Cannot access Error of successful result");

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    public Result<TOut, TError> Map<TOut>(Func<TValue, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return isSuccess
            ? Result<TOut, TError>.Success(mapper(value!))
            : Result<TOut, TError>.Failure(error!);
    }

    public Result<TOut, TError> Bind<TOut>(Func<TValue, Result<TOut, TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return isSuccess
            ? binder(value!)
            : Result<TOut, TError>.Failure(error!);
    }

    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<TError, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return isSuccess ? onSuccess(value!) : onFailure(error!);
    }
}
