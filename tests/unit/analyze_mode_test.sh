#!/usr/bin/env bash
# analyze_mode_test.sh - Tests for analyze mode functionality
# Tests that --analyze mode collects information without deleting files

# ============================================================
# SOURCE DEPENDENCIES (once at load time)
# ============================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"

# ============================================================
# BASHUNIT HOOKS (called automatically before/after each test)
# ============================================================

function set_up() {
    # Reset global state before each test
    ANALYZE_MODE=false
    DRY_RUN=false
    VERBOSE=false
    FORCE=false
    ANALYZE_ITEMS=()
    TOTAL_SPACE_FREED_KB=0

    # Create temporary test directory
    TEST_DIR=$(mktemp -d)
}

function tear_down() {
    # Clean up test directory
    if [[ -n "${TEST_DIR:-}" ]] && [[ -d "$TEST_DIR" ]]; then
        rm -rf "$TEST_DIR"
    fi

    # Reset global state
    ANALYZE_MODE=false
    ANALYZE_ITEMS=()
}

# ============================================================
# ANALYZE ITEM COLLECTION TESTS
# ============================================================

function test_add_analyze_item_collection() {
    # Test single item
    add_analyze_item "TestModule" "test-file" "1.5MB"
    assert_equals 1 "${#ANALYZE_ITEMS[@]}"
    assert_contains "TestModule|test-file|1.5MB" "${ANALYZE_ITEMS[0]}"

    # Test multiple items
    add_analyze_item "Module1" "file1" "100KB"
    add_analyze_item "Module2" "file2" "200KB"
    assert_equals 3 "${#ANALYZE_ITEMS[@]}"

    # Test special characters
    add_analyze_item "JetBrains" "IntelliJIdea2023.1" "2.4GB"
    assert_equals 4 "${#ANALYZE_ITEMS[@]}"
    assert_contains "IntelliJIdea2023.1" "${ANALYZE_ITEMS[3]}"
}

# ============================================================
# SAFE_RM IN ANALYZE MODE TESTS
# ============================================================

function test_safe_rm_does_not_delete_in_analyze_mode() {
    # Create test file
    local test_file="$TEST_DIR/test_file.txt"
    echo "test content" > "$test_file"
    assert_file_exists "$test_file"

    # Enable analyze mode
    ANALYZE_MODE=true

    # Call safe_rm
    safe_rm "$test_file" "test file"

    # File should still exist in analyze mode
    assert_file_exists "$test_file"

    # No space should be tracked in analyze mode
    assert_equals 0 "$TOTAL_SPACE_FREED_KB"
}

function test_safe_rm_deletes_when_analyze_mode_disabled() {
    # Create test file
    local test_file="$TEST_DIR/test_file.txt"
    echo "test content" > "$test_file"
    assert_file_exists "$test_file"

    # Analyze mode is disabled by default
    ANALYZE_MODE=false

    # Call safe_rm
    safe_rm "$test_file" "test file"

    # File should be deleted
    assert_file_not_exists "$test_file"
}

# ============================================================
# PREVIEW DISPLAY TESTS
# ============================================================

function test_show_analyze_preview_formatting() {
    # Test with no items
    local output1
    output1=$(show_analyze_preview 2>&1)
    assert_contains "No items found" "$output1"

    # Test with single item
    add_analyze_item "TestModule" "test-file" "100KB"
    local output2
    output2=$(show_analyze_preview 2>&1)
    assert_contains "TestModule" "$output2"
    assert_contains "test-file" "$output2"

    # Test with grouped items
    add_analyze_item "Module1" "file1" "200KB"
    add_analyze_item "Module2" "file2" "1MB"
    local output3
    output3=$(show_analyze_preview 2>&1)
    assert_contains "Module1" "$output3"
    assert_contains "Module2" "$output3"
    assert_contains "Total estimated" "$output3"
}

# ============================================================
# INTEGRATION TEST
# ============================================================

function test_analyze_mode_full_workflow() {
    # Enable analyze mode
    ANALYZE_MODE=true

    # Create test files
    mkdir -p "$TEST_DIR/files"
    echo "test1" > "$TEST_DIR/files/file1.txt"
    echo "test2" > "$TEST_DIR/files/file2.txt"

    # Simulate collection
    add_analyze_item "TestModule" "file1.txt" "5B"
    add_analyze_item "TestModule" "file2.txt" "5B"

    # Files should still exist
    assert_file_exists "$TEST_DIR/files/file1.txt"
    assert_file_exists "$TEST_DIR/files/file2.txt"

    # Items should be collected
    assert_equals 2 "${#ANALYZE_ITEMS[@]}"

    # Preview should work
    local output
    output=$(show_analyze_preview 2>&1)
    assert_contains "TestModule" "$output"
}
