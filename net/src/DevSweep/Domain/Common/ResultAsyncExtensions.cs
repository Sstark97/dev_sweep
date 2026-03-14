namespace DevSweep.Domain.Common;

public static class ResultAsyncExtensions
{
    public static async Task<Result<TOut, TError>> BindAsync<TValue, TError, TOut>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<TOut, TError>>> binder)
    {
        if (result.IsFailure)
            return Result<TOut, TError>.Failure(result.Error);

        return await binder(result.Value);
    }

    public static async Task<Result<TOut, TError>> BindAsync<TValue, TError, TOut>(
        this Task<Result<TValue, TError>> resultTask,
        Func<TValue, Task<Result<TOut, TError>>> binder)
    {
        var result = await resultTask;
        return await result.BindAsync(binder);
    }

    public static async Task<Result<TOut, TError>> MapAsync<TValue, TError, TOut>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TOut>> mapper)
    {
        if (result.IsFailure)
            return Result<TOut, TError>.Failure(result.Error);

        var mapped = await mapper(result.Value);
        return Result<TOut, TError>.Success(mapped);
    }

    public static async Task<Result<TOut, TError>> MapAsync<TValue, TError, TOut>(
        this Task<Result<TValue, TError>> resultTask,
        Func<TValue, TOut> mapper)
    {
        var result = await resultTask;
        return result.Map(mapper);
    }

    public static async Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<Unit, TError>>> sideEffect)
    {
        if (result.IsFailure)
            return result;

        var sideEffectResult = await sideEffect(result.Value);
        if (sideEffectResult.IsFailure)
            return Result<TValue, TError>.Failure(sideEffectResult.Error);

        return result;
    }

    public static async Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Task<Result<TValue, TError>> resultTask,
        Func<TValue, Task<Result<Unit, TError>>> sideEffect)
    {
        var result = await resultTask;
        return await result.TapAsync(sideEffect);
    }
}
