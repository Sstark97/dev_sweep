namespace DevSweep.Domain.Common;

public readonly record struct Option<T>
{
    private readonly T? value;

    private Option(T value)
    {
        this.value = value;
        IsSome = true;
    }

    public bool IsSome { get; }
    public bool IsNone => !IsSome;

    public static Option<T> Some(T value) => new(value);
    public static readonly Option<T> None = default;

    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
        IsSome ? onSome(value!) : onNone();

    public Option<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSome ? Option<TOut>.Some(mapper(value!)) : Option<TOut>.None;

    public Option<TOut> Bind<TOut>(Func<T, Option<TOut>> binder) =>
        IsSome ? binder(value!) : Option<TOut>.None;

    public Option<T> Filter(Func<T, bool> predicate) =>
        IsSome && predicate(value!) ? this : None;

    public T ValueOr(T fallback) =>
        IsSome ? value! : fallback;
}
