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

## 3. Publish to Homebrew Core (for `brew install devsweep`)

See [HOMEBREW_CORE_SUBMISSION.md](HOMEBREW_CORE_SUBMISSION.md) for the complete guide.

### Quick Process:

1. **Create GitHub Release**
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```
   Then create release on GitHub and upload `dist/devsweep-1.0.0.tar.gz`

2. **Download and verify SHA256** (GitHub generates a new tarball)
   ```bash
   curl -L https://github.com/Sstark97/dev_sweep/archive/refs/tags/v1.0.0.tar.gz -o github-release.tar.gz
   shasum -a 256 github-release.tar.gz
   # Update devsweep.rb with this SHA256
   ```

3. **Test locally**
   ```bash
   brew install --build-from-source ./devsweep.rb
   brew test devsweep
   brew audit --strict --online ./devsweep.rb
   ```

4. **Submit to Homebrew Core**
   ```bash
   # Fork Homebrew/homebrew-core on GitHub
   git clone https://github.com/YOUR_USERNAME/homebrew-core.git
   cd homebrew-core
   git checkout -b devsweep
   cp /path/to/devsweep.rb Formula/devsweep.rb
   git add Formula/devsweep.rb
   git commit -m "devsweep 1.0.0 (new formula)"
   git push origin devsweep
   # Create PR on GitHub
   ```

### Alternative: Your Own Tap (Faster)

If not ready for Homebrew Core, create your own tap first:

### Alternative: Your Own Tap (Faster)

If not ready for Homebrew Core, create your own tap first:

1. **Create tap repository on GitHub**
   ```bash
   # Repository name MUST be: homebrew-tap (or homebrew-devsweep)
   ```

2. **Copy formula to tap**
   ```bash
   git clone https://github.com/Sstark97/homebrew-tap.git
   cp devsweep.rb homebrew-tap/Formula/devsweep.rb
   cd homebrew-tap
   git add Formula/devsweep.rb
   git commit -m "Add devsweep 1.0.0"
   git push
   ```

3. **Users install with**
   ```bash
   brew tap sstark97/tap
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
