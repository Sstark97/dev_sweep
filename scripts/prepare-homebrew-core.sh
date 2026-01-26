#!/usr/bin/env bash
# Helper script to prepare DevSweep for Homebrew Core submission

set -e

VERSION="${1:-1.0.0}"
REPO_URL="https://github.com/Sstark97/dev_sweep"

echo "🚀 DevSweep Homebrew Core Preparation"
echo "======================================"
echo ""
echo "Version: ${VERSION}"
echo "Repository: ${REPO_URL}"
echo ""

# Check if tag exists
if git rev-parse "v${VERSION}" >/dev/null 2>&1; then
    echo "✅ Tag v${VERSION} already exists"
else
    echo "⚠️  Tag v${VERSION} does not exist yet"
    read -p "Create tag v${VERSION} and push it? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        git tag -a "v${VERSION}" -m "Release version ${VERSION}"
        git push origin "v${VERSION}"
        echo "✅ Tag created and pushed"
    else
        echo "❌ Tag required for release. Exiting."
        exit 1
    fi
fi

echo ""
echo "📦 Creating release tarball..."
./scripts/create-release.sh "${VERSION}"

echo ""
echo "📥 Downloading GitHub release tarball..."
RELEASE_URL="${REPO_URL}/archive/refs/tags/v${VERSION}.tar.gz"
curl -L "${RELEASE_URL}" -o "/tmp/devsweep-github-${VERSION}.tar.gz" 2>/dev/null || {
    echo ""
    echo "⚠️  Could not download from GitHub yet. This is normal if:"
    echo "   1. You haven't created the GitHub release yet"
    echo "   2. The tag was just pushed (wait a minute)"
    echo ""
    echo "📋 Next steps:"
    echo "   1. Go to: ${REPO_URL}/releases/new"
    echo "   2. Select tag: v${VERSION}"
    echo "   3. Title: DevSweep v${VERSION}"
    echo "   4. Upload: dist/devsweep-${VERSION}.tar.gz"
    echo "   5. Publish the release"
    echo "   6. Run this script again to get the SHA256"
    echo ""
    exit 0
}

echo "✅ Downloaded GitHub release tarball"
echo ""
echo "🔐 Calculating SHA256 from GitHub release..."
GITHUB_SHA256=$(shasum -a 256 "/tmp/devsweep-github-${VERSION}.tar.gz" | awk '{print $1}')
echo "GitHub SHA256: ${GITHUB_SHA256}"

echo ""
echo "📝 Updating devsweep.rb with GitHub release URL and SHA256..."

# Update the formula
sed -i.bak \
    -e "s|url \".*\"|url \"${RELEASE_URL}\"|" \
    -e "s|sha256 \".*\"|sha256 \"${GITHUB_SHA256}\"|" \
    devsweep.rb

rm devsweep.rb.bak

echo "✅ Formula updated"
echo ""
echo "🧪 Testing formula..."
if brew install --build-from-source ./devsweep.rb; then
    echo "✅ Installation successful"
    
    echo ""
    echo "🔬 Running formula tests..."
    if brew test devsweep; then
        echo "✅ Tests passed"
    else
        echo "❌ Tests failed"
        brew uninstall devsweep
        exit 1
    fi
    
    echo ""
    echo "🔍 Running audit..."
    if brew audit --strict --online ./devsweep.rb; then
        echo "✅ Audit passed"
    else
        echo "⚠️  Audit found issues. Fix them before submitting."
        brew uninstall devsweep
        exit 1
    fi
    
    brew uninstall devsweep
else
    echo "❌ Installation failed"
    exit 1
fi

echo ""
echo "✅ Formula is ready for Homebrew Core!"
echo ""
echo "📋 Next steps:"
echo ""
echo "1. Review the formula:"
echo "   cat devsweep.rb"
echo ""
echo "2. Fork Homebrew Core:"
echo "   https://github.com/Homebrew/homebrew-core/fork"
echo ""
echo "3. Clone and prepare:"
echo "   git clone https://github.com/YOUR_USERNAME/homebrew-core.git"
echo "   cd homebrew-core"
echo "   git checkout -b devsweep"
echo "   cp ../dev_sweep/devsweep.rb Formula/devsweep.rb"
echo ""
echo "4. Commit and push:"
echo "   git add Formula/devsweep.rb"
echo "   git commit -m 'devsweep ${VERSION} (new formula)'"
echo "   git push origin devsweep"
echo ""
echo "5. Create PR at:"
echo "   https://github.com/Homebrew/homebrew-core/compare"
echo ""
echo "📚 Full guide: HOMEBREW_CORE_SUBMISSION.md"
