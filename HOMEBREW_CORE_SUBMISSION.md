# Submitting DevSweep to Homebrew Core

This guide explains how to submit DevSweep to **Homebrew Core** so users can install with just:
```bash
brew install devsweep
```

## Prerequisites

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

## Step-by-Step Submission Process

### 1. Create a GitHub Release

```bash
# Tag the release
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# Create the release tarball
./scripts/create-release.sh 1.0.0
```

Go to GitHub → Releases → Create new release:
- Tag: `v1.0.0`
- Title: `DevSweep v1.0.0 - Initial Release`
- Description: List key features and changes
- Upload: `dist/devsweep-1.0.0.tar.gz`
- Publish release

### 2. Verify Release URL and SHA256

Once the GitHub release is published, the tarball URL will be:
```
https://github.com/Sstark97/dev_sweep/archive/refs/tags/v1.0.0.tar.gz
```

**IMPORTANT**: GitHub generates a NEW tarball when you create a release, so the SHA256 will be different from your local one. You need to download it and calculate the new SHA256:

```bash
# Download the release tarball
curl -L https://github.com/Sstark97/dev_sweep/archive/refs/tags/v1.0.0.tar.gz -o github-release.tar.gz

# Calculate SHA256
shasum -a 256 github-release.tar.gz

# Update devsweep.rb with this new SHA256!
```

### 3. Test the Formula Locally

```bash
# Test installation from the GitHub release
brew install --build-from-source ./devsweep.rb

# Verify it works
devsweep --version
devsweep --dry-run --all

# Run formula tests
brew test devsweep

# Check for issues
brew audit --strict --online ./devsweep.rb

# Clean up
brew uninstall devsweep
```

The audit should pass with **no warnings or errors** before submitting.

### 4. Fork and Clone Homebrew Core

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
git add Formula/devsweep.rb
git commit -m "devsweep 1.0.0 (new formula)"

# Push to your fork
git push origin devsweep
```

### 7. Create Pull Request

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

### 8. Review Process

Homebrew maintainers will review your PR. They may ask for changes:

**Common requests**:
- Fix audit warnings
- Improve formula tests
- Clarify the description
- Simplify installation logic

**Timeline**: Usually 1-7 days for review.

## What Happens After Approval

Once merged:
1. Formula goes live in homebrew-core
2. Users can install with: `brew install devsweep`
3. Homebrew auto-updates every few hours
4. Your package is now officially in Homebrew! 🎉

## Updating the Formula (Future Releases)

For version updates:

```bash
# In homebrew-core repo
git checkout master
git pull upstream master
git checkout -b devsweep-1.1.0

# Update Formula/devsweep.rb with new version, URL, and SHA256
# Test it
brew install --build-from-source Formula/devsweep.rb
brew test devsweep
brew audit --strict --online Formula/devsweep.rb

# Commit and create PR
git add Formula/devsweep.rb
git commit -m "devsweep 1.1.0"
git push origin devsweep-1.1.0
```

## Alternative: Start with Your Own Tap First

If you're not ready for Homebrew Core, you can start with your own tap:

```bash
# Create repository: homebrew-tap
# Users install with:
brew tap sstark97/tap
brew install devsweep
```

This gives you experience with Homebrew and lets users start installing your tool while you build up the project for Homebrew Core submission.

## Resources

- [Homebrew Core Guidelines](https://docs.brew.sh/Acceptable-Formulae)
- [Formula Cookbook](https://docs.brew.sh/Formula-Cookbook)
- [How to Open a PR](https://docs.brew.sh/How-To-Open-a-Homebrew-Pull-Request)
- [Maintainer Guidelines](https://docs.brew.sh/Maintainer-Guidelines)

## Current Status

Repository: `https://github.com/Sstark97/dev_sweep`
Formula ready: `devsweep.rb`

**Next step**: Create GitHub release v1.0.0 with tag and tarball.
