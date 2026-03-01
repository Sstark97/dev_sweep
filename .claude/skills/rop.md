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
