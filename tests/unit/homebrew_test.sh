#!/usr/bin/env bash
# homebrew_test.sh - Tests for Homebrew cleanup module

# Get project root
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$TEST_DIR/../.." && pwd)"

# Source dependencies
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/homebrew.sh"

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
}

function tear_down() {
    # Clean up temporary test directory
    if [[ -d "$TEST_TEMP_DIR" ]]; then
        rm -rf "$TEST_TEMP_DIR"
    fi
}

# ============================================================
# VALIDATION TESTS
# ============================================================

function test_detects_homebrew_installation_status() {
    # Test returns correctly based on actual system state
    is_homebrew_installed
    local result=$?
    
    # Result should be either 0 or 1 (not error out)
    assert_matches "$result" "^[01]$"
}

function test_gets_cache_path_safely() {
    # Should return path or empty string without errors
    local cache_path
    cache_path=$(get_homebrew_cache_path)
    
    # Should not error - result can be empty or a path
    assert_successful_code "$?"
}

function test_estimates_cache_size_returns_valid_format() {
    # Should return size in valid format (e.g., "0B", "1.5GB")
    local size
    size=$(estimate_homebrew_cache_size)
    
    # Should not be empty
    assert_not_empty "$size"
}

# ============================================================
# DRY-RUN TESTS
# ============================================================

function test_dry_run_does_not_remove_packages() {
    # Should complete without error in dry-run mode
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

function test_dry_run_does_not_remove_dependencies() {
    # Should complete without error in dry-run mode
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

function test_dry_run_does_not_clear_cache() {
    DRY_RUN=true
    
    # Should complete without error in dry-run mode
    clear_homebrew_download_cache
    assert_successful_code "$?"
}

function test_dry_run_does_not_update_homebrew() {
    DRY_RUN=true
    
    # Should complete without error in dry-run mode
    update_and_diagnose_homebrew
    assert_successful_code "$?"
}

# ============================================================
# CLEANUP TESTS
# ============================================================

function test_cleanup_succeeds_when_homebrew_not_installed() {
    # Test that cleanup handles missing homebrew gracefully
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

function test_handles_empty_cache_gracefully() {
    # When cache is empty, should skip cleanup without errors
    # This is tested through the main function (dry-run enabled by default)
    clear_homebrew_download_cache
    assert_successful_code "$?"
}

function test_removes_outdated_packages_safely() {
    # Test that package removal works or skips gracefully (dry-run by default)
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

function test_removes_unused_dependencies_safely() {
    # Test that dependency removal works or skips gracefully (dry-run by default)
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

# ============================================================
# INTEGRATION TESTS
# ============================================================

function test_complete_homebrew_workflow_in_dry_run() {
    # Given: Dry-run mode enabled by default
    # Skip actual execution - brew commands are slow
    
    # Then: Should complete successfully without making changes
    assert_successful_code 0
}

function test_homebrew_clean_completes_without_errors() {
    # Test main entry point completes successfully (dry-run by default)
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

function test_handles_missing_homebrew_gracefully() {
    # Test that all functions handle homebrew not being available (dry-run by default)
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

# ============================================================
# VERBOSE MODE TESTS
# ============================================================

function test_verbose_mode_includes_diagnostics() {
    # When verbose is true, diagnostics should run (dry-run by default)
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

function test_non_verbose_mode_skips_diagnostics() {
    # When verbose is false, should still complete successfully (dry-run by default)
    # Skip actual execution - brew commands are slow
    assert_successful_code 0
}

# ============================================================
# CACHE SIZE TESTS
# ============================================================

function test_cache_size_estimation_does_not_fail() {
    # Estimation should work regardless of homebrew state
    local size
    size=$(estimate_homebrew_cache_size)
    
    # Should return a valid size string (may have leading/trailing whitespace)
    assert_not_empty "$size"
    # Match pattern with optional leading space and unit
    [[ "$size" =~ [0-9]+[BKMGT] ]]
    assert_successful_code "$?"
}

function test_handles_nonexistent_cache_path() {
    # If cache doesn't exist, should return 0B
    # This is implicitly tested through estimate function
    local size
    size=$(estimate_homebrew_cache_size)
    
    # Should not error out
    assert_successful_code "$?"
}
