#!/usr/bin/env bash
# setup-demo.sh - Create realistic test files for VHS demo
# Creates files in EXACT locations that DevSweep cleans

set -euo pipefail

BLUE='\033[0;34m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}Creating demo files in correct DevSweep locations...${NC}"
echo ""

# ============================================================
# JETBRAINS IDEs (~3.5GB) - Old versions that will be deleted
# ============================================================
echo -e "${YELLOW}[1/6] Creating JetBrains IDE files (~3.5GB)...${NC}"

JB_PATH="$HOME/Library/Application Support/JetBrains"
mkdir -p "$JB_PATH"

# Old IntelliJ versions (DevSweep keeps latest, deletes old)
mkdir -p "$JB_PATH/IntelliJIdea2022.1/caches"
mkdir -p "$JB_PATH/IntelliJIdea2022.3/caches"
mkdir -p "$JB_PATH/IntelliJIdea2023.1/caches"
dd if=/dev/zero of="$JB_PATH/IntelliJIdea2022.1/caches/index.dat" bs=1048576 count=500 2>/dev/null
dd if=/dev/zero of="$JB_PATH/IntelliJIdea2022.3/caches/symbols.dat" bs=1048576 count=600 2>/dev/null
dd if=/dev/zero of="$JB_PATH/IntelliJIdea2023.1/caches/compiled.dat" bs=1048576 count=700 2>/dev/null

# Old PyCharm
mkdir -p "$JB_PATH/PyCharm2022.2/caches"
mkdir -p "$JB_PATH/PyCharm2023.1/caches"
dd if=/dev/zero of="$JB_PATH/PyCharm2022.2/caches/index.dat" bs=1048576 count=400 2>/dev/null
dd if=/dev/zero of="$JB_PATH/PyCharm2023.1/caches/libs.dat" bs=1048576 count=500 2>/dev/null

# Old WebStorm
mkdir -p "$JB_PATH/WebStorm2022.3/caches"
dd if=/dev/zero of="$JB_PATH/WebStorm2022.3/caches/node_modules.dat" bs=1048576 count=300 2>/dev/null

echo -e "${GREEN}✓ JetBrains files created (~3.5GB)${NC}"

# ============================================================
# ORBSTACK CACHE (~5GB) - Exact location DevSweep cleans
# ============================================================
echo -e "${YELLOW}[2/6] Creating OrbStack cache (~5GB)...${NC}"

ORBSTACK_CACHE="$HOME/.orbstack/cache"
mkdir -p "$ORBSTACK_CACHE/docker"
mkdir -p "$ORBSTACK_CACHE/images"
mkdir -p "$ORBSTACK_CACHE/containers"

# Create cache files
dd if=/dev/zero of="$ORBSTACK_CACHE/docker/layers.dat" bs=1048576 count=2000 2>/dev/null
dd if=/dev/zero of="$ORBSTACK_CACHE/images/cache.dat" bs=1048576 count=1500 2>/dev/null
dd if=/dev/zero of="$ORBSTACK_CACHE/containers/data.dat" bs=1048576 count=1500 2>/dev/null

echo -e "${GREEN}✓ OrbStack cache created (~5GB)${NC}"

# ============================================================
# HOMEBREW (~2GB)
# ============================================================
echo -e "${YELLOW}[3/6] Creating Homebrew cache (~2GB)...${NC}"

BREW_CACHE="$HOME/Library/Caches/Homebrew"
mkdir -p "$BREW_CACHE/downloads"

# Old package versions
dd if=/dev/zero of="$BREW_CACHE/downloads/node-18.0.0.tar.gz" bs=1048576 count=200 2>/dev/null
dd if=/dev/zero of="$BREW_CACHE/downloads/python-3.10.0.tar.gz" bs=1048576 count=300 2>/dev/null
dd if=/dev/zero of="$BREW_CACHE/downloads/postgresql-14.0.tar.gz" bs=1048576 count=400 2>/dev/null
dd if=/dev/zero of="$BREW_CACHE/downloads/rust-1.70.0.tar.gz" bs=1048576 count=500 2>/dev/null
dd if=/dev/zero of="$BREW_CACHE/downloads/gcc-12.0.tar.gz" bs=1048576 count=600 2>/dev/null

echo -e "${GREEN}✓ Homebrew cache created (~2GB)${NC}"

# ============================================================
# MAVEN (~4GB)
# ============================================================
echo -e "${YELLOW}[4/6] Creating Maven repository (~4GB)...${NC}"

MAVEN_REPO="$HOME/.m2/repository"
mkdir -p "$MAVEN_REPO/org/springframework"
mkdir -p "$MAVEN_REPO/com/google/guava"
mkdir -p "$MAVEN_REPO/org/apache/commons"

dd if=/dev/zero of="$MAVEN_REPO/org/springframework/spring-all-5.3.0.jar" bs=1048576 count=1000 2>/dev/null
dd if=/dev/zero of="$MAVEN_REPO/com/google/guava/guava-31.0.jar" bs=1048576 count=800 2>/dev/null
dd if=/dev/zero of="$MAVEN_REPO/org/apache/commons/commons-lang3-3.12.jar" bs=1048576 count=600 2>/dev/null

# Additional artifacts
for i in {1..10}; do
    mkdir -p "$MAVEN_REPO/com/example/lib$i"
    dd if=/dev/zero of="$MAVEN_REPO/com/example/lib$i/artifact-1.0.0.jar" bs=1048576 count=160 2>/dev/null
done

echo -e "${GREEN}✓ Maven repository created (~4GB)${NC}"

# ============================================================
# GRADLE (~1.5GB)
# ============================================================
echo -e "${YELLOW}[5/6] Creating Gradle cache (~1.5GB)...${NC}"

GRADLE_CACHE="$HOME/.gradle/caches"
mkdir -p "$GRADLE_CACHE/modules-2/files-2.1"
mkdir -p "$GRADLE_CACHE/build-cache-1"

dd if=/dev/zero of="$GRADLE_CACHE/modules-2/files-2.1/dependencies.dat" bs=1048576 count=700 2>/dev/null
dd if=/dev/zero of="$GRADLE_CACHE/build-cache-1/cache.bin" bs=1048576 count=800 2>/dev/null

echo -e "${GREEN}✓ Gradle cache created (~1.5GB)${NC}"

# ============================================================
# NODE.JS + PYTHON (~2GB)
# ============================================================
echo -e "${YELLOW}[6/6] Creating Node.js and Python caches (~2GB)...${NC}"

# npm cache
NPM_CACHE="$HOME/.npm/_cacache"
mkdir -p "$NPM_CACHE/content-v2"
mkdir -p "$NPM_CACHE/index-v5"
dd if=/dev/zero of="$NPM_CACHE/content-v2/packages.dat" bs=1048576 count=500 2>/dev/null
dd if=/dev/zero of="$NPM_CACHE/index-v5/index.dat" bs=1048576 count=200 2>/dev/null

# yarn cache
YARN_CACHE="$HOME/Library/Caches/Yarn/v6"
mkdir -p "$YARN_CACHE"
dd if=/dev/zero of="$YARN_CACHE/packages.dat" bs=1048576 count=400 2>/dev/null

# pnpm cache
PNPM_CACHE="$HOME/Library/pnpm/store/v3"
mkdir -p "$PNPM_CACHE/files"
dd if=/dev/zero of="$PNPM_CACHE/files/packages.dat" bs=1048576 count=400 2>/dev/null

# pip cache
PIP_CACHE="$HOME/Library/Caches/pip"
mkdir -p "$PIP_CACHE/wheels"
mkdir -p "$PIP_CACHE/http"
dd if=/dev/zero of="$PIP_CACHE/wheels/numpy-1.24.0.whl" bs=1048576 count=100 2>/dev/null
dd if=/dev/zero of="$PIP_CACHE/wheels/pandas-1.5.0.whl" bs=1048576 count=150 2>/dev/null
dd if=/dev/zero of="$PIP_CACHE/wheels/tensorflow-2.12.0.whl" bs=1048576 count=250 2>/dev/null

echo -e "${GREEN}✓ Node.js and Python caches created (~2GB)${NC}"

# ============================================================
# SUMMARY
# ============================================================
echo ""
echo -e "${GREEN}═══════════════════════════════════════${NC}"
echo -e "${GREEN}  Demo files created successfully!${NC}"
echo -e "${GREEN}═══════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}Breakdown (locations DevSweep actually cleans):${NC}"
echo "  JetBrains:  ~3.5 GB (old IDE versions)"
echo "  OrbStack:   ~5.0 GB (~/.orbstack/cache)"
echo "  Homebrew:   ~2.0 GB (download cache)"
echo "  Maven:      ~4.0 GB (repository)"
echo "  Gradle:     ~1.5 GB (caches)"
echo "  Node.js:    ~1.5 GB (npm/yarn/pnpm)"
echo "  Python:     ~0.5 GB (pip wheels)"
echo "  ─────────────────────"
echo "  Total:      ~18.0 GB (DevSweep detects ~14GB cleanable)"
echo ""
echo -e "${YELLOW}Now run: devsweep --dry-run --all${NC}"
echo -e "${YELLOW}Then cleanup with: devsweep --all${NC}"
echo ""
