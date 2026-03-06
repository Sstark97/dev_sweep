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
