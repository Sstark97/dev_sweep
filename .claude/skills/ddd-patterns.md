---
name: ddd-patterns
description: >
  DDD patterns for DevSweep: value objects as readonly record struct, entities as record,
  and explicit factory methods. No default parameters, no public state exposure.
---

# Value Objects

**ALWAYS `readonly record struct` with Result-returning factory**

## Rules
- Type: `readonly record struct`
- Private constructor
- Static factory: `Result<T, DomainError> Create(...)`
- Methods return computed values (not properties exposing internal state)
- Operators for comparison (`<`, `>`, `<=`, `>=`)
- Private fields without underscore prefix

## Template

```csharp
public readonly record struct MyValueObject
{
    private readonly SomeType value;

    private MyValueObject(SomeType value) => this.value = value;

    public static Result<MyValueObject, DomainError> Create(SomeType input)
    {
        if (/* validation fails */)
            return Result<MyValueObject, DomainError>.Failure(
                DomainError.Validation("Error message"));

        return Result<MyValueObject, DomainError>.Success(new MyValueObject(input));
    }

    public SomeOtherType SomeOperation() => /* compute from value */;

    public static bool operator >(MyValueObject left, MyValueObject right) =>
        left.value > right.value;
}
```

## Convenience Properties and Alternative Factories

Value objects can expose well-known instances as `static` properties and additional `Create*` factory methods with domain-specific units.

### Rules
- `Zero` / `Empty` properties bypass validation for well-known safe values
- Alternative factories use domain units (`FromMegabytes`, `FromSeconds`) and validate in the same style as `Create`
- `Empty` on composite types can delegate to its own `Create()` + `.Value` (safe because zero is always valid)

### Examples

```csharp
// CORRECT — Zero as bypassing factory (always valid)
public static FileSize Zero => new(0);

// CORRECT — Alternative factory with domain-specific units
public static Result<FileSize, DomainError> FromMegabytes(long megabytes)
{
    if (megabytes < 0)
        return Result<FileSize, DomainError>.Failure(
            DomainError.Validation("Megabytes cannot be negative"));

    return Result<FileSize, DomainError>.Success(
        new FileSize(megabytes * (long)(BytesPerKilobyte * BytesPerKilobyte)));
}

// CORRECT — Empty on composite value object delegating to own factory
public static CleanupResult Empty => Create(0, FileSize.Zero).Value;

// WRONG — Exposing constructor publicly for "zero" instances
public static FileSize Zero => new FileSize(0);  // OK only in private context
public FileSize() { bytes = 0; }                  // WRONG: public parameterless ctor
```

---

# Entities

**ALWAYS `record` with explicit factory methods and Result-returning methods**

## Rules
- Type: `record` (reference semantics)
- Private constructor
- Explicit factory methods — NO default parameters
- Methods return `Result<T, DomainError>` for business operations
- Immutability via `with` expressions or constructor calls

## Template

```csharp
public record MyEntity
{
    private MyEntity(/* parameters */)
    {
        // Initialize properties
    }

    public static MyEntity CreateVariant1(/* params */) =>
        new(/* args */);

    public static MyEntity CreateVariant2(/* params */) =>
        new(/* args */);

    public SomeProperty Property { get; }

    public Result<MyEntity, DomainError> SomeBusinessMethod()
    {
        if (/* business rule violated */)
            return Result<MyEntity, DomainError>.Failure(
                DomainError.InvalidOperation("Error message"));

        return Result<MyEntity, DomainError>.Success(new MyEntity(/* new state */));
    }
}
```

---

# Factory Methods — No Default Parameters

**Default parameters hide intent — use explicit factory methods**

## Rules
- NO default parameters: `Create(int x, string? y = null)`
- YES explicit factory methods for each case
- Names reveal intent: `Create`, `CreateWithErrors`, `CreateSafe`, `CreateUnsafe`

## Examples

```csharp
// WRONG - Default parameters
public static Result<CleanupResult, DomainError> Create(
    int filesDeleted,
    FileSize bytesFreed,
    IReadOnlyList<string>? errors = null)  // WRONG: hidden default
{ ... }

// CORRECT - Explicit factory methods
public static Result<CleanupResult, DomainError> Create(
    int filesDeleted,
    FileSize bytesFreed)
{
    return Result<CleanupResult, DomainError>.Success(
        new CleanupResult(filesDeleted, bytesFreed, []));
}

public static Result<CleanupResult, DomainError> CreateWithErrors(
    int filesDeleted,
    FileSize bytesFreed,
    IReadOnlyList<string> errors)
{
    return Result<CleanupResult, DomainError>.Success(
        new CleanupResult(filesDeleted, bytesFreed, errors));
}

// CORRECT - Entity with intent-revealing factories
public static CleanableItem CreateSafe(
    FilePath path, FileSize size, CleanupModuleName moduleType, string reason) =>
    new(path, size, moduleType, isSafe: true, reason);

public static CleanableItem CreateUnsafe(
    FilePath path, FileSize size, CleanupModuleName moduleType, string reason) =>
    new(path, size, moduleType, isSafe: false, reason);
```

---

# Use Cases

**`sealed class` with primary constructor and private helper methods**

## Rules
- Type: `sealed class` (not record, not struct)
- Dependencies injected via C# primary constructor
- Implements a driving port interface (e.g., `IAnalyzeUseCase`)
- Public method is always named `Invoke` (see naming.md)
- Long `Invoke()` methods MUST be broken into private helpers
- Each private helper has one responsibility and a semantic name
- Async helpers return `Task<Result<T, DomainError>>`
- Sync helpers return `Result<T, DomainError>`
- `Invoke()` should read like a high-level story (guard -> process -> output -> return)

## Template

```csharp
public sealed class MyUseCase(
    SomeDependency dependency,
    IOutputFormatter outputFormatter
) : IMyUseCase
{
    public async Task<Result<MyResult, DomainError>> Invoke(
        MyInput input,
        CancellationToken cancellationToken)
    {
        if (input is null)
            return Result<MyResult, DomainError>.Failure(
                DomainError.Validation("input is required"));

        var processedResult = await ProcessAsync(input, cancellationToken);
        if (processedResult.IsFailure)
            return Result<MyResult, DomainError>.Failure(processedResult.Error);

        outputFormatter.DisplayResult(processedResult.Value);

        return Result<MyResult, DomainError>.Success(processedResult.Value);
    }

    private async Task<Result<MyResult, DomainError>> ProcessAsync(
        MyInput input, CancellationToken cancellationToken)
    {
        // Single-responsibility helper logic
    }
}
```

## Private Method Naming Guide

| Responsibility | Name Pattern | Return Type |
|----------------|-------------|-------------|
| Resolve/lookup from registry | `ResolveModules` | `Result<List<T>, DomainError>` |
| Iterate + call async operations | `AnalyzeModulesAsync`, `CleanModulesAsync` | `Task<Result<List<T>, DomainError>>` |
| User confirmation check | `UserConfirmedAsync` | `Task<bool>` |

---

# Strategy Pattern in Infrastructure

When a module has multiple runtime backends (e.g., Docker CLI vs OrbStack), extract a **strategy interface** and inject strategies as `IReadOnlyList<TStrategy>`.

## Rules
- Strategy interface lives next to its implementations (same namespace)
- Module orchestrates strategies: filters by platform, aggregates results
- Each strategy is a `sealed class` with dependencies via primary constructor
- Strategies are filtered by `IsAvailable(OperatingSystemType)` before use
- Module aggregates results from active strategies using `Combine()` or `Collect()`

## Template

```csharp
// CORRECT — Strategy interface
public interface IContainerRuntimeStrategy
{
    string RuntimeName { get; }
    bool IsAvailable(OperatingSystemType operatingSystem);
    Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken);
    Task<Result<CleanupResult, DomainError>> CleanAsync(IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken);
}

// CORRECT — Module orchestrating strategies
public sealed class DockerModule(
    IEnvironmentProvider environment,
    IReadOnlyList<IContainerRuntimeStrategy> strategies) : ICleanupModule
{
    public async Task<Result<ModuleAnalysis, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var analysisResults = await Task.WhenAll(
            ActiveStrategies().Select(s => s.AnalyzeAsync(cancellationToken)));

        List<CleanableItem> allItems = [.. from result in analysisResults
            where result.IsSuccess
            from item in result.Value
            select item];

        return ModuleAnalysis.Create(CleanupModuleName.Docker, allItems);
    }

    private IEnumerable<IContainerRuntimeStrategy> ActiveStrategies() =>
        strategies.Where(s => s.IsAvailable(environment.CurrentOperatingSystem));
}
```

---

# Static Helper Methods on Models

Models (value objects, application models) can expose `static` helper methods that encapsulate common `Result` matching patterns. This keeps match logic co-located with the type.

```csharp
// CORRECT — Static helper encapsulating Result matching on the type itself
public static string ErrorMessage(Result<CommandOutput, DomainError> result) =>
    result.IsFailure ? result.Error.ToString() : result.Value.StandardError();

// Usage in callers is clean and declarative
var errorMsg = CommandOutput.ErrorMessage(commandResult);

// WRONG — Duplicating match logic at every call site
var errorMsg = commandResult.IsFailure
    ? commandResult.Error.ToString()
    : commandResult.Value.StandardError();
```
