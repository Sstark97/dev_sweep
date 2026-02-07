# DevSweep - Quick Start Guide

## Installation

```bash
# Clone the repository
git clone <your-repo-url>
cd dev_sweep

# Install bashunit for testing
make install-bashunit

# Run tests
make test

# Install system-wide (optional)
make install
```

## Usage

### 1. Safe Preview (Recommended First Step)
```bash
# See what would be cleaned without deleting anything
./bin/devsweep --dry-run --all
```

### 2. Interactive Mode
```bash
# Launch menu to select modules
./bin/devsweep
```

### 3. Clean Specific Module
```bash
# Clean only JetBrains IDEs
./bin/devsweep --jetbrains
```

### 4. Full Cleanup
```bash
# Clean everything (with confirmations)
./bin/devsweep --all
```

## Common Commands

```bash
# Help
./bin/devsweep --help

# Version
./bin/devsweep --version

# Verbose output
./bin/devsweep --verbose --jetbrains

# Skip confirmations (DANGEROUS!)
./bin/devsweep --force --all

# Run tests
make test

# Syntax check
make check

# Install
make install

# Uninstall
make uninstall
```

## What Gets Cleaned?

### JetBrains IDEs (✅ Implemented)
- **Old Versions:** Removes all but the latest version
- **Index Caches:** Clears corrupted/outdated caches
- **Processes:** Safely terminates running IDEs

### Docker (🔜 Coming Soon)
- Docker Desktop data
- Unused containers/images

### Homebrew (🔜 Coming Soon)
- Homebrew cache
- Old package versions

### Dev Tools (🔜 Coming Soon)
- Maven repository
- Gradle caches
- Node/npm caches

### System (🔜 Coming Soon)
- System logs
- User caches
- Spotlight index rebuild

## Safety Features

✅ **Dry-run mode** - Preview before deletion
✅ **Path validation** - Prevents accidental root/home deletion  
✅ **Confirmations** - Interactive prompts for dangerous operations
✅ **Logging** - Full transparency of actions taken
✅ **Version preservation** - Keeps latest JetBrains IDE versions

## Troubleshooting

### Bash Version Warning
```bash
# Install newer bash (recommended)
brew install bash
/usr/local/bin/bash ./bin/devsweep --help
```

### Permission Denied
```bash
# Make executable
chmod +x bin/devsweep
```

### Tests Failing
```bash
# Reinstall bashunit
rm ./bashunit
make install-bashunit
make test
```

## Development

```bash
# Create a new module
cp src/modules/jetbrains.sh src/modules/newmodule.sh
# Edit and implement your module

# Run specific test file
./bashunit tests/unit/common_test.sh

# Check syntax
make check

# Run linter (requires shellcheck)
make lint
```

## Next Steps

1. ⭐ Star the repo
2. 🐛 Report issues
3. 🔧 Contribute modules
4. 📖 Read CONTRIBUTING.md
5. 🚀 Share with other developers

---

**Made with ❤️ for developers who value disk space**
