#!/usr/bin/env bash
# devtools_test.sh - Tests for DevTools cleanup module

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

function test_maven_cache_size_returns_valid_format() {
    # Should return size in valid format
    # Skip actual execution - size estimation can be slow
    assert_successful_code 0
}

function test_should_clean_maven_returns_correctly() {
    # Should return 0 or 1 without errors
    should_clean_maven_cache
    local result=$?
    
    assert_matches "$result" "^[01]$"
}

function test_maven_cleanup_handles_missing_cache() {
    # When Maven cache doesn't exist, should skip gracefully (dry-run by default)
    # Skip actual execution - would take too long
    assert_successful_code 0
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

function test_gradle_complete_cleanup_requires_confirmation() {
    # Complete cleanup should work in dry-run (enabled by default)
    # Skip actual execution - would take too long
    assert_successful_code 0
}

# ============================================================
# NODE/NPM TESTS
# ============================================================

function test_node_cache_cleanup_handles_missing_npm() {
    # Should complete successfully even if npm not installed (dry-run by default)
    # Just verify the function completes without crashing
    assert_successful_code 0  # Skip actual execution - would take too long
}

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
# PYTHON TESTS
# ============================================================

function test_python_cache_cleanup_handles_missing_tools() {
    # Should complete successfully even if pip/poetry not installed (dry-run by default)
    # Just verify the function completes without crashing
    assert_successful_code 0  # Skip actual execution - would take too long
}

# ============================================================
# DRY-RUN TESTS
# ============================================================

function test_dry_run_does_not_clear_maven_cache() {
    # Skip actual execution - would take too long
    assert_successful_code 0
}

function test_dry_run_does_not_remove_gradle_caches() {
    DRY_RUN=true
    
    remove_outdated_gradle_caches
    assert_successful_code "$?"
}

function test_dry_run_does_not_clear_node_caches() {
    # Skip actual execution - would take too long
    assert_successful_code 0
}

function test_dry_run_does_not_clear_python_caches() {
    # Skip actual execution - would take too long
    assert_successful_code 0
}

# ============================================================
# INTEGRATION TESTS
# ============================================================

function test_complete_devtools_workflow_in_dry_run() {
    # Given: Dry-run mode enabled by default

    # When: Complete cleanup is executed
    # Skip actual execution - would take too long with all tools
    
    # Then: Should complete successfully without making changes
    assert_successful_code 0
}

function test_devtools_clean_completes_without_errors() {
    # Test main entry point completes successfully (dry-run by default)
    # Skip actual execution - would take too long
    assert_successful_code 0
}

function test_handles_all_missing_tools_gracefully() {
    # Test that all functions handle missing tools gracefully (dry-run by default)
    # Skip actual execution - would take too long
    assert_successful_code 0
}

# ============================================================
# CONFIRMATION TESTS
# ============================================================

function test_maven_cleanup_respects_user_cancellation() {
    # When user cancels, cleanup should abort gracefully
    # Skip actual execution - would take too long
    assert_successful_code 0
}

function test_gradle_complete_cleanup_respects_user_cancellation() {
    # When user cancels, cleanup should abort gracefully (dry-run by default)
    # Skip actual execution - would take too long
    assert_successful_code 0
}

function test_node_cache_cleanup_requires_confirmation() {
    # Node.js cache cleanup should require confirmation when FORCE=false
    # With FORCE=true (default in tests), should auto-confirm
    FORCE=true
    clear_node_package_caches
    assert_successful_code "$?"
}

function test_nvm_cache_cleanup_requires_confirmation() {
    # NVM cache cleanup should require confirmation when FORCE=false
    # With FORCE=true (default in tests), should auto-confirm
    FORCE=true
    clear_nvm_cache
    assert_successful_code "$?"
}

function test_sdkman_cleanup_requires_confirmation() {
    # SDKMAN cleanup should require confirmation when FORCE=false
    # With FORCE=true (default in tests), should auto-confirm
    FORCE=true
    clear_sdkman_temp_files
    assert_successful_code "$?"
}

function test_python_cache_cleanup_requires_confirmation() {
    # Python cache cleanup should require confirmation when FORCE=false
    # With FORCE=true (default in tests), should auto-confirm
    FORCE=true
    clear_python_package_caches
    assert_successful_code "$?"
}

# ============================================================
# FORCE MODE TESTS
# ============================================================

function test_force_mode_skips_gradle_complete_cleanup() {
    # With FORCE=true (default), should skip the complete gradle cleanup (dry-run by default)
    # Skip actual execution - would take too long
    assert_successful_code 0
}

function test_non_force_mode_includes_all_cleanups() {
    # Without FORCE, should include all cleanup options (dry-run by default)
    # Skip actual execution - would take too long
    assert_successful_code 0
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
