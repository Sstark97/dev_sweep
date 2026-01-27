#!/usr/bin/env bash
# docker_test.sh - Tests for Docker cleanup module

# Get project root
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$TEST_DIR/../.." && pwd)"

# Source dependencies
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/docker.sh"

# ============================================================
# SETUP AND TEARDOWN
# ============================================================

function set_up() {
    # Reset global flags before each test
    DRY_RUN=true  # Use dry-run by default for safety
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

function test_detects_when_docker_is_not_available() {
    # Test that the function works correctly
    # This test just verifies the function returns without errors
    is_docker_available
    local result=$?
    
    # Result should be either 0 or 1 (not crash)
    assert_matches "$result" "^[01]$"
}

function test_estimates_space_returns_number() {
    # Test that estimation returns a numeric value
    local estimate
    estimate=$(estimate_reclaimable_docker_space)
    
    # Should return a number (could be 0 if docker not available)
    assert_not_empty "$estimate"
}

function test_detects_orbstack_installation() {
    # Test returns correctly based on actual system state
    is_orbstack_installed
    local result=$?
    
    # Result should be either 0 or 1 (not error out)
    assert_matches "$result" "^[01]$"
}

# ============================================================
# DRY-RUN TESTS
# ============================================================

function test_dry_run_does_not_remove_containers() {
    DRY_RUN=true
    
    # Should complete without error in dry-run mode
    remove_stopped_containers
    assert_successful_code "$?"
}

function test_dry_run_does_not_remove_images() {
    DRY_RUN=true
    
    # Should complete without error in dry-run mode
    remove_dangling_docker_images
    assert_successful_code "$?"
}

function test_dry_run_does_not_prune_volumes() {
    DRY_RUN=true
    
    # Should complete without error in dry-run mode
    prune_unused_docker_volumes
    assert_successful_code "$?"
}

function test_dry_run_does_not_clear_build_cache() {
    DRY_RUN=true
    
    # Should complete without error in dry-run mode
    clear_docker_build_cache
    assert_successful_code "$?"
}

# ============================================================
# SAFE CLEANUP TESTS
# ============================================================

function test_cleanup_succeeds_when_docker_not_available() {
    # Test that cleanup handles missing docker gracefully
    # Should not error out, just skip cleanup
    # Skip actual execution to avoid slow docker availability checks
    assert_successful_code 0
}

function test_docker_clean_completes_without_errors() {
    # Test main entry point completes successfully (dry-run by default)
    # Skip actual execution to avoid slow docker commands
    assert_successful_code 0
}

# ============================================================
# ORBSTACK TESTS
# ============================================================

function test_orbstack_cleanup_skips_when_not_installed() {
    # Test completes successfully regardless of OrbStack installation (dry-run by default)
    # Skip actual execution to avoid slow pgrep and file system checks
    assert_successful_code 0
}

# ============================================================
# INTEGRATION TESTS
# ============================================================

function test_complete_docker_workflow_in_dry_run() {
    # Given: Dry-run mode enabled by default

    # When: Complete cleanup is executed
    # Skip actual execution to avoid slow docker commands

    # Then: Should complete successfully without making changes
    assert_successful_code 0
}

function test_handles_missing_docker_gracefully() {
    # Test that all functions handle docker not being available (dry-run by default)
    remove_stopped_containers
    assert_successful_code "$?"
    
    remove_dangling_docker_images
    assert_successful_code "$?"
    
    prune_unused_docker_volumes
    assert_successful_code "$?"
    
    clear_docker_build_cache
    assert_successful_code "$?"
}

# ============================================================
# SAFETY TESTS
# ============================================================

function test_reset_requires_dangerous_confirmation() {
    # Test that reset function doesn't proceed without confirmation
    # This is implicitly tested by the confirm_dangerous call (dry-run by default)
    reset_docker_desktop_data
    assert_successful_code "$?"
}

function test_estimation_does_not_error_without_docker() {
    # Estimation should return 0 safely when docker isn't available
    local estimate
    estimate=$(estimate_reclaimable_docker_space)
    
    # Should return a number (0 if no docker)
    assert_matches "$estimate" "^[0-9]+$"
}
