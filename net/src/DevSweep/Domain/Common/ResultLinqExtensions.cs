namespace DevSweep.Domain.Common;

public static class ResultLinqExtensions
{
    public static Result<TOut, TError> Select<TValue, TError, TOut>(
        this Result<TValue, TError> result,
        Func<TValue, TOut> selector)
        => result.Map(selector);

    public static Result<TOut, TError> SelectMany<TValue, TError, TIntermediate, TOut>(
        this Result<TValue, TError> result,
        Func<TValue, Result<TIntermediate, TError>> binder,
        Func<TValue, TIntermediate, TOut> projector)
        => result.Bind(value =>
            binder(value).Map(intermediate =>
                projector(value, intermediate)));

    public static Result<IReadOnlyList<T>, TError> Collect<TSource, T, TError>(
        this IEnumerable<TSource> source,
        Func<TSource, Result<T, TError>> selector)
    {
        var accumulated = new List<T>();
        foreach (var item in source)
        {
            var result = selector(item);
            if (result.IsFailure)
                return Result<IReadOnlyList<T>, TError>.Failure(result.Error);
            accumulated.Add(result.Value);
        }
        return Result<IReadOnlyList<T>, TError>.Success(accumulated);
    }

    public static Result<IReadOnlyList<T>, TError> CollectMany<TSource, T, TError>(
        this IEnumerable<TSource> source,
        Func<TSource, Result<IReadOnlyList<T>, TError>> selector)
    {
        var accumulated = new List<T>();
        foreach (var item in source)
        {
            var result = selector(item);
            if (result.IsFailure)
                return Result<IReadOnlyList<T>, TError>.Failure(result.Error);
            accumulated.AddRange(result.Value);
        }
        return Result<IReadOnlyList<T>, TError>.Success(accumulated);
    }
}
