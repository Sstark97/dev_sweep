#!/usr/bin/env bash
# Create a release tarball for distribution

set -e

VERSION="${1:-1.0.0}"
RELEASE_NAME="devsweep-${VERSION}"
RELEASE_DIR="dist/${RELEASE_NAME}"

echo "Creating release ${VERSION}..."

# Clean previous builds
rm -rf dist
mkdir -p "${RELEASE_DIR}"

# Copy files to release directory
echo "Copying files..."
cp -r bin src "${RELEASE_DIR}/"
cp LICENSE README.md CONTRIBUTING.md QUICKSTART.md "${RELEASE_DIR}/"
cp Makefile "${RELEASE_DIR}/"

# Create tarball
echo "Creating tarball..."
cd dist
tar -czf "${RELEASE_NAME}.tar.gz" "${RELEASE_NAME}"
cd ..

# Calculate SHA256
echo ""
echo "Release created: dist/${RELEASE_NAME}.tar.gz"
echo "SHA256:"
shasum -a 256 "dist/${RELEASE_NAME}.tar.gz" | awk '{print $1}'

echo ""
echo "Next steps:"
echo "1. Create a GitHub release with tag v${VERSION}"
echo "2. Upload dist/${RELEASE_NAME}.tar.gz as a release asset"
echo "3. Update devsweep.rb with the release URL and SHA256"
echo "4. Test with: brew install --build-from-source ./devsweep.rb"
