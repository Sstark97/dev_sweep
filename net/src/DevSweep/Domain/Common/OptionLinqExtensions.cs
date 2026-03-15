namespace DevSweep.Domain.Common;

public static class OptionLinqExtensions
{
    public static Option<TOut> Select<TValue, TOut>(
        this Option<TValue> option,
        Func<TValue, TOut> selector) =>
        option.Map(selector);

    public static Option<TOut> SelectMany<TValue, TIntermediate, TOut>(
        this Option<TValue> option,
        Func<TValue, Option<TIntermediate>> binder,
        Func<TValue, TIntermediate, TOut> projector) =>
        option.Bind(value =>
            binder(value).Map(intermediate =>
                projector(value, intermediate)));

    public static Option<T> Where<T>(
        this Option<T> option,
        Func<T, bool> predicate) =>
        option.Filter(predicate);

    public static Option<T> ToOption<T, TError>(this Result<T, TError> result) =>
        result.IsSuccess ? Option<T>.Some(result.Value) : Option<T>.None;

    public static IEnumerable<T> ToEnumerable<T>(this Option<T> option) =>
        option.Match<IEnumerable<T>>(value => [value], () => []);
}
