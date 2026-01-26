# DevSweep

> Professional macOS developer cache cleaner - Reclaim gigabytes of disk space safely.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Bash](https://img.shields.io/badge/Bash-5.0+-blue.svg)](https://www.gnu.org/software/bash/)
[![Platform](https://img.shields.io/badge/Platform-macOS-lightgrey.svg)](https://www.apple.com/macos/)

## Overview

DevSweep is a production-grade CLI tool that safely cleans deep system caches on macOS, specifically targeting:

- **JetBrains IDEs** - Removes old versions, keeps the latest, cleans corrupted index caches
- **Docker/OrbStack** - Resets container data and reclaims disk space
- **Homebrew** - Aggressive cleanup and cache pruning
- **Dev Tools** - Maven, Gradle, Node, npm, nvm cache cleanup
- **System** - Logs, caches, and Spotlight index rebuild

## Features

- **Safety First**: Built-in `--dry-run` mode to preview actions
- **Interactive Menu**: User-friendly interface when run without arguments
- **Modular Architecture**: Clean, testable, extensible codebase
- **Double Confirmation**: Explicit "type yes" for destructive operations
- **Comprehensive Logging**: Colored output with verbosity control
- **Well Tested**: Full bashunit test suite with mocking

## Installation

### Homebrew (Recommended)

```bash
# Coming soon
brew tap your-username/devsweep
brew install devsweep
```

### Manual Installation

```bash
git clone https://github.com/your-username/devsweep.git
cd devsweep
make install
```

## Usage

### Interactive Mode (Default)

```bash
devsweep
```

Launches an interactive menu to select which modules to run.

### Command-Line Flags

```bash
# Run all cleanup modules (with confirmations)
devsweep --all

# Safe preview mode - no files deleted
devsweep --dry-run --all

# Clean specific modules
devsweep --jetbrains
devsweep --docker --homebrew

# Skip confirmations (use with caution!)
devsweep --force --all

# Verbose output
devsweep --verbose --jetbrains
```

### Available Flags

| Flag | Short | Description |
|------|-------|-------------|
| `--help` | `-h` | Show help message |
| `--version` | `-v` | Show version |
| `--dry-run` | `-d` | Preview mode - no deletions |
| `--verbose` | | Detailed output |
| `--force` | `-f` | Skip confirmations |
| `--all` | `-a` | Run all cleanup modules |
| `--jetbrains` | | Clean JetBrains IDEs only |
| `--docker` | | Clean Docker/OrbStack only |
| `--homebrew` | | Clean Homebrew only |
| `--devtools` | | Clean dev tools (Maven, Gradle, Node) |
| `--system` | | Clean system caches and logs |

## Safety Features

1. **Dry-Run Mode**: Test commands without side effects
2. **Interactive Confirmations**: Explicit approval for dangerous operations
3. **Sudo Validation**: Only requests elevated privileges when necessary
4. **Detailed Logging**: Full transparency of actions taken
5. **Version Preservation**: Keeps latest JetBrains IDE versions

## Examples

```bash
# Safe exploration - see what would be deleted
devsweep --dry-run --all

# Clean only JetBrains caches (interactive confirmation)
devsweep --jetbrains

# Full cleanup with verbose output
devsweep --verbose --all

# Nuclear option (skip all prompts)
devsweep --force --all
```

## Development

### Prerequisites

- macOS 10.15+
- Bash 5.0+
- [bashunit](https://github.com/TypedDevs/bashunit) for testing

### Running Tests

```bash
# Install bashunit
make install-bashunit

# Run all tests
make test

# Run specific test file
./bashunit tests/unit/jetbrains_test.sh
```

### Project Structure

```
dev_sweep/
├── bin/devsweep              # Entry point
├── src/
│   ├── modules/              # Cleanup logic modules
│   │   ├── jetbrains.sh
│   │   ├── docker.sh
│   │   ├── homebrew.sh
│   │   ├── devtools.sh
│   │   └── system.sh
│   └── utils/                # Shared utilities
│       ├── config.sh
│       ├── common.sh
│       └── menu.sh
└── tests/                    # Test suite
    ├── unit/
    └── integration/
```

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Author

Built with craftsmanship by a Senior Software Engineer who values clean code, safety, and professional tooling.

## Acknowledgments

- Inspired by the need to reclaim disk space on developer machines
- Built with [bashunit](https://github.com/TypedDevs/bashunit) for professional testing
- Follows Clean Code principles and modern bash best practices
