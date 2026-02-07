#!/usr/bin/env bash
# cleanup_test.sh - End-to-end tests for cleanup functionality
# Tests real file creation and deletion

set -euo pipefail

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Import modules
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/jetbrains.sh"
source "$PROJECT_ROOT/src/modules/devtools.sh"

# Test fixtures path
TEST_FIXTURES_PATH="${HOME}/.devsweep_test_fixtures"

# ============================================================
# SETUP & TEARDOWN
# ============================================================

function setup_test_fixtures() {
    # Create test fixtures directory
    mkdir -p "$TEST_FIXTURES_PATH"
}

function teardown_test_fixtures() {
    # Clean up test fixtures
    if [[ -d "$TEST_FIXTURES_PATH" ]]; then
        rm -rf "$TEST_FIXTURES_PATH"
    fi
}

# ============================================================
# HELPER FUNCTIONS
# ============================================================

function create_test_file() {
    local path="$1"
    local size_kb="${2:-10}"  # Default 10KB (optimized for faster tests)

    mkdir -p "$(dirname "$path")"
    dd if=/dev/zero of="$path" bs=1024 count="$size_kb" 2>/dev/null
}

function file_exists() {
    local path="$1"
    [[ -f "$path" ]]
}

function dir_exists() {
    local path="$1"
    [[ -d "$path" ]]
}

function dir_is_empty() {
    local path="$1"
    [[ -d "$path" ]] && [[ -z "$(ls -A "$path")" ]]
}

# ============================================================
# JETBRAINS TESTS
# ============================================================

function test_jetbrains_clears_cache() {
    # Setup: Create test cache directory
    local test_cache="$TEST_FIXTURES_PATH/JetBrains"
    mkdir -p "$test_cache/test_ide_cache"
    create_test_file "$test_cache/test_ide_cache/index.dat" 50
    
    # Override JetBrains cache path for test
    JB_CACHE_PATH="$test_cache"
    
    # Execute: Clear cache using safe_rm
    DRY_RUN=false
    safe_rm "$test_cache" "Test JetBrains cache"
    
    # Assert: Cache should be deleted
    if [[ -d "$test_cache" ]]; then
        assert_fail "Cache was not deleted"
    fi
}

function test_jetbrains_dry_run_preserves_cache() {
    # Setup: Create test cache
    local test_cache="$TEST_FIXTURES_PATH/JetBrains_dry"
    mkdir -p "$test_cache/test_ide"
    create_test_file "$test_cache/test_ide/data.bin" 30
    
    # Override path
    JB_CACHE_PATH="$test_cache"
    
    # Execute: Dry-run should not delete
    DRY_RUN=true
    safe_rm "$test_cache" "Test JetBrains dry-run"
    
    # Assert: Cache should still exist
    if [[ ! -d "$test_cache" ]]; then
        assert_fail "Cache was deleted in dry-run mode"
    fi
}

# ============================================================
# DEVTOOLS - MAVEN TESTS
# ============================================================

function test_maven_cleanup_removes_repository() {
    # Setup: Create test Maven repo
    local test_maven="$TEST_FIXTURES_PATH/.m2/repository"
    mkdir -p "$test_maven/com/example/lib"
    create_test_file "$test_maven/com/example/lib/artifact.jar" 100
    
    # Execute: Clear Maven cache
    DRY_RUN=false
    safe_rm "$test_maven" "Test Maven repository"
    
    # Assert: Repository should be deleted
    if [[ -d "$test_maven" ]]; then
        assert_fail "Maven repository was not deleted"
    fi
}

# ============================================================# ============================================================
# DEVTOOLS - NODE.JS TESTS
# ============================================================

function test_npm_cache_cleanup() {
    # Setup: Create test npm cache
    local test_npm="$TEST_FIXTURES_PATH/.npm/_cacache"
    mkdir -p "$test_npm/content-v2"
    create_test_file "$test_npm/content-v2/package-123.tgz" 80
    
    # Execute: Clear npm cache
    DRY_RUN=false
    safe_rm "$test_npm" "Test npm cache"
    
    # Assert: Cache should be deleted
    if [[ -d "$test_npm" ]]; then
        assert_fail "npm cache was not deleted"
    fi
}

# ============================================================
# DEVTOOLS - PYTHON TESTS
# ============================================================

function test_pip_cache_cleanup() {
    # Setup: Create test pip cache
    local test_pip="$TEST_FIXTURES_PATH/pip_cache"
    mkdir -p "$test_pip/wheels"
    create_test_file "$test_pip/wheels/package.whl" 50
    
    # Execute: Clear pip cache
    DRY_RUN=false
    safe_rm "$test_pip" "Test pip cache"
    
    # Assert: Cache should be deleted
    if [[ -d "$test_pip" ]]; then
        assert_fail "pip cache was not deleted"
    fi
}

# ============================================================
# SPACE TRACKING TESTS
# ============================================================

function test_space_tracking_accumulates() {
    # Setup: Create multiple test files
    local test_dir="$TEST_FIXTURES_PATH/tracking_test"
    mkdir -p "$test_dir"
    create_test_file "$test_dir/file1.bin" 100  # 100KB
    create_test_file "$test_dir/file2.bin" 200  # 200KB
    
    # Reset space counter
    TOTAL_SPACE_FREED_KB=0
    
    # Execute: Delete files individually to track space
    DRY_RUN=false
    safe_rm "$test_dir/file1.bin" "Test file 1"
    safe_rm "$test_dir/file2.bin" "Test file 2"
    
    # Assert: Should have tracked approximately 300KB
    # Allow 10% margin for filesystem overhead
    local expected_kb=300
    local min_kb=$((expected_kb * 90 / 100))
    local max_kb=$((expected_kb * 110 / 100))
    
    assert_greater_than "$min_kb" "$TOTAL_SPACE_FREED_KB"
    assert_less_than "$max_kb" "$TOTAL_SPACE_FREED_KB"
}

function test_space_tracking_dry_run_no_accumulation() {
    # Setup: Create test file
    local test_file="$TEST_FIXTURES_PATH/dry_run_tracking.bin"
    create_test_file "$test_file" 150
    
    # Reset space counter
    TOTAL_SPACE_FREED_KB=0
    
    # Execute: Dry-run should not track space
    DRY_RUN=true
    safe_rm "$test_file" "Test dry-run tracking"
    
    # Assert: No space should be tracked
    assert_equals 0 "$TOTAL_SPACE_FREED_KB"
}

# ============================================================
# INTEGRATION TESTS
# ============================================================

function test_complete_cleanup_workflow() {
    # Setup: Create realistic test structure
    local test_root="$TEST_FIXTURES_PATH/complete_workflow"
    
    # JetBrains cache
    mkdir -p "$test_root/JetBrains/IntelliJIdea2023.1/caches"
    create_test_file "$test_root/JetBrains/IntelliJIdea2023.1/caches/index.dat" 500
    
    # Maven repo
    mkdir -p "$test_root/.m2/repository/org/example"
    create_test_file "$test_root/.m2/repository/org/example/lib.jar" 300
    
    # npm cache
    mkdir -p "$test_root/.npm/_cacache"
    create_test_file "$test_root/.npm/_cacache/pkg.tgz" 200
    
    # Reset counter
    TOTAL_SPACE_FREED_KB=0
    
    # Execute: Clean everything
    DRY_RUN=false
    safe_rm "$test_root/JetBrains" "JetBrains test"
    safe_rm "$test_root/.m2" "Maven test"
    safe_rm "$test_root/.npm" "npm test"
    
    # Assert: All should be deleted
    if [[ -d "$test_root/JetBrains" ]]; then
        assert_fail "JetBrains directory was not deleted"
    fi
    if [[ -d "$test_root/.m2" ]]; then
        assert_fail "Maven directory was not deleted"
    fi
    if [[ -d "$test_root/.npm" ]]; then
        assert_fail "npm directory was not deleted"
    fi
    
    # Assert: Space should be tracked (approximately 1000KB)
    local expected_kb=1000
    local min_kb=$((expected_kb * 80 / 100))  # 20% margin for overhead
    
    assert_greater_than "$min_kb" "$TOTAL_SPACE_FREED_KB"
}

# ============================================================
# RUN TESTS
# ============================================================

# Setup before all tests
setup_test_fixtures

# Teardown after all tests (even on failure)
trap teardown_test_fixtures EXIT

# Note: bashunit will automatically discover and run all test_* functions
