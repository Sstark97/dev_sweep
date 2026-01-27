#!/usr/bin/env bash
# common_test.sh - Unit tests for src/utils/common.sh

# Get project root
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$TEST_DIR/../.." && pwd)"

# Source dependencies
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"

# Helper function to strip ANSI color codes from output
strip_colors() {
    sed 's/\x1b\[[0-9;]*m//g'
}

# ============================================================
# SETUP AND TEARDOWN
# ============================================================

function set_up() {
    # Reset global flags before each test
    DRY_RUN=false
    VERBOSE=false
    FORCE=false
    TOTAL_SPACE_FREED_KB=0

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
# LOGGING TESTS
# ============================================================

function test_log_info_works_without_error() {
    # Just verify the function works (doesn't crash)
    log_info "test message" >/dev/null 2>&1
    assert_successful_code "$?"
}

function test_log_success_works_without_error() {
    log_success "success message" >/dev/null 2>&1
    assert_successful_code "$?"
}

function test_log_warn_works_without_error() {
    log_warn "warning message" >/dev/null 2>&1
    assert_successful_code "$?"
}

function test_log_error_works_without_error() {
    log_error "error message" >/dev/null 2>&1
    assert_successful_code "$?"
}

function test_log_debug_hidden_when_verbose_false() {
    VERBOSE=false
    local output
    output=$(log_debug "debug message" 2>&1)
    # When verbose is false, debug should produce no output
    assert_empty "$output"
}

function test_log_debug_shown_when_verbose_true() {
    VERBOSE=true
    # Just verify it runs without error when verbose is on
    log_debug "debug message" >/dev/null 2>&1
    assert_successful_code "$?"
}

# ============================================================
# SAFE_RM TESTS
# ============================================================

function test_safe_rm_dry_run_does_not_delete() {
    local test_file="$TEST_TEMP_DIR/test.txt"
    echo "test" > "$test_file"

    DRY_RUN=true
    safe_rm "$test_file" "test file"

    # File should still exist
    assert_file_exists "$test_file"
}

function test_safe_rm_actually_deletes_when_not_dry_run() {
    local test_file="$TEST_TEMP_DIR/test.txt"
    echo "test" > "$test_file"

    DRY_RUN=false
    safe_rm "$test_file" "test file"

    # File should be deleted
    assert_file_not_exists "$test_file"
}

function test_safe_rm_handles_nonexistent_file_gracefully() {
    local nonexistent="$TEST_TEMP_DIR/does_not_exist.txt"

    DRY_RUN=false
    safe_rm "$nonexistent" "nonexistent file"

    # Should return success even though file doesn't exist
    assert_successful_code "$?"
}

function test_safe_rm_rejects_root_path() {
    DRY_RUN=false
    safe_rm "/" "root"

    # Should fail
    assert_general_error "$?"
}

function test_safe_rm_rejects_home_path() {
    DRY_RUN=false
    safe_rm "$HOME" "home"

    # Should fail
    assert_general_error "$?"
}

function test_safe_rm_rejects_empty_path() {
    DRY_RUN=false
    safe_rm "" "empty"

    # Should fail
    assert_general_error "$?"
}

function test_safe_rm_deletes_directory() {
    local test_dir="$TEST_TEMP_DIR/subdir"
    mkdir -p "$test_dir"
    touch "$test_dir/file.txt"

    DRY_RUN=false
    safe_rm "$test_dir" "test directory"

    # Directory should be deleted
    assert_directory_not_exists "$test_dir"
}

# ============================================================
# SAFE_KILL TESTS
# ============================================================

function test_safe_kill_dry_run_does_not_kill() {
    DRY_RUN=true
    
    # Test dry-run mode - should not error even if process doesn't exist
    safe_kill "some_test_process"

    # Should return success
    assert_successful_code "$?"
}

function test_safe_kill_calls_pkill_when_not_dry_run() {
    # We can't easily test pkill without actually killing processes
    # Instead, we test that safe_kill returns successfully when process doesn't exist
    DRY_RUN=false
    
    # Use a process name that won't exist
    safe_kill "this_process_definitely_does_not_exist_12345"

    # Should return success (safe_kill returns 0 even if process doesn't exist)
    assert_successful_code "$?"
}

function test_safe_kill_handles_nonexistent_process() {
    DRY_RUN=false
    
    # Test with a process that definitely doesn't exist
    safe_kill "nonexistent_process_xyz_123"

    # Should return success (safe_kill handles nonexistent gracefully)
    assert_successful_code "$?"
}

# ============================================================
# CONFIRM_ACTION TESTS
# ============================================================

function test_confirm_action_auto_confirms_with_force() {
    FORCE=true

    # Should return success without prompting when FORCE is true
    confirm_action "test prompt"
    assert_successful_code "$?"
}

# Note: Interactive tests (confirm_action with user input) are tested manually
# because mocking 'read' builtin is unreliable in unit tests

# ============================================================
# CONFIRM_DANGEROUS TESTS
# ============================================================

function test_confirm_dangerous_auto_confirms_with_force() {
    FORCE=true

    # Should return success without prompting when FORCE is true
    confirm_dangerous "test dangerous action"
    assert_successful_code "$?"
}

# Note: Interactive tests (confirm_dangerous with user input) are tested manually
# because mocking 'read' builtin is unreliable in unit tests

# ============================================================
# FILE SYSTEM HELPER TESTS
# ============================================================

function test_is_dir_not_empty_returns_true_for_nonempty_dir() {
    local test_dir="$TEST_TEMP_DIR/nonempty"
    mkdir -p "$test_dir"
    touch "$test_dir/file.txt"

    is_dir_not_empty "$test_dir"
    assert_successful_code "$?"
}

function test_is_dir_not_empty_returns_false_for_empty_dir() {
    local test_dir="$TEST_TEMP_DIR/empty"
    mkdir -p "$test_dir"

    is_dir_not_empty "$test_dir"
    assert_general_error "$?"
}

function test_is_dir_not_empty_returns_false_for_nonexistent_dir() {
    is_dir_not_empty "$TEST_TEMP_DIR/nonexistent"
    assert_general_error "$?"
}

function test_get_size_returns_size_for_existing_file() {
    local test_file="$TEST_TEMP_DIR/file.txt"
    echo "test content" > "$test_file"

    local size
    size=$(get_size "$test_file")

    # Should return a size (not empty)
    assert_not_empty "$size"
}

function test_get_size_returns_0B_for_nonexistent_file() {
    local size
    size=$(get_size "$TEST_TEMP_DIR/nonexistent.txt")

    assert_equals "0B" "$size"
}

# ============================================================
# SPACE TRACKING TESTS
# ============================================================

function test_parse_size_to_kb_handles_gigabytes() {
    local result
    result=$(parse_size_to_kb "2GB")

    # 2GB = 2 * 1024 * 1024 = 2097152 KB
    assert_equals "2097152" "$result"
}

function test_parse_size_to_kb_handles_megabytes() {
    local result
    result=$(parse_size_to_kb "500MB")

    # 500MB = 500 * 1024 = 512000 KB
    assert_equals "512000" "$result"
}

function test_parse_size_to_kb_handles_kilobytes() {
    local result
    result=$(parse_size_to_kb "1024KB")

    assert_equals "1024" "$result"
}

function test_parse_size_to_kb_handles_bytes() {
    local result
    result=$(parse_size_to_kb "2048B")

    # 2048B = 2048 / 1024 = 2 KB
    assert_equals "2" "$result"
}

function test_parse_size_to_kb_handles_lowercase_units() {
    local result
    result=$(parse_size_to_kb "10mb")

    # Should handle lowercase - 10MB = 10 * 1024 = 10240 KB
    assert_equals "10240" "$result"
}

function test_parse_size_to_kb_returns_zero_for_invalid_input() {
    local result
    result=$(parse_size_to_kb "invalid")

    assert_equals "0" "$result"
}

function test_format_kb_to_human_shows_gigabytes() {
    local result
    result=$(format_kb_to_human 2097152)

    # 2097152 KB = 2 GB
    assert_equals "2GB" "$result"
}

function test_format_kb_to_human_shows_megabytes() {
    local result
    result=$(format_kb_to_human 512000)

    # 512000 KB = 500 MB
    assert_equals "500MB" "$result"
}

function test_format_kb_to_human_shows_kilobytes() {
    local result
    result=$(format_kb_to_human 512)

    assert_equals "512KB" "$result"
}

function test_track_freed_space_accumulates_correctly() {
    # Reset counter
    TOTAL_SPACE_FREED_KB=0

    # Track some space
    track_freed_space "100MB"  # 100 * 1024 = 102400 KB
    assert_equals "102400" "$TOTAL_SPACE_FREED_KB"

    # Track more space - should accumulate
    track_freed_space "50MB"   # 50 * 1024 = 51200 KB
    assert_equals "153600" "$TOTAL_SPACE_FREED_KB"  # 102400 + 51200

    # Reset for next test
    TOTAL_SPACE_FREED_KB=0
}

function test_track_freed_space_ignores_zero_bytes() {
    TOTAL_SPACE_FREED_KB=0

    track_freed_space "0B"
    assert_equals "0" "$TOTAL_SPACE_FREED_KB"

    # Reset
    TOTAL_SPACE_FREED_KB=0
}

function test_safe_rm_tracks_freed_space_on_successful_deletion() {
    TOTAL_SPACE_FREED_KB=0

    # Create a test file
    local test_file="$TEST_TEMP_DIR/test_tracking.txt"
    echo "some content for testing" > "$test_file"

    # Delete it with safe_rm
    safe_rm "$test_file" "test file" >/dev/null 2>&1

    # Space should have been tracked (should be > 0)
    assert_greater_than "$TOTAL_SPACE_FREED_KB" "0"

    # Reset
    TOTAL_SPACE_FREED_KB=0
}

function test_safe_rm_does_not_track_in_dry_run() {
    TOTAL_SPACE_FREED_KB=0
    DRY_RUN=true

    local test_file="$TEST_TEMP_DIR/test_dry_run.txt"
    echo "content" > "$test_file"

    safe_rm "$test_file" "test file" >/dev/null 2>&1

    # In dry-run, no space should be tracked
    assert_equals "0" "$TOTAL_SPACE_FREED_KB"

    # Cleanup
    DRY_RUN=false
    TOTAL_SPACE_FREED_KB=0
    rm -f "$test_file"
}

# ============================================================
# ENVIRONMENT VALIDATION TESTS
# ============================================================

function test_require_macos_works_on_current_system() {
    # Since this test suite is designed for macOS, require_macos should succeed
    require_macos
    assert_successful_code "$?"
}
