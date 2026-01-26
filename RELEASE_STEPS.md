# Release Steps - Quick Reference

## 1. Install DevSweep

### Local Installation (Recommended, no sudo)
```bash
make install-local
```
Command available at: `~/.local/bin/devsweep`

### System-wide Installation (requires sudo)
```bash
sudo make install
```
Command available at: `/usr/local/bin/devsweep`

### Uninstall
```bash
make uninstall-local    # For local installation
sudo make uninstall     # For system-wide installation
```

## 2. Create a GitHub Release

### Step 1: Create Release Tarball
```bash
./scripts/create-release.sh 1.0.0
```
This creates `dist/devsweep-1.0.0.tar.gz` and displays the SHA256 hash.

### Step 2: Create GitHub Release
1. Go to GitHub → Releases → Create new release
2. Tag: `v1.0.0`
3. Title: `DevSweep v1.0.0`
4. Upload `dist/devsweep-1.0.0.tar.gz`
5. Publish release

## 3. Publish to Homebrew

### Option A: Your Own Tap (Easiest)

1. **Create tap repository on GitHub**
   ```bash
   # Repository name MUST be: homebrew-devsweep
   ```

2. **Update formula with release URL**
   ```bash
   # Edit devsweep.rb:
   # - Update URL to point to GitHub release tarball
   # - Update sha256 with value from create-release.sh
   ```

3. **Copy formula to tap**
   ```bash
   git clone https://github.com/YOUR_USERNAME/homebrew-devsweep.git
   cp devsweep.rb homebrew-devsweep/Formula/devsweep.rb
   cd homebrew-devsweep
   git add Formula/devsweep.rb
   git commit -m "Add devsweep 1.0.0"
   git push
   ```

4. **Users can now install**
   ```bash
   brew tap YOUR_USERNAME/devsweep
   brew install devsweep
   ```

### Option B: Official Homebrew (Advanced)

See [HOMEBREW_RELEASE.md](HOMEBREW_RELEASE.md) for complete instructions.

## 4. Test Before Publishing

```bash
# Test local formula
brew install --build-from-source ./devsweep.rb

# Verify it works
devsweep --version
devsweep --dry-run --all

# Run formula tests
brew test devsweep

# Audit formula
brew audit --strict devsweep

# Clean up
brew uninstall devsweep
```

## Quick Publishing Checklist

- [ ] All tests pass: `make test`
- [ ] Version updated in `bin/devsweep` (if applicable)
- [ ] Create release: `./scripts/create-release.sh X.Y.Z`
- [ ] Copy SHA256 hash
- [ ] Create GitHub release vX.Y.Z with tarball
- [ ] Update `devsweep.rb` with URL and SHA256
- [ ] Test formula: `brew install --build-from-source ./devsweep.rb`
- [ ] Push to tap: Copy formula to `homebrew-devsweep` repo
- [ ] Tag release: `git tag v1.0.0 && git push --tags`

## Current Status

✅ Project installed locally at: `~/.local/bin/devsweep`  
✅ Release tarball created: `dist/devsweep-1.0.0.tar.gz`  
✅ Homebrew formula ready: `devsweep.rb`

**Next step**: Create GitHub repository and first release, then publish to Homebrew tap.
