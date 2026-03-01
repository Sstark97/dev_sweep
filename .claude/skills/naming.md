---
name: naming
description: >
  Naming conventions for DevSweep: no underscore prefix, no Get prefix, no numbers in names.
  Tell Don't Ask — expose behavior, not state. Applies to production and test code.
---

# Naming Conventions (MANDATORY)

Natural language and semantic names everywhere. No technical artifacts in names.

## Rules — Fields & Methods
- NO `_` prefix in private fields: `_value`, `_bytes` → use `value`, `bytes`
- NO `Get` prefix in methods: `GetValue()`, `GetBytes()` → use `Value()`, `InBytes()`
- YES descriptive field names: `value`, `bytes`, `items`
- YES natural method names: `Add()`, `CompareTo()`, `InKilobytes()`

## Rules — Variables (Production & Tests)
- NO numbers in variable names: `file1`, `size2`, `item1`
- NO abstract suffixes: `fileA`, `fileB`, `itemX`
- YES semantic names that express intent: `smallSize`, `safeItem`, `dockerItem`

## Examples

```csharp
// WRONG - Numbers and abstract suffixes
var file1 = FilePath.Create("/path/file1.txt");
var file2 = FilePath.Create("/path/file2.txt");
var size1 = FileSize.Create(1024);
var itemA = new CleanableItem(...);
var itemB = new CleanableItem(...);

// CORRECT - Semantic names
var configPath = FilePath.Create("/etc/config.json");
var dataPath = FilePath.Create("/var/data.db");
var smallSize = FileSize.Create(1024);
var safeItem = CleanableItem.CreateSafe(...);
var unsafeItem = CleanableItem.CreateUnsafe(...);
var dockerItem = CleanableItem.CreateSafe(..., CleanupModuleName.Docker, ...);
var homebrewItem = CleanableItem.CreateSafe(..., CleanupModuleName.Homebrew, ...);
```

---

# Tell, Don't Ask

Objects expose **behavior**, not internal state. Ask them to do things, don't query their internals.

## Rules
- NO public properties that expose raw internal state
- YES methods that compute and return meaningful values
- YES operators for natural comparisons

## Examples

```csharp
// WRONG - Exposes raw state
public readonly record struct FileSize
{
    public long Bytes { get; }       // WRONG: exposes internal state as property
    private long _bytes;             // WRONG: underscore prefix
    public long GetBytes() { ... }  // WRONG: "Get" prefix
}

// CORRECT - Exposes behavior
public readonly record struct FileSize
{
    private readonly long bytes;          // CORRECT: private, no underscore

    public decimal InMegabytes() => ...  // CORRECT: computed behavior
    public decimal InKilobytes() => ...
    public FileSize Add(FileSize other) => ...

    public static bool operator >(FileSize left, FileSize right) =>
        left.bytes > right.bytes;
}
```
