# Publishing DevSweep via Homebrew

## Current Status: Personal Tap

DevSweep is currently available via a **personal Homebrew tap**:

```bash
brew tap sstark97/tap
brew install devsweep
```

### Why Not Homebrew Core?

Homebrew Core has strict [notability requirements](https://docs.brew.sh/Acceptable-Formulae#niche-or-self-submitted-stuff) for new formulae:

- Must have significant user base and community adoption
- Cannot be self-submitted niche projects
- Needs to demonstrate widespread utility

**Our path forward:**
1. ✅ Build community and user base via personal tap
2. ✅ Gather feedback and improve the tool
3. ✅ Gain GitHub stars and recognition
4. 🔄 Resubmit to Homebrew Core once notable

## Publishing to Your Own Tap (Current Approach)

## Publishing to Your Own Tap (Current Approach)

### Step 1: Create Tap Repository

```bash
# Create a new GitHub repository named: homebrew-tap
# (Must start with "homebrew-" prefix)
```

### Step 2: Set Up Tap Structure

```bash
# Clone your tap repository
git clone https://github.com/Sstark97/homebrew-tap.git
cd homebrew-tap

# Create Formula directory
mkdir -p Formula

# Copy the formula
cp ../dev_sweep/devsweep.rb Formula/devsweep.rb

# Commit and push
git add Formula/devsweep.rb
git commit -m "Add devsweep formula"
git push origin main
```

### Step 3: Users Can Install

```bash
# Add your tap
brew tap sstark97/tap

# Install DevSweep
### Requirements for Homebrew Core

**Notability Indicators:**
- 75+ GitHub stars
- Active community (issues, PRs, discussions)
- Multiple contributors
- Used by organizations or notable projects
- Positive reception in developer communities
- Documentation of widespread usage

**Technical Requirements:**

✅ **Stable and Maintained**
- No frequent breaking changes
- Active maintenance and bug fixes
- Responsive to issues

✅ **Good Documentation**
- [x] Clear README
- [x] License (MIT)
- [x] Contributing guidelines

✅ **Versioned Releases**
- [x] Semantic versioning
- [x] Proper GitHub releases
- [x] Tagged commits

✅ **No Closed-Source Dependencies**
- [x] All dependencies open source

### Submission Process (When Ready)changes
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

### Submission Process (When Ready)

1. **Create release** using `make publish VERSION=X.Y.Z`
2. **Fork Homebrew Core** on GitHub
3. **Add formula** to `Formula/devsweep.rb`
4. **Test thoroughly**
5. **Submit PR** with evidence of notability

See [Homebrew documentation](https://docs.brew.sh/Acceptable-Formulae) for complete guidelines.

## Current Workflow

### Release and Update Taps - GitHub generates tarball automatically)

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
