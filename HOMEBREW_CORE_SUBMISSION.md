# Submitting DevSweep to Homebrew Core

This guide explains how to submit DevSweep to **Homebrew Core** so users can install with:
```bash
brew install devsweep
```

## Quick Start

Use the Makefile commands for the entire release workflow:

```bash
# Full automated release
make publish VERSION=1.0.0
```

This runs all steps automatically:
1. ✅ Runs tests
2. ✅ Creates release tarball
3. ✅ Creates and pushes git tag
4. ⏸️  Pauses for you to create GitHub release
5. ✅ Downloads GitHub tarball and updates formula with correct SHA256
6. ✅ Tests formula locally

## Individual Commands

```bash
# Create release tarball only
make release VERSION=1.0.0

# Create and push git tag
make tag VERSION=1.0.0

# Update Homebrew formula with GitHub release
make formula VERSION=1.0.0

# Test formula locally
make test-formula
```

## Homebrew Core Requirements

## Homebrew Core Requirements

Before submitting to Homebrew Core, your project must meet these requirements:

✅ **Stable and Maintained**
- [ ] No frequent breaking changes
- [ ] Active maintenance and bug fixes
- [ ] Responsive to issues

✅ **Notable/Widely Useful**
- [ ] Useful to many macOS developers
- [ ] Not a niche/personal tool
- [ ] Solves a real problem

✅ **Good Documentation**
- [x] Clear README
- [x] License (MIT)
- [x] Contributing guidelines

✅ **Versioned Releases**
- [ ] Semantic versioning (v1.0.0)
- [ ] Proper GitHub releases
- [ ] Tagged commits

✅ **No Closed-Source Dependencies**
- [x] All dependencies open source
- [x] No proprietary requirements

## Submission Process

### 1. Release Your Package

```bash
# Run the full release workflow
make publish VERSION=1.0.0
```

The command will:
- Run all tests
- Create tarball in `dist/`
- Create and push git tag `v1.0.0`
- Prompt you to create GitHub release
- Update `devsweep.rb` with correct SHA256 from GitHub
- Test the formula locally

**Important**: When prompted, create the GitHub release:
1. Go to https://github.com/Sstark97/dev_sweep/releases/new
2. Select tag `v1.0.0`
3. Title: `DevSweep v1.0.0`
4. Describe changes
5. Publish (do NOT upload files - GitHub generates tarball automatically)

### 2. Submit to Homebrew Core

```bash
# Fork homebrew-core on GitHub first
# Then clone your fork
git clone https://github.com/YOUR_USERNAME/homebrew-core.git
cd homebrew-core

# Add upstream remote
git remote add upstream https://github.com/Homebrew/homebrew-core.git
git fetch upstream
```

### 5. Create a Branch and Add Formula

```bash
# Create branch (use lowercase package name)
git checkout -b devsweep

# Copy your formula
cp /path/to/dev_sweep/devsweep.rb Formula/devsweep.rb

# Test it works from homebrew-core
brew install --build-from-source Formula/devsweep.rb
brew test devsweep
brew audit --strict --online Formula/devsweep.rb

# Clean up
brew uninstall devsweep
```

### 6. Commit and Push

```bash
# Commit with the exact format
### 2. Submit to Homebrew Core

```bash
# Fork Homebrew Core on GitHub first
# https://github.com/Homebrew/homebrew-core/fork

# Clone your fork
git clone https://github.com/YOUR_USERNAME/homebrew-core.git
cd homebrew-core
### 3. Create Pull Request

Go to your fork on GitHub and create a PR to `Homebrew/homebrew-core`:

**PR Title**: `devsweep 1.0.0 (new formula)`

**PR Description**:
```markdown
## Description
DevSweep is an intelligent macOS developer cache cleaner that safely reclaims disk space by cleaning:
- JetBrains IDEs (old versions, corrupted caches)
- Docker/OrbStack containers and images
- Homebrew packages and caches
- Dev tools (Maven, Gradle, npm, nvm)

## Features
- Safe dry-run mode
- Interactive menu
- Double confirmation for destructive operations
- Modular architecture with comprehensive tests

## Checklist
- [x] Formula tested locally
- [x] Passes `brew audit --strict --online`
- [x] Passes `brew test`
- [x] License: MIT
- [x] GitHub release with tarball
```

### 4at Happens After Approval

Once merged:
1. Formula goes live in homebrew-core
2. Users can install with: `brew install devsweep`
3. Homebrew auto-updates every few hours
4. Your package is now officially in Homebrew! 🎉

## Updating the Formula (Future Releases)

For version updates:
your dev_sweep project
make publish VERSION=1.1.0

# This handles everything automatically
# Then just update and submit the formula to Homebrew Core again
git commit -m "devsweep 1.1.0"
git push origin devsweep-1.1.0
```

## Alternative: Start with Your Own Tap First

If you're not ready for Homebrew Core, you can start with your own tap:

```bash
# Create repository: homebrew-tap
# Users install with:
brew tap sstark97/tap
brew install devYour Own Tap (Faster Start)

If not ready for Homebrew Core, create your own tap:

```bash
# Create repository on GitHub: homebrew-tap

# Clone it
git clone https://github.com/Sstark97/homebrew-tap.git
cd homebrew-tap

# Add formula
mkdir -p Formula
cp ../dev_sweep/devsweep.rb Formula/devsweep.rb
git add Formula/devsweep.rb
git commit -m "Add devsweep 1.0.0"
git push

# Users install with:
# brew tap sstark97/tap

# Then submit to Homebrew Core following steps above
```

Repository: https://github.com/Sstark97/dev_sweep  
Formula: `devsweep.rb`
## Current Status

Repository: `https://github.com/Sstark97/dev_sweep`
Formula ready: `devsweep.rb`

**Next step**: Create GitHub release v1.0.0 with tag and tarball.
