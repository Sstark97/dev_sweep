#!/usr/bin/env bash
# analyze_mode_test.sh - Tests for analyze mode functionality
# Tests that --analyze mode collects information without deleting files

# ============================================================
# TEST FIXTURES
# ============================================================

function setup() {
    # Setup test environment
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

    # Source modules to test
    source "$PROJECT_ROOT/src/utils/config.sh"
    source "$PROJECT_ROOT/src/utils/common.sh"
    
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

function teardown() {
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

function test_add_analyze_item_stores_data() {
    setup
    
    add_analyze_item "TestModule" "test-file" "1.5MB"
    
    # Verify item was added
    assert_equals 1 "${#ANALYZE_ITEMS[@]}"
    assert_contains "TestModule|test-file|1.5MB" "${ANALYZE_ITEMS[0]}"
    
    teardown
}

function test_add_analyze_item_multiple_items() {
    setup
    
    add_analyze_item "Module1" "file1" "100KB"
    add_analyze_item "Module1" "file2" "200KB"
    add_analyze_item "Module2" "file3" "1.5MB"
    
    # Verify all items were added
    assert_equals 3 "${#ANALYZE_ITEMS[@]}"
    
    teardown
}

function test_add_analyze_item_with_special_characters() {
    setup
    
    add_analyze_item "JetBrains" "IntelliJIdea2023.1" "2.4GB"
    
    # Verify item with special characters was stored correctly
    assert_equals 1 "${#ANALYZE_ITEMS[@]}"
    assert_contains "IntelliJIdea2023.1" "${ANALYZE_ITEMS[0]}"
    
    teardown
}

# ============================================================
# SAFE_RM IN ANALYZE MODE TESTS
# ============================================================

function test_safe_rm_does_not_delete_in_analyze_mode() {
    setup
    
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
    
    teardown
}

function test_safe_rm_deletes_when_analyze_mode_disabled() {
    setup
    
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
    
    teardown
}

# ============================================================
# PREVIEW DISPLAY TESTS
# ============================================================

function test_show_analyze_preview_with_no_items() {
    setup
    
    # Ensure no items
    ANALYZE_ITEMS=()
    
    # Call preview with no items
    local output
    output=$(show_analyze_preview 2>&1)
    
    # Should show "no items" message
    assert_contains "No items found" "$output"
    
    teardown
}

function test_show_analyze_preview_with_single_item() {
    setup
    
    # Add single item
    add_analyze_item "TestModule" "test-file" "100KB"
    
    # Call preview
    local output
    output=$(show_analyze_preview 2>&1)
    
    # Should contain module name and item
    assert_contains "TestModule" "$output"
    assert_contains "test-file" "$output"
    
    teardown
}

function test_show_analyze_preview_groups_by_module() {
    setup
    
    # Add items from different modules
    add_analyze_item "Module1" "file1" "100KB"
    add_analyze_item "Module1" "file2" "200KB"
    add_analyze_item "Module2" "file3" "1MB"
    
    # Call preview
    local output
    output=$(show_analyze_preview 2>&1)
    
    # Should contain both modules
    assert_contains "Module1" "$output"
    assert_contains "Module2" "$output"
    
    # Should contain files
    assert_contains "file1" "$output"
    assert_contains "file2" "$output"
    assert_contains "file3" "$output"
    
    teardown
}

function test_show_analyze_preview_shows_total() {
    setup
    
    # Add items with known sizes
    add_analyze_item "Module1" "file1" "1MB"
    add_analyze_item "Module1" "file2" "2MB"
    
    # Call preview
    local output
    output=$(show_analyze_preview 2>&1)
    
    # Should show total estimate
    assert_contains "Total estimated" "$output"
    
    teardown
}

# ============================================================
# INTEGRATION TEST
# ============================================================

function test_analyze_mode_full_workflow() {
    setup
    
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
    
    teardown
}
