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
