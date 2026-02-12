# DevSweep Migration Progress

## Overview

DevSweep is migrating from Bash (v1.x) to .NET 10 AOT (v2.x) to provide cross-platform support for macOS, Linux, and Windows while maintaining clean architecture principles.

## Repository Structure

```
dev_sweep/
├── bash/          # v1.x - Production Bash version (macOS only)
├── net/           # v2.x - .NET 10 AOT version (cross-platform) [WIP]
└── [common files] # README, CONTRIBUTING, LICENSE, etc.
```

## Migration Roadmap

### ✅ Phase 0: Foundation
- [x] **#45** - Repository restructuring for bash and .NET coexistence
- [x] **#46** - Create marketing website with Astro Starlight

### ✅ Phase 1: Foundation (Completed)
- [x] **#25** - Initialize .NET 10 solution with hexagonal folder structure
- [x] **#26** - Implement domain value objects and enums
- [x] **#27** - Implement domain entities and services

### ⏳ Phase 2: Application Layer
*Depends on Phase 1*

- [ ] **#28** - Define port interfaces and module contracts
- [ ] **#29** - Implement use cases

### ⏳ Phase 3: Infrastructure Adapters
*Depends on Phase 2, can run in parallel*

- [ ] **#30** - Implement file system adapter
- [ ] **#31** - Implement process and command adapters
- [ ] **#32** - Implement cross-platform environment provider

### ⏳ Phase 4: Cleanup Modules
*Depends on Phase 3, all run in parallel*

- [ ] **#33** - Implement JetBrains cleanup module
- [ ] **#34** - Implement Docker cleanup module
- [ ] **#35** - Implement Homebrew cleanup module
- [ ] **#36** - Implement DevTools cleanup module
- [ ] **#37** - Implement stale projects cleanup module
- [ ] **#38** - Implement system cleanup module

### ⏳ Phase 5: CLI Presentation
*Depends on Phase 4*

- [ ] **#39** - Implement CLI subcommands
- [ ] **#40** - Implement output formatters and interaction strategies
- [ ] **#41** - Wire DI composition root

### ⏳ Phase 6: E2E and Polish
*Depends on Phase 5, can run in parallel*

- [ ] **#42** - Add E2E smoke tests
- [ ] **#43** - Cross-platform CI pipeline
- [ ] **#44** - Documentation and README update

## Technical Stack

| Package | Purpose | Justification |
|---------|---------|---------------|
| .NET 10 AOT | Runtime + native compilation | LTS, ~75% faster cold starts, self-contained binaries |
| DotMake.CommandLine | CLI parsing | Source generators, zero reflection, AOT native |
| Spectre.Console | Terminal UI | Rich UI (tables, spinners, colors), AOT compatible |
| xUnit | Testing | Industry standard |
| FluentAssertions | Readable assertions | `.Should().BeEmpty()` |
| NSubstitute | Mocking | Clean syntax |

## Architecture

**Hexagonal Architecture** (single .csproj with folder separation):

```
Domain (pure logic, zero dependencies)
    ↑
Application (ports + use cases + ICleanupModule contracts)
    ↑
Infrastructure (adapters: CLI, FileSystem, Process, Modules)
```

### Modules (Strategy Pattern)
- **JetBrains** - IDE caches and config (macOS, Linux, Windows)
- **Docker** - Container images, volumes, build cache (macOS, Linux, Windows)
- **Homebrew** - Package cache and diagnostics (macOS, Linux)
- **DevTools** - Maven, Gradle, Node, Python, SDKMAN caches (macOS, Linux, Windows)
- **Projects** - Stale project detection and node_modules cleanup (macOS, Linux, Windows)
- **System** - OS caches, logs, temp files (macOS, Linux, Windows)

### Output Strategies (MCP preparation)
- **Rich** - Colored terminal output with Spectre.Console (default)
- **Plain** - Plain text for CI/CD and scripting
- **JSON** - Structured output for MCP server and programmatic use

### Interaction Strategies (MCP preparation)
- **Interactive** - Prompts and confirmations for CLI users
- **AutoConfirm** - Non-interactive mode for MCP server and CI/CD

## Current Status

**Phase 0** is in progress. The repository has been restructured to support parallel development of both versions.

**Bash version (v1.x)**: Stable, production-ready, located in `bash/`
**NET version (v2.x)**: Not started yet, will be located in `net/`

## Contributing

For contribution guidelines, see [CONTRIBUTING.md](CONTRIBUTING.md).

For the complete technical plan, see the [migration plan](https://github.com/aitorsantana/dev_sweep/issues/45).
