---
name: testing
description: >
  Testing conventions for DevSweep: naming pattern [Class]Should.VerbNoun(), Arrange-Act-Extract-Assert,
  Builders for domain objects, no abstract base classes, no OS-specific paths.
---

# Testing Conventions (MANDATORY)

## Organization
- ONE test file per class: `CleanupResultShould.cs`
- NO multiple files: `CleanupResultCreationShould.cs`, `CleanupResultBehaviorShould.cs`
- Test ONLY business behavior
- NO testing enums (they're just constants)

## Naming Convention

**Pattern**: `[WhatIsBeingTested]Should.VerbNounWhenCondition()`

```csharp
// CORRECT — reads as natural sentence
public class FilePathShould
{
    [Fact]
    public void FailWhenPathIsEmpty() { }
    // "FilePath Should Fail When Path Is Empty"

    [Fact]
    public void SucceedWhenPathIsValid() { }
}

// WRONG
public class FilePathTests          // WRONG: no "Should" suffix
{
    [Fact]
    public void ReturnsErrorWhenEmpty() { }  // WRONG: "Returns" is technical

    [Fact]
    public void Constructor_ShouldValidate() { }  // WRONG: underscores
}
```

## Ubiquitous Language in Test Names

Test names must use the language of the domain, not the language of the implementation.

### Rules
- NO `Return` prefix — it is a technical/implementation verb, not a business behavior description
- NO boolean words in test names: `true`, `false`
- NO data type names (`List`, `String`, `Int`, `Bool`, etc.)
- YES verbs that describe observable business behavior: `Detect`, `Know`, `Find`, `Treat`, `Calculate`, `Fail`, `Delete`, `Succeed`
- YES natural language that reads like a business requirement

```csharp
// WRONG — technical vocabulary, boolean words, data type names
public void ReturnTrueWhenDirectoryExists() { }
public void ReturnFalseWhenDirectoryDoesNotExist() { }
public void ReturnEmptyListWhenNoFilesMatch() { }

// CORRECT — natural, ubiquitous language
public void DetectExistingDirectory() { }
public void NotDetectMissingDirectory() { }
public void FindNoFilesWhenNoneMatchPattern() { }
```

---

# Arrange-Act-Extract-Assert (MANDATORY)

Every test follows a strict 4-phase structure. ALL assertions go at the end.

1. **Arrange** — set up inputs, use `.Value` directly (no guard assertions)
2. **Act** — call the method under test
3. **Extract** — pull `.Value` and any derived data from the result
4. **Assert** — ALL assertions grouped together, no blank lines between them

```csharp
// CORRECT — full AAEA pattern
[Fact]
public void ProvideErrorMessagesWhenErrorsExist()
{
    var zeroSize = FileSize.Create(0).Value;
    var expectedErrors = new List<string> { "Permission denied", "File locked" };

    var result = CleanupResult.CreateWithErrors(0, zeroSize, expectedErrors);

    var cleanupResult = result.Value;
    var actualErrors = cleanupResult.ErrorMessages();

    result.IsSuccess.Should().BeTrue();
    actualErrors.Count.Should().Be(2);
    actualErrors.Should().Contain("Permission denied");
    actualErrors.Should().Contain("File locked");
}

// WRONG — guard assertions and interleaved assertions
[Fact]
public void ProvideErrorMessagesWhenErrorsExist()
{
    var sizeResult = FileSize.Create(0);
    sizeResult.IsSuccess.Should().BeTrue();  // WRONG: guard assertion in Arrange

    var result = CleanupResult.CreateWithErrors(0, sizeResult.Value, errors);
    result.IsSuccess.Should().BeTrue();  // WRONG: assertion before extraction

    var cleanupResult = result.Value;
    cleanupResult.HasErrors().Should().BeTrue();
}
```

## Rules
- If `.Value` access on setup fails → test throws `InvalidOperationException`, that's sufficient signal
- NO `result.IsSuccess.Should().BeTrue()` BEFORE `var x = result.Value` — extract first
- NO blank lines between consecutive assertions
- Phase comments (`// Arrange`, `// Act`) are optional if structure is self-evident

## Test Types

```csharp
// Success test — just verify IsSuccess
[Fact]
public void SucceedWithValidParameters()
{
    var size = FileSize.Create(1024).Value;

    var result = CleanupResult.Create(10, size);

    result.IsSuccess.Should().BeTrue();
}

// Failure test — verify IsFailure + error details
[Fact]
public void FailWhenFilesDeletedIsNegative()
{
    var zeroSize = FileSize.Create(0).Value;

    var result = CleanupResult.Create(-1, zeroSize);

    result.IsFailure.Should().BeTrue();
    result.Error.MessageContains("negative").Should().BeTrue();
}

// Behavior test — full AAEA
[Fact]
public void CombineTwoResults()
{
    var smallSize = FileSize.Create(1024).Value;
    var largeSize = FileSize.Create(2048).Value;
    var firstResult = CleanupResult.Create(3, smallSize).Value;
    var secondResult = CleanupResult.Create(5, largeSize).Value;

    var combined = firstResult.Combine(secondResult);

    combined.TotalFilesDeleted().Should().Be(8);
}
```

---

# Test Design Patterns

## Constructor Fields — Mocks Only

xUnit creates a new class instance per test. Use `private readonly` fields for shared mock dependencies only. Domain objects use Builders inline.

```csharp
// CORRECT — constructor fields for mock interfaces
public class CleanupContextShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IProcessRunner processRunner = Substitute.For<IProcessRunner>();
}

// WRONG — constructor fields for domain value objects
public class CleanableItemShould
{
    private readonly FilePath path = FilePath.Create("/any/test/path").Value;  // WRONG
    private readonly FileSize size = FileSize.Create(1024).Value;              // WRONG
}
```

## Builders for Domain Objects

Use Builder classes in `Builders/` for complex domain object construction. Builders hide construction details; tests express intent.

```csharp
// CORRECT
var safeItem = new CleanableItemBuilder()
    .ForModule(CleanupModuleName.Docker)
    .Safe()
    .WithReason("Old cache")
    .Build();

var largeUnsafeItem = new CleanableItemBuilder()
    .ForModule(CleanupModuleName.Homebrew)
    .Unsafe()
    .Large()
    .Build();

// Derive expected values from built items — no magic numbers
var smallItem = new CleanableItemBuilder().Small().Build();
var largeItem = new CleanableItemBuilder().Large().Build();
analysis.TotalSize().Should().Be(smallItem.Size.Add(largeItem.Size));

// WRONG — magic number comparison
analysis.TotalSize().Should().Be(FileSize.Create(3072).Value);  // WRONG: where does 3072 come from?

// WRONG — direct factory call leaks construction details into test
var item = CleanableItem.CreateSafe(
    FilePath.Create("/any/test/path").Value,
    FileSize.Create(1024).Value,
    CleanupModuleName.Docker,
    "Old cache");  // WRONG: test knows too much about construction
```

## No Abstract Base Test Classes

```csharp
// WRONG
public abstract class TestBase
{
    protected FilePath Path = FilePath.Create("/test").Value;
}
public class CleanableItemShould : TestBase { }  // WRONG: inheritance for setup

// CORRECT
public class CleanableItemShould
{
    [Fact]
    public void CreateSafeItemWithReason()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();
        // ...
    }
}
```

## No OS-Specific Paths

Use `Path.Combine()` for test paths instead of hardcoded separators. Use generic segments like `"any"`, `"home"`, `"test"`.

```csharp
// CORRECT — Cross-platform via Path.Combine with generic segments
private static readonly FilePath DockerConfig = FilePath.Create(Path.Combine("any", ".docker")).Value;
private static readonly FilePath Home = FilePath.Create(Path.Combine("any", "home")).Value;

// CORRECT — Generic paths in test assertions
var path = FilePath.Create("/any/test/path").Value;
var report = FilePath.Create("/documents/report.pdf").Value;

// WRONG
FilePath.Create("/var/lib/docker/cache")   // WRONG: Linux-specific
FilePath.Create("/Users/test/file.txt")    // WRONG: macOS-specific
FilePath.Create("C:\\Users\\test\\file")   // WRONG: Windows-specific
```

## Given Helpers — Readable Arrange Phase

For infrastructure/adapter tests with many mock setups, extract `private` helper methods prefixed with `Given` to make the Arrange phase read like a story. These helpers compose: higher-level `Given*` can call lower-level ones.

```csharp
// CORRECT — Named Given helpers make test intent clear
[Test]
public async Task FindNoItemsWhenDockerDaemonNotRunning()
{
    GivenDockerCliAvailable();
    GivenDockerDaemonNotRunning();

    var result = await strategy.AnalyzeAsync(CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.Should().BeEmpty();
}

// CORRECT — Given helpers as private methods
private void GivenDockerCliAvailable() =>
    commandRunner.IsCommandAvailable("docker").Returns(true);

private void GivenDockerDaemonNotRunning() =>
    commandRunner.RunAsync("docker", "info", Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(CommandOutput.Create(1, string.Empty, "Cannot connect")));

// CORRECT — Composable Given helpers (high-level calls low-level)
private void GivenDockerAvailable()
{
    GivenDockerCliAvailable();
    commandRunner.RunAsync("docker", "info", Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(CommandOutput.Create(0, "docker info output", string.Empty)));
}

// CORRECT — Parameterized Given helpers for varying data
private void GivenStoppedContainerCount(int count)
{
    var output = string.Join("\n", Enumerable.Repeat("abc123", count));
    commandRunner.RunAsync("docker", "ps -aq --filter status=exited", Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(CommandOutput.Create(0, output, string.Empty)));
}

// CORRECT — Group "nothing" setup into a composite Given
private void GivenNoResources()
{
    GivenStoppedContainerCount(0);
    GivenNoDanglingImages();
    GivenNoVolumes();
    GivenNoCustomNetworks();
}

// CORRECT — Factory Given for building test items
private static CleanableItem GivenOrbStackItem() =>
    new CleanableItemBuilder()
        .WithPath(Path.Combine("any", "home", ".orbstack", "cache"))
        .ForModule(CleanupModuleName.Docker)
        .Unsafe()
        .WithReason("docker:orbstack-cache")
        .Large()
        .Build();
```

## Shared Test Constants as Static Readonly Fields

When multiple tests share the same derived values (paths, configs), use `private static readonly` fields. These are computed once and shared safely (value types are immutable).

```csharp
// CORRECT — Static readonly for shared test paths
private static readonly FilePath DockerConfig = FilePath.Create(Path.Combine("any", ".docker")).Value;
private static readonly FilePath Home = FilePath.Create(Path.Combine("any", "home")).Value;
private static readonly FilePath CacheDir = FilePath.Create(Path.Combine("any", "home", ".orbstack", "cache")).Value;

// WRONG — Instance fields for value objects (see Constructor Fields rule)
private readonly FilePath path = FilePath.Create("/any/test/path").Value;  // WRONG: instance field

// WRONG — Duplicating construction in every test
var home = FilePath.Create(Path.Combine("any", "home")).Value;  // WRONG: repeated in 10 tests
```

---

# Checklist — Test Code

- [ ] One test file per class
- [ ] Names follow `[Class]Should.VerbNounWhenCondition()`
- [ ] No numbers in variable names
- [ ] `.Value` extracted ONCE, in Extract phase
- [ ] All assertions at the end (AAEA), no blank lines between them
- [ ] No guard assertions in Arrange phase
- [ ] Success tests: just verify `IsSuccess`
- [ ] Failure tests: verify `IsFailure` + error details
- [ ] Semantic variable names (`smallSize`, `safeItem`)
- [ ] No testing of enums
- [ ] Constructor fields for mock interfaces only (not domain objects)
- [ ] No OS-specific paths — use `Path.Combine("any", ...)` with generic segments
- [ ] No magic numbers for sizes — use `.Small()` / `.Large()` on Builders
- [ ] No abstract base test classes
- [ ] Builders for complex domain object creation
- [ ] No `Return` prefix in test names (use behavior verbs instead)
- [ ] No boolean words (`true`, `false`) in test names
- [ ] No data type names in test names (`List`, `String`, etc.)
- [ ] Names describe observable behavior, not implementation return values
- [ ] Given helpers for readable Arrange phase in adapter/infrastructure tests
- [ ] Shared paths as `private static readonly` fields, not instance fields
