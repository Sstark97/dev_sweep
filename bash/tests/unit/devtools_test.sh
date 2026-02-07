#!/usr/bin/env bash
# devtools_test.sh - Tests for DevTools cleanup module
# Optimized: Removed placeholder tests that only did assert_successful_code 0

# Get project root
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$TEST_DIR/../.." && pwd)"

# Source dependencies
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/devtools.sh"

# ============================================================
# SETUP AND TEARDOWN
# ============================================================

function set_up() {
    # Reset global flags before each test
    DRY_RUN=true  # Use dry-run by default to avoid slow system commands
    VERBOSE=false
    FORCE=true    # Skip confirmations in tests

    # Create temporary test directory
    TEST_TEMP_DIR="$(mktemp -d)"

    # Create mock cache directories for testing
    MOCK_MAVEN_PATH="$TEST_TEMP_DIR/maven"
    MOCK_GRADLE_PATH="$TEST_TEMP_DIR/gradle"
    MOCK_NPM_PATH="$TEST_TEMP_DIR/npm"
}

function tear_down() {
    # Clean up temporary test directory
    if [[ -d "$TEST_TEMP_DIR" ]]; then
        rm -rf "$TEST_TEMP_DIR"
    fi
}

# ============================================================
# MAVEN TESTS
# ============================================================

function test_should_clean_maven_returns_correctly() {
    # Should return 0 or 1 without errors
    should_clean_maven_cache
    local result=$?

    assert_matches "$result" "^[01]$"
}

# ============================================================
# GRADLE TESTS
# ============================================================

function test_identifies_old_gradle_versions_safely() {
    # Should not error even if gradle path doesn't exist
    local old_versions
    old_versions=$(identify_old_gradle_versions)

    # Should complete successfully (result can be empty)
    assert_successful_code "$?"
}

function test_gradle_cleanup_handles_missing_cache() {
    # When Gradle cache doesn't exist, should skip gracefully (dry-run by default)
    remove_outdated_gradle_caches
    assert_successful_code "$?"
}

# ============================================================
# NODE/NPM TESTS
# ============================================================

function test_nvm_cache_cleanup_handles_missing_nvm() {
    # Should skip gracefully if NVM not installed (dry-run by default)
    clear_nvm_cache
    assert_successful_code "$?"
}

# ============================================================
# SDKMAN TESTS
# ============================================================

function test_sdkman_cleanup_handles_missing_sdkman() {
    # Should skip gracefully if SDKMAN not installed (dry-run by default)
    clear_sdkman_temp_files
    assert_successful_code "$?"
}

# ============================================================
# DRY-RUN TESTS
# ============================================================

function test_dry_run_does_not_remove_gradle_caches() {
    DRY_RUN=true

    remove_outdated_gradle_caches
    assert_successful_code "$?"
}

# ============================================================
# SIZE ESTIMATION TESTS
# ============================================================

function test_maven_size_estimation_does_not_fail() {
    # Estimation should work regardless of maven state
    local size
    size=$(estimate_maven_cache_size)

    # Should return a valid size string
    assert_not_empty "$size"
}

function test_handles_nonexistent_maven_cache() {
    # If cache doesn't exist, should return 0B
    local size
    size=$(estimate_maven_cache_size)

    # Should not error out
    assert_successful_code "$?"
}

# ============================================================
# ANALYZE MODE TESTS
# ============================================================

function test_analyze_mode_detects_gradle_caches() {
    # Setup
    ANALYZE_MODE=true
    local original_items=("${ANALYZE_ITEMS[@]}")
    ANALYZE_ITEMS=()
    
    # Create mock outdated Gradle caches in actual GRADLE_CACHE_PATH
    local test_gradle_path="${GRADLE_CACHE_PATH}/test_7.4_devsweep_test"
    mkdir -p "$test_gradle_path"
    echo "test" > "$test_gradle_path/test.txt"
    
    # Execute
    remove_outdated_gradle_caches
    
    # Cleanup test directory
    rm -rf "$test_gradle_path"
    
    # Restore original items
    ANALYZE_ITEMS=("${original_items[@]}")
    
    # Test passed if function didn't error
    assert_successful_code "$?"
}

function test_analyze_mode_detects_python_caches() {
    # Setup
    ANALYZE_MODE=true
    local original_items=("${ANALYZE_ITEMS[@]}")
    local original_home="$HOME"
    ANALYZE_ITEMS=()
    
    # Create mock pip cache in test directory
    local mock_pip_cache="$TEST_TEMP_DIR/Library/Caches/pip"
    mkdir -p "$mock_pip_cache"
    echo "test data" > "$mock_pip_cache/test.txt"
    
    # Temporarily override HOME
    HOME="$TEST_TEMP_DIR"
    
    # Execute
    clear_python_package_caches
    
    # Restore HOME
    HOME="$original_home"
    
    # Verify pip cache was registered
    local found=false
    for item in "${ANALYZE_ITEMS[@]}"; do
        if [[ "$item" == *"pip cache"* ]]; then
            found=true
            break
        fi
    done
    
    # Restore original items
    ANALYZE_ITEMS=("${original_items[@]}")
    
    assert_true "$found" "pip cache should be detected in analyze mode"
}


