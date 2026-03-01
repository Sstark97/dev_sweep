# DevSweep - Project Rules

@.claude/skills/clean-code.md
@.claude/skills/rop.md
@.claude/skills/ddd-patterns.md
@.claude/skills/naming.md
@.claude/skills/testing.md
@.claude/skills/anti-patterns.md

## Project Identity

DevSweep is a cross-platform CLI cache cleaner for developers (macOS, Linux, Windows).

**Repository Structure:**
- `bash/` - v1.x stable production version (Bash, macOS only)
- `net/` - v2.x in development (.NET 10 AOT, cross-platform)
- All .NET work happens under `net/`

## Tech Stack

**Runtime & Framework:**
- .NET 10 with AOT compilation (`PublishAot=true`)
- C# latest language version
- Single .csproj: `net/src/DevSweep/DevSweep.csproj`

**Testing:**
- xUnit for test framework
- FluentAssertions for readable assertions
- NSubstitute for mocking

**CLI & UI:**
- DotMake.CommandLine (source generators, zero reflection, AOT native)
- Spectre.Console (tables, spinners, colors, AOT compatible)

**Dependencies:**
- Domain layer: ZERO external dependencies
- Application/Infrastructure: minimal, AOT-compatible only

## Architecture

**Hexagonal Architecture** (single project, folder separation):

```
Domain (pure logic, zero dependencies)
    ↑
Application (ports + use cases)
    ↑
Infrastructure (adapters: CLI, FileSystem, Process, Modules)
```

**Key Principles:**
- **Domain-Driven Design (DDD)**: Value objects, entities, domain services
- **Railway-Oriented Programming (ROP)**: ALL domain operations return `Result<TValue, TError>`
- **Dependency Rule**: Dependencies point inward only (Infrastructure → Application → Domain)
- **NO exceptions for business errors** in domain layer
- **NO throw statements** in domain logic
- **LINQ query syntax** for Result composition

## Coding Standards

**MANDATORY Reference:** `.claude/skills/clean-code.md`

This file contains ALL coding standards and MUST be followed strictly. Key highlights:

**Value Objects:**
- Type: `readonly record struct`
- Private constructor
- Factory method: `static Result<T, DomainError> Create(...)`
- NO public properties exposing state
- Methods for behavior, NOT properties

**Entities:**
- Type: `record` (reference semantics)
- Private constructor
- Explicit factory methods (NO default parameters)
- Methods return `Result<T, DomainError>`

**Naming:**
- NO underscore prefix in private fields (`bytes`, not `_bytes`)
- NO Get prefix in methods (`InMegabytes()`, not `GetMegabytes()`)
- NO numbers in variable names (`smallSize`, not `size1`)
- Semantic, natural language names everywhere

**Testing:**
- Test naming: `[Class]Should.VerbNounWhenCondition()`
- One test file per class
- Extract `.Value` ONCE when testing behavior
- Success tests: just verify `IsSuccess`
- Failure tests: verify `IsFailure` + error details

**Anti-Patterns (BANNED):**
- AutoMapper or any reflection-based mapping
- Comments in production code (self-documenting code only)
- Deep inheritance hierarchies
- Default parameters (use explicit factory methods)

## Build Commands

All commands run from `net/` directory:

```bash
# Build (must pass with 0 errors, 0 warnings, 0 AOT warnings)
dotnet build

# Run all tests (must pass 100%)
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ClassName"
```

## Commit Rules

**Format:** Conventional Commits (English)

```
<type>(<scope>): <description>

[optional body]
```

**Types:** `feat`, `fix`, `refactor`, `test`, `chore`, `docs`

**Scopes:** `domain`, `application`, `infrastructure`, `cli`, `build`

**Examples:**
```
feat(domain): add Description value object
fix(application): correct validation logic in CleanupUseCase
refactor(domain): simplify FileSize comparison operators
test(domain): add edge cases for FilePath validation
chore(build): update .NET SDK to 10.0.1
```

**Pre-commit Requirements:**
- `dotnet build` passes (0 errors, 0 warnings)
- `dotnet test` passes (all tests green)
- Code follows `.claude/skills/clean-code.md` standards

## Language Convention

- **User communication:** Spanish (messages, documentation for users)
- **Code:** English (variables, methods, classes, comments if any)
- **Commits:** English
- **Technical documentation:** English

## Current Phase

**Phase 1: Foundation** ✅ COMPLETE
- Domain value objects implemented (FileSize, FilePath, CleanupResult)
- Domain entities implemented (CleanableItem, CleanupSummary)
- Result<T, DomainError> type with LINQ support
- Domain enums (CleanupModuleName, InteractionStrategy, OutputStrategy)

**Phase 2: Application Layer** 🚧 IN PROGRESS
- Port interfaces and module contracts
- Use cases implementation

See `PROGRESS.md` for complete roadmap and dependency graph.

## Agent Workflow

When implementing features:
1. Read this file (`CLAUDE.md`) for project context
2. Read `AGENTS.md` for agent roles and project structure
3. Follow `.claude/skills/clean-code.md` for coding standards
4. Use `.claude/workspace/` for task coordination
5. Always verify build and tests pass before completion
