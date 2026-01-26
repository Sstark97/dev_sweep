# Contributing to DevSweep

Thank you for your interest in contributing to DevSweep! This document provides guidelines and instructions for contributing.

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help maintain a welcoming environment

## Development Setup

### Prerequisites

- macOS 10.15+
- Bash 5.0+
- Git

### Getting Started

1. Fork and clone the repository:
```bash
git clone https://github.com/your-username/devsweep.git
cd devsweep
```

2. Install development dependencies:
```bash
make setup
```

3. Run tests to verify setup:
```bash
make test
```

## Project Structure

```
dev_sweep/
├── bin/devsweep              # Main entry point
├── src/
│   ├── modules/              # Cleanup modules
│   │   └── jetbrains.sh
│   └── utils/                # Shared utilities
│       ├── config.sh
│       ├── common.sh
│       └── menu.sh
└── tests/                    # Test suite
    └── unit/
```

## Development Workflow

### 1. Create a Branch

```bash
git checkout -b feature/your-feature-name
```

Use prefixes:
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation
- `refactor/` - Code refactoring
- `test/` - Test additions/fixes

### 2. Make Changes

Follow these coding standards:

#### Bash Style Guide

- Use `set -euo pipefail` in all scripts
- Use `local` for function variables
- Use `readonly` for constants
- Quote all variables: `"$var"` not `$var`
- Use `[[` instead of `[` for conditionals
- Prefer `$(command)` over backticks
- Use descriptive function names: `verb_noun` pattern

#### Function Documentation

```bash
# Brief description of what the function does
# Usage: function_name <arg1> <arg2>
# Returns: 0 on success, 1 on error
function_name() {
    local arg1="$1"
    local arg2="$2"

    # Implementation
}
```

#### Safety Requirements

- ALL destructive operations must use `safe_rm` or `safe_kill`
- ALL destructive operations must respect `DRY_RUN` flag
- Validate all paths before deletion (no empty, root, or HOME)
- Use `confirm_dangerous` for operations that delete data

### 3. Write Tests

Every new function requires tests:

```bash
# tests/unit/mymodule_test.sh

function test_myfunction_handles_edge_case() {
    # Setup
    local input="test"

    # Execute
    myfunction "$input"

    # Assert
    assert_successful_code "$?"
}
```

Run tests:
```bash
make test
```

### 4. Run Quality Checks

```bash
# Syntax check
make check

# Shellcheck linting
make lint
```

### 5. Test Manually

```bash
# Dry-run to verify behavior
./bin/devsweep --dry-run --jetbrains

# Test help output
./bin/devsweep --help

# Test verbose mode
./bin/devsweep --verbose --dry-run --all
```

### 6. Commit Changes

Use conventional commits:

```bash
git commit -m "feat: add Docker cleanup module"
git commit -m "fix: handle empty JetBrains directory"
git commit -m "docs: update installation instructions"
git commit -m "test: add tests for safe_rm function"
```

Prefixes:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation
- `test:` - Tests
- `refactor:` - Code refactoring
- `chore:` - Maintenance

### 7. Push and Create PR

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub.

## Adding New Modules

To add a new cleanup module:

### 1. Create Module File

Create `src/modules/newmodule.sh`:

```bash
#!/usr/bin/env bash
# newmodule.sh - Description of what this module cleans

set -euo pipefail

# Main entry point
# Usage: newmodule_clean
# Returns: 0 on success, 1 on error
newmodule_clean() {
    log_section "New Module Cleanup"

    # Your implementation here
    # Use safe_rm, safe_kill, etc.

    log_success "New module cleanup completed"
    return 0
}
```

### 2. Add to Main Script

In `bin/devsweep`, add:

```bash
# Source the module
source "$PROJECT_ROOT/src/modules/newmodule.sh"

# Add to argument parsing
--newmodule)
    SELECTED_MODULES+=("newmodule")
    shift
    ;;

# Add to execution
newmodule)
    if newmodule_clean; then
        log_success "Module completed: newmodule"
    else
        log_error "Module failed: newmodule"
        ((failed++))
    fi
    ;;
```

### 3. Write Tests

Create `tests/unit/newmodule_test.sh`:

```bash
#!/usr/bin/env bash

# Source dependencies
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/newmodule.sh"

function test_newmodule_clean_succeeds() {
    newmodule_clean
    assert_successful_code "$?"
}
```

### 4. Update Documentation

- Add module description to README.md
- Add usage examples
- Document any special requirements

## Testing Guidelines

### Test Structure

```bash
function setup() {
    # Runs before each test
    # Reset state, create temp directories
}

function teardown() {
    # Runs after each test
    # Clean up temp files
}

function test_descriptive_name() {
    # Arrange
    local input="test"

    # Act
    result=$(function_to_test "$input")

    # Assert
    assert_same "expected" "$result"
}
```

### Common Assertions

- `assert_successful_code` - Exit code is 0
- `assert_general_error` - Exit code is non-zero
- `assert_same` - Exact match
- `assert_contains` - Substring match
- `assert_empty` - String is empty
- `assert_file_exists` - File exists
- `assert_directory_exists` - Directory exists

### Mocking

```bash
# Mock a command
mock find echo "/fake/path/file.txt"

# Spy on a function
spy safe_rm

# Verify spy was called
assert_have_been_called safe_rm
assert_have_been_called_with safe_rm "/path/to/file"
```

## Pull Request Guidelines

### PR Checklist

- [ ] Tests pass (`make test`)
- [ ] Syntax checks pass (`make check`)
- [ ] Code follows style guide
- [ ] All functions have tests
- [ ] Destructive operations use safety wrappers
- [ ] Documentation updated
- [ ] Commit messages follow conventions

### PR Description Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Refactoring

## Testing
How did you test this?

## Screenshots (if applicable)

## Additional Notes
```

## Getting Help

- Open an issue for bugs or feature requests
- Start a discussion for questions
- Join our community chat (coming soon)

## Recognition

Contributors will be recognized in:
- README.md contributors section
- Release notes
- Project documentation

Thank you for contributing to DevSweep!
