---
name: rop
description: >
  Railway-Oriented Programming for DevSweep. Result<T, DomainError> patterns,
  no exceptions in domain logic, LINQ composition for error propagation.
---

# Railway-Oriented Programming (MANDATORY)

**ALL domain operations MUST return `Result<TValue, TError>`**

## Rules
- NO `throw` statements for business errors
- NO try/catch for domain logic
- YES `Result<T, DomainError>` for ALL domain methods
- YES LINQ query syntax for composition
- YES Pattern matching with `Match()` for final output

## Examples

```csharp
// CORRECT - Railway-Oriented
public static Result<FilePath, DomainError> Create(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        return Result<FilePath, DomainError>.Failure(
            DomainError.Validation("File path cannot be empty"));

    return Result<FilePath, DomainError>.Success(new FilePath(path));
}

// CORRECT - LINQ Composition
var result =
    from filePath in FilePath.Create(inputPath)
    from size in FileSize.Create(bytes)
    from item in CreateCleanableItem(filePath, size)
    select item;

// WRONG - Throwing exceptions
public static FilePath Create(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("Path cannot be empty");  // WRONG

    return new FilePath(path);
}
```

## LINQ Query Syntax for Collections

When filtering and mapping collections, prefer LINQ query syntax over fluent method chains (`.Where().Select()`). This is consistent with using query syntax for `Result` composition.

```csharp
// CORRECT - LINQ query syntax for collection pipelines
return [.. from module in registry.Modules()
    where module.IsAvailableOnPlatform(currentOs)
    let descriptor = ModuleDescriptor.Create(module.Name, module.Description, module.IsDestructive)
    where descriptor.IsSuccess
    select descriptor.Value];

// WRONG - Fluent method chains
return registry.Modules()
    .Where(m => m.IsAvailableOnPlatform(currentOs))
    .Select(m => ModuleDescriptor.Create(m.Name, m.Description, m.IsDestructive))
    .Where(r => r.IsSuccess)
    .Select(r => r.Value)
    .ToList();
```

## Recover — Fallback on Failure

Use `Recover()` to turn a failed `Result` into a success with a default value. This is the ROP equivalent of a try/catch fallback:

```csharp
// CORRECT — Recover with simple fallback value
var safeOutput = dangerousOperation().Recover(defaultValue);

// CORRECT — Recover computing fallback from the error
var withMessage = dangerousOperation().Recover(error => $"recovered: {error}");

// CORRECT — Use in pipelines where failure is acceptable
private static long EstimateFromOutput(
    Result<CommandOutput, DomainError> result,
    Func<string, int> counter,
    long megabytesPerItem) =>
    result.ToOption()
        .Filter(output => output.IsSuccessful())
        .Map(output => counter(output.StandardOutput()) * megabytesPerItem * BytesPerMegabyte)
        .ValueOr(0L);
```

## Async Railway Extensions

For async pipelines, use `BindAsync`, `MapAsync`, and `TapAsync` to chain `Task<Result<T, E>>` without nested `await` + `if` blocks:

```csharp
// CORRECT — async bind chain (flat, readable pipeline)
var finalResult = await initialResult
    .BindAsync(value => SomeAsyncOperation(value, cancellationToken))
    .MapAsync(transformed => transformed.Property);

// CORRECT — TapAsync for side effects that can fail
var tapped = await result.TapAsync(async value =>
{
    await logger.LogAsync(value, cancellationToken);
    return Result<Unit, DomainError>.Success(Unit.Value);
});

// WRONG — manual unwrap + rebind
var intermediate = await someTask;
if (intermediate.IsFailure)
    return Result<T, DomainError>.Failure(intermediate.Error);
var next = await AnotherAsync(intermediate.Value);
```

## Option<T> — Missing Values Without Errors

Use `Option<T>` when absence is normal and NOT an error. Use `Result<T, E>` when absence IS an error.

```csharp
// CORRECT — Option for "might not exist" (no error to report)
private Option<CleanableItem> BuildCacheItem()
{
    if (!OrbStackPresent())
        return Option<CleanableItem>.None;

    return
        from path in FilePath.Create(cachePath).ToOption()
        where fileSystem.DirectoryExists(path) && fileSystem.IsDirectoryNotEmpty(path)
        from size in fileSystem.Size(path).ToOption()
        select CleanableItem.CreateUnsafe(path, size, moduleName, reason);
}

// CORRECT — Bridge Result to Option when error details are irrelevant
var option = result.ToOption();  // Success → Some, Failure → None

// CORRECT — Option with LINQ query syntax (Select, SelectMany, Where)
var computed =
    from v in option
    where v > threshold
    select v * 2;

// CORRECT — Extract with fallback
var value = option.ValueOr(defaultValue);

// CORRECT — Option.Filter for conditional presence checks
var present = FilePath.Create(somePath)
    .ToOption()
    .Filter(fileSystem.DirectoryExists)
    .IsSome;

// WRONG — Using Result when absence is expected
Result<Config, DomainError> TryFindConfig()  // WRONG: failure implies error
Option<Config> FindConfig()                    // CORRECT: absence is normal
```

## Collect — Aggregating Multiple Results

Use `Collect()` to turn a collection of `Result<T, E>` into a single `Result<IReadOnlyList<T>, E>` (fail-fast):

```csharp
// CORRECT — Aggregate strategy results
return
    from results in cleanResults.ToList().Collect(r => r)
    select results.Aggregate(CleanupResult.Empty, (acc, r) => acc.Combine(r));

// CORRECT — Validate + collect in one step
var candidates = items.Collect(item => ValidateItem(item));
```
