# Release Quick Start

## Release a New Version

```bash
# Complete automated workflow
make publish VERSION=1.0.0
```

This command will:
1. ✅ Run all tests
2. ✅ Create release tarball in `dist/`
3. ✅ Create and push git tag `v1.0.0`
4. ⏸️  Pause for you to create GitHub release
5. ✅ Download GitHub tarball and update formula SHA256
6. ✅ Test formula locally

## Individual Commands

```bash
# Create release tarball
make release VERSION=1.0.0

# Create and push git tag
make tag VERSION=1.0.0

# Update Homebrew formula (after GitHub release)
make formula VERSION=1.0.0

# Test formula locally
make test-formula
```

## GitHub Release Steps (Manual)

When `make publish` pauses:

1. Go to https://github.com/Sstark97/dev_sweep/releases/new
2. Select tag: `v1.0.0`
3. Title: `DevSweep v1.0.0`
4. Describe changes
5. **Publish** (don't upload files - GitHub generates tarball automatically)
6. Press ENTER in terminal to continue

## Submit to Homebrew Core

After `make publish` completes:

```bash
# Fork Homebrew Core first
# https://github.com/Homebrew/homebrew-core/fork

# Clone your fork
git clone https://github.com/YOUR_USERNAME/homebrew-core.git
cd homebrew-core

# Create branch and add formula
git checkout -b devsweep
cp ../dev_sweep/devsweep.rb Formula/devsweep.rb

# Test and commit
brew install --build-from-source Formula/devsweep.rb
brew test devsweep
brew audit --strict --online Formula/devsweep.rb
brew uninstall devsweep

git add Formula/devsweep.rb
git commit -m "devsweep 1.0.0 (new formula)"
git push origin devsweep

# Create PR on GitHub to Homebrew/homebrew-core
```

Full guide: [HOMEBREW_CORE_SUBMISSION.md](HOMEBREW_CORE_SUBMISSION.md)
