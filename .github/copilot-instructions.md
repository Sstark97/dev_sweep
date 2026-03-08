# DevSweep — Copilot Instructions

## Project Identity

DevSweep is a cross-platform CLI cache cleaner for developers (macOS, Linux, Windows).

- `bash/` — v1.x stable production version (Bash, macOS only)
- `net/` — v2.x in development (.NET 10 AOT, cross-platform)
- All .NET work happens under `net/`

## Tech Stack

- .NET 10 with AOT compilation (`PublishAot=true`)
- C# latest language version, single .csproj: `net/src/DevSweep/DevSweep.csproj`
- Testing: xUnit + FluentAssertions + NSubstitute
- CLI: DotMake.CommandLine (source generators, zero reflection, AOT native)
- UI: Spectre.Console (tables, spinners, colors, AOT compatible)
- Domain layer: ZERO external dependencies

## Architecture

Hexagonal Architecture (single project, folder separation):

```
Domain (pure logic, zero dependencies)
  ↑
Application (ports + use cases)
  ↑
Infrastructure (adapters: CLI, FileSystem, Process, Modules)
```

Dependencies point inward only. Domain has no external packages.

## Coding Standards

MANDATORY: Read `.claude/skills/clean-code.md` before writing any code.

Key rules:
- Railway-Oriented Programming: ALL domain methods return `Result<T, DomainError>`, NO exceptions
- Value objects: `readonly record struct`, private constructor, `Create()` factory
- Entities: `record`, private constructor, explicit factory methods, NO default parameters
- Naming: NO `_` prefix, NO `Get` prefix, NO numbers in variable names
- Testing: `[Class]Should.VerbNounWhenCondition()`, AAEA pattern, Builders
- BANNED: AutoMapper, comments in production code, deep inheritance, `ConfigureAwait(false)`, `out` keyword

Detailed standards in `.claude/skills/`: `rop.md`, `ddd-patterns.md`, `naming.md`, `testing.md`, `anti-patterns.md`.

## Build Commands

All commands run from `net/` directory:

```bash
dotnet build    # Must pass: 0 errors, 0 warnings, 0 AOT warnings
dotnet test     # Must pass: all tests green
dotnet test --filter "FullyQualifiedName~ClassName"  # Specific class
```

## Commit Rules

Format: Conventional Commits (English)
- Types: `feat`, `fix`, `refactor`, `test`, `chore`, `docs`
- Scopes: `domain`, `application`, `infrastructure`, `cli`, `build`
- Pre-commit: `dotnet build` + `dotnet test` must pass

## Agent Workflow

This project uses a linear task pipeline with specialized agents:

1. **backend-planner** — Analyzes tasks, creates plans in `.claude/workspace/planning/`. NEVER writes code.
2. **backend-developer** — Reads plans from `.claude/workspace/progress/`, implements, runs build+tests. NEVER makes architectural decisions.
3. **code-reviewer** — Reviews via `git diff`, writes REVIEW with PASS/FAIL verdict. NEVER modifies code.
4. **test-writer** — Generates tests following conventions. NEVER writes production code.

Pipeline flow: `planning/ → progress/ → review/ → completed/`

See `AGENTS.md` for full details.

## Language Convention

- User communication: Spanish
- Code, commits, technical docs: English

## Resources

- `CLAUDE.md` — Full project rules and conventions
- `AGENTS.md` — Agent registry, project structure, quality standards
- `PROGRESS.md` — Current roadmap and phase status
- `.claude/skills/` — Detailed coding standards per topic
