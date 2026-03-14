---
name: clean-code
description: >
  DevSweep coding standards index. Covers ROP, DDD patterns, naming, testing, and anti-patterns.
  Activate when writing or reviewing any C# code in DevSweep.
---

# Clean Code — DevSweep Standards

Index of all coding standards. Each sub-skill covers a specific area:

- **rop.md** — Result<T, DomainError>, no throw, LINQ composition
- **ddd-patterns.md** — Value objects, entities, factory methods
- **naming.md** — No underscore, no Get prefix, no numbers; Tell Don't Ask
- **testing.md** — Naming, AAEA pattern, Builders, no abstract base classes
- **anti-patterns.md** — No AutoMapper, no comments, no inheritance

---

## Checklist Before Committing

### Domain Code
- [ ] All methods return `Result<T, DomainError>` (no exceptions)
- [ ] No `throw` statements for business errors
- [ ] All value objects are `readonly record struct`
- [ ] All entities are `record`
- [ ] No default parameters (explicit factory methods)
- [ ] No `_` prefix in private fields
- [ ] No `Get` prefix in method names
- [ ] No numbers in variable names
- [ ] No comments in code
- [ ] LINQ query syntax used for Result composition
- [ ] `Option<T>` for missing values that are NOT errors; `Result<T, E>` for errors
- [ ] `Recover()` for fallback on expected failures
- [ ] Convenience properties (`Zero`, `Empty`) for well-known instances
- [ ] Strategy pattern for modules with multiple backends

### Test Code
- [ ] One test file per class
- [ ] Names follow `[Class]Should.VerbNounWhenCondition()`
- [ ] No numbers in variable names
- [ ] `.Value` extracted ONCE in Extract phase
- [ ] All assertions at end, no blank lines between them
- [ ] No guard assertions in Arrange
- [ ] Success tests: just verify `IsSuccess`
- [ ] Failure tests: verify `IsFailure` + error details
- [ ] Constructor fields for mock interfaces only (not domain objects)
- [ ] No OS-specific paths — use `Path.Combine("any", ...)` with generic segments
- [ ] No magic numbers for sizes — use `.Small()` / `.Large()` on Builders
- [ ] No abstract base test classes
- [ ] Given helpers for readable Arrange phase in adapter tests
- [ ] Shared paths as `private static readonly` fields

### Build
- [ ] `dotnet build` — 0 errors, 0 warnings
- [ ] `dotnet test` — all tests pass
- [ ] No AOT warnings

---

## Quick Reference

| Aspect | Do | Don't |
|--------|-----|-------|
| **Error Handling** | `Result<T, DomainError>` | `throw` exceptions |
| **Value Objects** | `readonly record struct` | `class` |
| **Entities** | `record` | `class` |
| **Fields** | `private readonly long bytes;` | `private long _bytes;` |
| **Methods** | `InMegabytes()` | `GetMegabytes()` |
| **Variables** | `smallSize`, `safeItem` | `size1`, `itemA` |
| **Factories** | `Create()`, `CreateWithErrors()`, `FromMegabytes()` | `Create(errors = null)` |
| **Convenience** | `FileSize.Zero`, `CleanupResult.Empty` | Public parameterless constructors |
| **Missing values** | `Option<T>` when absence is normal | `Result<T,E>` for expected absence |
| **Tests** | `.Value` extracted once | `.Value.Prop.Method()` |
| **Test Names** | `FailWhenPathIsEmpty()` | `Create_Should_Return_Error()` |
| **Test Structure** | Arrange-Act-Extract-Assert | Guard assertions, interleaved assertions |
| **Test Setup** | Constructor fields (mocks) + Builders (domain) | Abstract base classes |
| **Test Arrange** | `Given*` helpers for mock setup | Inline mock setup repeated per test |
| **Test Paths** | `Path.Combine("any", ...)` | `/var/lib/docker`, `/Users/` |
| **Test Sizes** | `.Small()`, `.Large()` on builders | bare `1024` |
| **Mapping** | Extension methods | AutoMapper |
