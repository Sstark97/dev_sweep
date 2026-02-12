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
}
