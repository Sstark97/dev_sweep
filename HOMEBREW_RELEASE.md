# Homebrew Release Guide

This guide explains how to publish DevSweep to Homebrew.

## Option 1: Quick Start - Your Own Homebrew Tap (Recommended)

The easiest way to distribute via Homebrew is to create your own "tap" (third-party repository).

### Step 1: Create a Homebrew Tap Repository

```bash
# Create a new GitHub repository named "homebrew-devsweep"
# (Must start with "homebrew-" prefix)
git clone https://github.com/YOUR_USERNAME/homebrew-devsweep.git
cd homebrew-devsweep
```

### Step 2: Create the Formula

```bash
# Copy the formula to your tap
cp /path/to/dev_sweep/devsweep.rb Formula/devsweep.rb

# Edit the formula to update URLs and SHA256
# Then commit and push
git add Formula/devsweep.rb
git commit -m "Add DevSweep formula"
git push origin main
```

### Step 3: Users Can Install

```bash
# Add your tap
brew tap YOUR_USERNAME/devsweep

# Install DevSweep
brew install devsweep
```

### Step 4: Updating the Formula

When you release a new version:

```bash
# In your dev_sweep project
./scripts/create-release.sh 1.1.0

# Create GitHub release v1.1.0 with the tarball
# Update Formula/devsweep.rb in your tap with new URL and SHA256
# Push changes
```

## Option 2: Submit to Homebrew Core (Advanced)

To get DevSweep into the official Homebrew repository:

### Requirements

1. **Stable project**: No frequent breaking changes
2. **Wide appeal**: Useful to many macOS developers
3. **Active maintenance**: Regular updates and bug fixes
4. **Good documentation**: README, license, etc.
5. **GitHub releases**: Proper versioned releases

### Process

1. **Create a GitHub Release**
   ```bash
   # Generate the release tarball
   ./scripts/create-release.sh 1.0.0
   
   # Create GitHub release v1.0.0 and upload the tarball
   ```

2. **Test the Formula Locally**
   ```bash
   # Update devsweep.rb with actual URL and SHA256
   brew install --build-from-source ./devsweep.rb
   brew test devsweep
   brew audit --strict devsweep
   ```

3. **Submit Pull Request**
   ```bash
   # Fork homebrew-core
   git clone https://github.com/Homebrew/homebrew-core.git
   cd homebrew-core
   
   # Create branch
   git checkout -b devsweep
   
   # Copy formula
   cp /path/to/devsweep.rb Formula/devsweep.rb
   
   # Commit and push
   git add Formula/devsweep.rb
   git commit -m "devsweep 1.0.0 (new formula)"
   git push origin devsweep
   
   # Create PR on GitHub
   ```

4. **PR Requirements**
   - Formula must pass `brew audit --strict`
   - Must include a test block that works
   - No dependencies on other taps
   - URL must be from a stable GitHub release

## Local Testing Workflow

### Test Installation from Source

```bash
# Install from local formula
brew install --build-from-source ./devsweep.rb

# Test the installation
devsweep --version
devsweep --dry-run --all

# Run formula tests
brew test devsweep

# Audit the formula
brew audit --strict devsweep

# Uninstall
brew uninstall devsweep
```

### Test Installation from GitHub Release

```bash
# After creating a GitHub release
# Update devsweep.rb with real URL and SHA256
brew install --build-from-source ./devsweep.rb
```

## Release Checklist

- [ ] Update version in bin/devsweep (if applicable)
- [ ] Run all tests: `make test`
- [ ] Create release tarball: `./scripts/create-release.sh X.Y.Z`
- [ ] Create GitHub release vX.Y.Z
- [ ] Upload tarball to GitHub release
- [ ] Update devsweep.rb with new URL and SHA256
- [ ] Test installation: `brew install --build-from-source ./devsweep.rb`
- [ ] Test execution: `devsweep --version` and `devsweep --dry-run --all`
- [ ] Run formula audit: `brew audit --strict devsweep`
- [ ] Update tap repository (if using your own tap)
- [ ] Tag repository: `git tag v1.0.0 && git push origin v1.0.0`

## Troubleshooting

### Formula doesn't install

```bash
# Check formula syntax
brew audit --strict ./devsweep.rb

# Install with verbose output
brew install --build-from-source --verbose ./devsweep.rb
```

### Command not found after install

```bash
# Check if binary is linked
ls -l $(brew --prefix)/bin/devsweep

# Try relinking
brew unlink devsweep
brew link devsweep
```

### SHA256 mismatch

```bash
# Recalculate SHA256
shasum -a 256 dist/devsweep-1.0.0.tar.gz

# Update devsweep.rb with the correct hash
```

## Resources

- [Homebrew Formula Cookbook](https://docs.brew.sh/Formula-Cookbook)
- [Homebrew Acceptable Formulae](https://docs.brew.sh/Acceptable-Formulae)
- [How to Create and Maintain a Tap](https://docs.brew.sh/How-to-Create-and-Maintain-a-Tap)
- [Formula Guidlines](https://docs.brew.sh/Formula-Cookbook#guidelines)
