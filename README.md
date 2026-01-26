<div align="center">
  <img src="assets/icon.png" alt="DevSweep Logo" width="200" />
  
  # DevSweep
  
  **Professional macOS developer cache cleaner**
  
  _Reclaim gigabytes of disk space safely and intelligently_

  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
  [![Bash](https://img.shields.io/badge/Bash-5.0+-blue.svg)](https://www.gnu.org/software/bash/)
  [![Platform](https://img.shields.io/badge/Platform-macOS-lightgrey.svg)](https://www.apple.com/macos/)
  [![Tests](https://img.shields.io/badge/Tests-101%20passing-brightgreen.svg)](tests/)
</div>

---

## 🎯 Overview

DevSweep is a **production-grade CLI tool** that safely cleans deep system caches on macOS, helping developers reclaim valuable disk space without compromising system stability.

### 🧹 What It Cleans

- **🎨 JetBrains IDEs** - Removes old versions, keeps latest, cleans corrupted caches (~2-5GB)
- **🐳 Docker/OrbStack** - Containers, images, volumes, and build cache (~5-20GB)
- **🍺 Homebrew** - Old package versions, unused dependencies, download cache (~500MB-2GB)
- **⚙️ Dev Tools** - Maven, Gradle, npm, yarn, pnpm, pip caches (~5-15GB)
- **🗂️ System** _(Coming soon)_ - System logs and caches

**Average recovery**: 10-30GB of disk space

## ✨ Features

- 🛡️ **Safety First**: Built-in `--dry-run` mode to preview all actions
- 🎛️ **Interactive Menu**: User-friendly interface for guided cleanup
- 🔧 **Modular Architecture**: Clean, testable, extensible codebase
- ⚠️ **Double Confirmation**: Explicit approval for destructive operations
- 📊 **Smart Analysis**: Shows estimated space to be recovered
- 🎨 **Beautiful Output**: Colored, organized logging with progress indicators
- ✅ **Well Tested**: 101 tests, 123 assertions, 100% passing
-  📦 Installation

### Homebrew _(Coming soon)_

```bash
brew install devsweep
```

### Quick Install (Recommended)

```bash
# Clone and install locally (no sudo required)
git clone https://github.com/Sstark97/dev_sweep.git
cd dev_sweep
make install-local

# Verify installation
devsweep --version
```

The command will be available at `~/.local/bin/devsweep`

### System-wide Installation

```bash
git clone https://github.com/Sstark97/dev_sweep.git
cd dev_sweep
sudo make install

# Available globally
devsweep --version
```

### Uninstall

```bash
# Local installation
make uninstall-local

# System-wide
sudo make uninstall
```

## 🚀 Usagemd](HOMEBREW_CORE_SUBMISSION.md) for publishing to Homebrew.

## Usage
 🚀 Usage

### Quick Start

```bash
# Interactive mode - guided cleanup
devsweep

# Safe preview - see what would be deleted
devsweep --dry-run --all

# Clean everything (with confirmations)
devsweep --all
```

### Interactive Mode
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
# Clean only JetBrains with preview
devsweep --jetbrains --verbose

# Full cleanup of dev tools
devsweep --devtools --docker --homebrew

# Clean everything (skip confirmations - use with caution!)
devsweep --force --all
```

## 🛡️ Safety Features
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

## 💻 Development

### Prerequisites

- macOS 10.15+
- Bash 5.0+
- [bashunit](https://github.com/TypedDevs/bashunit) 0.32.0+ for testing

### Setup

```bash
# Clone repository
git clone https://github.com/Sstark97/dev_sweep.git
cd dev_sweep

# Install dependencies
make setup

# Run tests
make test
```

### Running Tests

```bash
# Run all tests (101 tests, ~11s)
make test

# Run specific test suite
./bashunit tests/unit/jetbrains_test.sh

# Watch mode (requires fswatch)
make watch-test
```

### Make Commands

```bash
make help              # Show all available commands
make test              # Run all tests
make lint              # Run shellcheck
make check             # Syntax validation
make install-local     # Install locally
make clean             # Remove temporary files
```

### Project Structure

```
dev_sweep/
├── bin/
│   └── devsweep              # Main entry point
├── src/
│   ├── modules/              # Cleanup modules
│   │   ├── jetbrains.sh      # JetBrains IDE cleanup
│   │   ├── docker.sh         # Docker/OrbStack cleanup
│   │   ├── homebrew.sh       # Homebrew cleanup
│   │   └── devtools.sh       # Dev tools cleanup
│   └── utils/                # Shared utilities
│       ├── config.sh         # Configuration
│       ├── common.sh         # Common functions
│       └── menu.sh           # Interactive menu
├── tests/
│   └── unit/                 # Unit tests (101 tests)
├── Makefile                  # Build automation
└── .bashunit.yml             # Test configuration
```

## 🔄 For Maintainers

### Creating a Release

```bash
# Complete automated release workflow
make publish VERSION=1.0.0
```

This command:
1. ✅ Runs all tests
2. ✅ Creates release tarball
3. ✅ Creates and pushes git tag
4. ⏸️ Pauses for GitHub release creation
5. ✅ Updates Homebrew formula with correct SHA256
6. ✅ Validates everything is ready

See [QUICKSTART_RELEASE.md](QUICKSTART_RELEASE.md) for details.

### Publishing to Homebrew

After creating a release, submit to Homebrew Core:

```bash
# Fork and clone homebrew-core
git clone https://github.com/YOUR_USERNAME/homebrew-core.git
cd homebrew-core

# Add formula
git checkout -b devsweep
cp ../dev_sweep/devsweep.rb Formula/devsweep.rb

# Test and submit
brew install --build-from-source Formula/devsweep.rb
brew test devsweep
brew audit --strict --online Formula/devsweep.rb

git add Formula/devsweep.rb
git commit -m "devsweep 1.0.0 (new formula)"
git push origin devsweep
```

See [HOMEBREW_CORE_SUBMISSION.md](HOMEBREW_CORE_SUBMISSION.md) for complete guide.

## 📊 Test Coverage

```
Tests:      101 passed, 101 total
Assertions: 123 passed, 123 total
Time:       ~11 seconds
Coverage:   All modules tested
```

## 🤝 Contributing

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Code style guidelines
- Testing requirements
- Pull request process
- Development workflow

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- Built with [bashunit](https://github.com/TypedDevs/bashunit) for professional testing
- Follows Clean Code principles and modern bash best practices
- Inspired by the need to reclaim disk space on developer machines

## 📚 Documentation

- [QUICKSTART.md](QUICKSTART.md) - Quick start guide
- [QUICKSTART_RELEASE.md](QUICKSTART_RELEASE.md) - Release workflow guide
- [HOMEBREW_CORE_SUBMISSION.md](HOMEBREW_CORE_SUBMISSION.md) - Homebrew publishing guide
- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines

## 🔗 Links

- **Repository**: https://github.com/Sstark97/dev_sweep
- **Issues**: https://github.com/Sstark97/dev_sweep/issues
- **Releases**: https://github.com/Sstark97/dev_sweep/releases

---

<div align="center">
  
  **Made with ❤️ by developers, for developers**
  
  If DevSweep saved you disk space, give it a ⭐!

</div>
