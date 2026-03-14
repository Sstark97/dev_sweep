---
name: anti-patterns
description: >
  Banned patterns in DevSweep: no AutoMapper, no comments in production code,
  no deep inheritance hierarchies.
---

# Anti-Patterns (BANNED)

## No Reflection-Based Mapping
- BANNED: AutoMapper, Mapster, ExpressMapper
- USE: Explicit extension methods

```csharp
// WRONG
var dto = _mapper.Map<UserDto>(entity);

// CORRECT
public static class UserMappings
{
    public static UserDto ToDto(this UserEntity entity) => new(
        Id: entity.Id.ToString(),
        Name: entity.FullName,
        Email: entity.EmailAddress);
}

var dto = entity.ToDto();
```

## No Comments in Production Code
- NO comments — code must be self-documenting
- YES semantic names that make intent obvious

## No Deep Inheritance
- NO abstract base classes in application code
- NO inheritance hierarchies
- YES composition and interfaces
- YES records for modeling variants

## No ConfigureAwait(false)

DevSweep is a pure CLI/console AOT app. There is no synchronization context (`SynchronizationContext.Current` is always `null`). `ConfigureAwait(false)` has zero effect and adds visual noise.

- BANNED: `.ConfigureAwait(false)` on any await expression
- BANNED: `.ConfigureAwait(true)` (also pointless)

```csharp
// WRONG
var result = await module.AnalyzeAsync(cancellationToken).ConfigureAwait(false);
var confirmed = await userInteraction.ConfirmAsync(message, true, ct).ConfigureAwait(false);

// CORRECT
var result = await module.AnalyzeAsync(cancellationToken);
var confirmed = await userInteraction.ConfirmAsync(message, true, ct);
```

## No `out` Keyword or `out` Variables

- BANNED: `out` parameters and `out var` declarations in project code
- USE: return values with `Result<T, DomainError>`, tuples, or small dedicated value objects

```csharp
// WRONG
return Version.TryParse(versionText, out var version)
  ? version
  : new Version(0, 0);

// CORRECT
public readonly record struct JetBrainsVersion
{
    private readonly Version value;

    private JetBrainsVersion(Version value) => this.value = value;

    public static Result<JetBrainsVersion, DomainError> Create(string rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
            return Result<JetBrainsVersion, DomainError>.Failure(
                DomainError.Validation(nameof(rawVersion), "Version is required"));

        var parts = rawVersion.Split('.', StringSplitOptions.TrimEntries);

        if (parts.Length > 4 || parts.Any(part => string.IsNullOrEmpty(part) || part.Any(ch => !char.IsDigit(ch))))
            return Result<JetBrainsVersion, DomainError>.Failure(
                DomainError.Validation(nameof(rawVersion), "Version format is invalid"));

        var numbers = parts.Select(int.Parse).ToArray();

        var version = numbers.Length switch
        {
            1 => new Version(numbers[0], 0),
            2 => new Version(numbers[0], numbers[1]),
            3 => new Version(numbers[0], numbers[1], numbers[2]),
            4 => new Version(numbers[0], numbers[1], numbers[2], numbers[3]),
            _ => new Version(0, 0)
        };

        return Result<JetBrainsVersion, DomainError>.Success(new JetBrainsVersion(version));
    }

    public Version AsVersion() => value;
}

private static Version ExtractVersion(string directoryName, string product)
{
    var rawVersion = directoryName.Length > product.Length
        ? directoryName[product.Length..]
        : string.Empty;

    var parsedVersion = JetBrainsVersion.Create(rawVersion);

    return parsedVersion.IsSuccess
        ? parsedVersion.Value.AsVersion()
        : new Version(0, 0);
}
```

## No `default` or `default!` Expressions

Using `default` on reference types produces `null`, and `default!` suppresses the null warning -- both hide potential null reference bugs and defeat the purpose of nullable reference types.

- BANNED: `default!` to satisfy non-null return types
- BANNED: `default` as a placeholder for missing values
- USE: Proper fallback values, empty collections `[]`, or restructure with `SelectMany` / `Option<T>`

```csharp
// WRONG — default! hides null behind a non-null type
List<CleanableItem> items = [.. from item in options
    where item.IsSome
    select item.Match(x => x, () => default!)];

// CORRECT — SelectMany flattens Option into 0 or 1 elements, no null needed
List<CleanableItem> items = [..
    options.SelectMany(opt => opt.Match<IEnumerable<CleanableItem>>(item => [item], () => []))];
```
