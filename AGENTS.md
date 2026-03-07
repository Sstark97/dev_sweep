# DevSweep - Agent Registry

## Available Agents

| Agent | Model | Role | Responsibility |
|-------|-------|------|----------------|
| **backend-planner** | opus | Planner | Analyzes tasks, designs architecture, creates implementation plans in `.claude/workspace/planning/`. NEVER writes production code. |
| **backend-developer** | sonnet | Implementer | Reads plans from `.claude/workspace/progress/`, implements features, writes tests, runs builds. NEVER makes architectural decisions outside the plan. |
| **code-reviewer** | opus | Reviewer | Reviews code via git diff, produces review document in `.claude/workspace/review/` with PASS/FAIL/FAIL-TESTS verdict. NEVER modifies code. |
| **test-writer** | sonnet | Test Specialist | Generates xUnit tests for domain types following project conventions. Invoked on FAIL-TESTS verdict or standalone for existing untested classes. NEVER writes production code. |

## Responsibility Boundaries (STRICT)

These boundaries are CRITICAL for the agent system to work correctly:

- **Planners NEVER write production code** - Only create plan files in `.claude/workspace/planning/`
- **Developers NEVER make architectural decisions** - Follow the plan exactly, flag ambiguities but proceed with simplest interpretation
- **Reviewers NEVER modify code** - Only produce review documents with verdict and issues
- **Test writers NEVER write production code** - Only generate test files in `net/tests/DevSweep.Tests/`

Each agent has a single, well-defined responsibility. Mixing responsibilities causes confusion and poor outcomes.

## Project Structure

```
net/
  src/DevSweep/
    Domain/
      Common/
        Result.cs                    # Result<TValue, TError> type
        ResultLinqExtensions.cs      # LINQ support for Result
      Entities/
        CleanableItem.cs             # File to clean with safety flag
        CleanupSummary.cs            # Cleanup operation summary
      Enums/
        CleanupModuleName.cs         # Module types (JetBrains, Docker, etc.)
        InteractionStrategy.cs       # Interactive vs AutoConfirm
        OutputStrategy.cs            # Rich, Plain, JSON
      Errors/
        DomainError.cs               # Domain error representation
      ValueObjects/
        CleanupResult.cs             # Cleanup operation result
        FilePath.cs                  # File path with validation
        FileSize.cs                  # File size with conversions
      Services/                      # Domain services (Phase 2)
    Application/                     # Ports, use cases (Phase 2)
    Infrastructure/                  # Adapters (Phase 3+)
    Program.cs                       # Entry point

  tests/DevSweep.Tests/
    Domain/                          # Mirrors src/Domain structure
      Entities/
      ValueObjects/
      Common/
```

## Development Commands

All commands run from `net/` directory:

```bash
# Build project (must pass with 0 errors, 0 warnings, 0 AOT warnings)
dotnet build

# Run all tests (must pass 100%)
dotnet test

# Run tests for specific class
dotnet test --filter "FullyQualifiedName~FilePathShould"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Clean build artifacts
dotnet clean
```

## Available Dotnet Skills

These are specialized skills (from `dotnet-skills` plugin) that agents can invoke when a task falls within their scope:

| Skill | Used By | Purpose |
|-------|---------|---------|
| `csharp-coding-standards` | developer, reviewer | Modern C# patterns: records, pattern matching, value objects |
| `csharp-type-design-performance` | planner, developer, reviewer | Type design: sealed, readonly struct, static pure, collections |
| `project-structure` | planner | .slnx, Directory.Build.props, central package management |
| `package-management` | developer | NuGet with CPM via dotnet CLI (never edit XML) |
| `serialization` | planner, developer | AOT-compatible serialization (System.Text.Json, source generators) |
| `microsoft-extensions-dependency-injection` | planner, developer | DI composition root with Add* extension methods |
| `slopwatch` | **reviewer (MANDATORY)** | Detects LLM reward hacking: disabled tests, suppressed warnings |
| `crap-analysis` | reviewer | Code coverage + CRAP scores for risk hotspots |

**Rule:** Skills are invoked on-demand, only when the task scope matches. Slopwatch is the exception - it runs on EVERY review.

## Quality Standards

Before marking any task complete:

1. **Build passes:** `dotnet build` with 0 errors, 0 warnings, 0 AOT warnings
2. **Tests pass:** `dotnet test` all green
3. **Code standards:** Follows `.claude/skills/` strictly (rop, ddd-patterns, naming, testing, anti-patterns)
4. **Railway-Oriented:** All domain methods return `Result<T, DomainError>`
5. **No exceptions:** No throw statements in domain layer for business errors
6. **Naming:** No underscores, no Get prefix, no numbers in names
7. **Testing:** Test naming follows `[Class]Should.VerbNounWhenCondition()`

## Task Workflow

Tasks move through the workspace directories in a linear pipeline:

```
planning/  →  progress/  →  review/  →  completed/
   ↑            ↑            ↑            ↑
Planner     Developer    Developer    Reviewer
creates     reads &      signals       approves
plan        implements   done
```

### 1. `planning/` — Plans Created Here

The **backend-planner** creates `PLAN-{task-slug}.md` files here containing: task description, acceptance criteria, files to create/modify, implementation steps, testing requirements, and complexity estimate.

### 2. `progress/` — Active Implementation

The coordinator moves the plan file here when implementation starts. The **backend-developer** reads it, implements step by step, checks off items, runs build and tests, then signals completion.

### 3. `review/` — Code Under Review

The coordinator moves the plan file here when implementation is complete. The **code-reviewer** examines `git diff`, creates `REVIEW-{task-slug}.md` with verdict PASS / FAIL / FAIL-TESTS.

### 4. `completed/` — Finished Tasks

All task files are moved here once the review passes (or after the one allowed retry). This directory is the permanent historical record.

## WIP.md — Coordinator Tracking

The `do-task` command creates `.claude/workspace/WIP.md` to track its own progress through phases (Setup → Analysis → Planning → Implementation → Review). It is deleted when the task completes.

## Workspace Rules

1. Only agents write to `workspace/` — the coordinator only moves files between directories
2. Plans are contracts — developers follow plans exactly; flag ambiguities but proceed with simplest interpretation
3. One retry maximum — if review fails, developer gets ONE chance to fix issues
4. No loops — linear pipeline: planning → implementation → review → done
5. WIP.md is ephemeral — deleted after task completion
6. Plan files are permanent — moved to `completed/` as historical record

## Current Phase

**Phase 1 Complete:** Domain layer with value objects, entities, Result type
**Phase 2 Complete:** Application layer ports and use cases
**Phase 3 Complete:** Infrastructure adapters for file system, processes, cross-platform environment

See `PROGRESS.md` for complete roadmap.
