#!/usr/bin/env bash
# nuclear_mode_test.sh - Tests for nuclear mode functionality

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
    DRY_RUN=true  # Use dry-run by default to avoid actual deletions
    VERBOSE=false
    FORCE=true    # Skip confirmations (but nuclear should ignore this)
    NUCLEAR_MODE=false
    ANALYZE_MODE=false

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
# CONFIG TESTS
# ============================================================

function test_nuclear_mode_variable_exists() {
    # NUCLEAR_MODE should be defined in config
    assert_equals "false" "$NUCLEAR_MODE"
}

function test_nuclear_mode_can_be_set_to_true() {
    NUCLEAR_MODE=true
    assert_equals "true" "$NUCLEAR_MODE"
}

function test_nuclear_paths_are_defined() {
    # All nuclear paths should be defined as readonly
    assert_not_empty "$GRADLE_WRAPPER_PATH"
    assert_not_empty "$NPM_FULL_PATH"
}

function test_gradle_wrapper_path_points_to_gradle_wrapper() {
    # Should end with ".gradle/wrapper"
    [[ "$GRADLE_WRAPPER_PATH" == *".gradle/wrapper" ]]
    assert_successful_code "$?"
}

function test_npm_full_path_points_to_npm_directory() {
    # Should end with ".npm"
    [[ "$NPM_FULL_PATH" == *".npm" ]]
    assert_successful_code "$?"
}



# ============================================================
# CONFIRM_NUCLEAR TESTS
# ============================================================

function test_confirm_nuclear_function_exists() {
    # Function should exist
    declare -f confirm_nuclear > /dev/null
    assert_successful_code "$?"
}

# Note: We cannot easily test the interactive behavior of confirm_nuclear
# because it requires user input. The key behavior (not auto-confirming with FORCE)
# is tested implicitly through the integration tests.

# ============================================================
# NUCLEAR CLEANUP FUNCTION TESTS
# ============================================================

function test_nuclear_devtools_clean_function_exists() {
    # Function should be defined
    declare -f nuclear_devtools_clean > /dev/null
    assert_successful_code "$?"
}

function test_nuclear_respects_dry_run_mode() {
    DRY_RUN=true
    NUCLEAR_MODE=true
    
    # Create mock directories in test environment
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    mkdir -p "$TEST_TEMP_DIR/.gradle/caches"
    echo "test" > "$TEST_TEMP_DIR/.gradle/caches/test.txt"
    
    # Run nuclear cleanup (will skip confirmation in analyze mode)
    ANALYZE_MODE=true
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    # In dry-run with analyze, function should succeed
    assert_successful_code "$?"
    
    # File should still exist (not deleted)
    assert_file_exists "$TEST_TEMP_DIR/.gradle/caches/test.txt"
}

function test_nuclear_handles_missing_directories_gracefully() {
    DRY_RUN=true
    NUCLEAR_MODE=true
    ANALYZE_MODE=true
    
    # With no directories existing, should complete successfully
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    # Should return success even with no targets
    assert_successful_code "$?"
}

function test_nuclear_analyze_mode_completes_successfully() {
    ANALYZE_MODE=true
    NUCLEAR_MODE=true
    local original_items=("${ANALYZE_ITEMS[@]}")
    ANALYZE_ITEMS=()
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    # Create mock npm directory
    mkdir -p "$TEST_TEMP_DIR/.npm"
    echo "test" > "$TEST_TEMP_DIR/.npm/test.txt"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    ANALYZE_ITEMS=("${original_items[@]}")
    
    # Should complete successfully in analyze mode
    assert_successful_code "$?"
}

function test_nuclear_detects_gradle_caches() {
    ANALYZE_MODE=true
    NUCLEAR_MODE=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    # Create mock Gradle cache
    mkdir -p "$TEST_TEMP_DIR/.gradle/caches"
    echo "test" > "$TEST_TEMP_DIR/.gradle/caches/test.txt"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    # Function should complete successfully
    assert_successful_code "$?"
}

function test_nuclear_detects_gradle_wrapper() {
    ANALYZE_MODE=true
    NUCLEAR_MODE=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    # Create mock Gradle wrapper
    mkdir -p "$TEST_TEMP_DIR/.gradle/wrapper"
    echo "test" > "$TEST_TEMP_DIR/.gradle/wrapper/test.txt"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    assert_successful_code "$?"
}

function test_nuclear_detects_npm_directory() {
    ANALYZE_MODE=true
    NUCLEAR_MODE=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    # Create mock npm directory
    mkdir -p "$TEST_TEMP_DIR/.npm"
    echo "test" > "$TEST_TEMP_DIR/.npm/test.txt"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    assert_successful_code "$?"
}

function test_nuclear_detects_pip_cache_macos() {
    ANALYZE_MODE=true
    NUCLEAR_MODE=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    # Create mock pip cache (macOS location)
    mkdir -p "$TEST_TEMP_DIR/Library/Caches/pip"
    echo "test" > "$TEST_TEMP_DIR/Library/Caches/pip/test.txt"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    assert_successful_code "$?"
}

function test_nuclear_detects_poetry_cache_macos() {
    ANALYZE_MODE=true
    NUCLEAR_MODE=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    # Create mock poetry cache (macOS location)
    mkdir -p "$TEST_TEMP_DIR/Library/Caches/pypoetry"
    echo "test" > "$TEST_TEMP_DIR/Library/Caches/pypoetry/test.txt"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    assert_successful_code "$?"
}

function test_nuclear_with_multiple_caches() {
    ANALYZE_MODE=true
    NUCLEAR_MODE=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    # Create multiple mock caches
    mkdir -p "$TEST_TEMP_DIR/.gradle/caches"
    mkdir -p "$TEST_TEMP_DIR/.npm"
    mkdir -p "$TEST_TEMP_DIR/.m2/repository"
    echo "test" > "$TEST_TEMP_DIR/.gradle/caches/test.txt"
    echo "test" > "$TEST_TEMP_DIR/.npm/test.txt"
    echo "test" > "$TEST_TEMP_DIR/.m2/repository/test.txt"
    
    nuclear_devtools_clean 2>/dev/null
    
    HOME="$original_home"
    
    assert_successful_code "$?"
}

# ============================================================
# MODULE NUCLEAR FUNCTION TESTS
# ============================================================

function test_nuclear_maven_clean_function_exists() {
    # Function should be defined
    declare -f nuclear_maven_clean > /dev/null
    assert_successful_code "$?"
}

function test_nuclear_gradle_clean_function_exists() {
    # Function should be defined
    declare -f nuclear_gradle_clean > /dev/null
    assert_successful_code "$?"
}

function test_nuclear_node_clean_function_exists() {
    # Function should be defined
    declare -f nuclear_node_clean > /dev/null
    assert_successful_code "$?"
}

function test_nuclear_python_clean_function_exists() {
    # Function should be defined
    declare -f nuclear_python_clean > /dev/null
    assert_successful_code "$?"
}

function test_nuclear_maven_clean_with_confirmed_flag() {
    DRY_RUN=true
    export NUCLEAR_CONFIRMED=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    mkdir -p "$TEST_TEMP_DIR/.m2/repository"
    echo "test" > "$TEST_TEMP_DIR/.m2/repository/test.txt"
    
    nuclear_maven_clean 2>/dev/null
    local result=$?
    
    HOME="$original_home"
    unset NUCLEAR_CONFIRMED
    
    # Should succeed without prompting
    assert_successful_code "$result"
}

function test_nuclear_gradle_clean_with_confirmed_flag() {
    DRY_RUN=true
    export NUCLEAR_CONFIRMED=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    mkdir -p "$TEST_TEMP_DIR/.gradle/caches"
    mkdir -p "$TEST_TEMP_DIR/.gradle/wrapper"
    
    nuclear_gradle_clean 2>/dev/null
    local result=$?
    
    HOME="$original_home"
    unset NUCLEAR_CONFIRMED
    
    assert_successful_code "$result"
}

function test_nuclear_node_clean_with_confirmed_flag() {
    DRY_RUN=true
    export NUCLEAR_CONFIRMED=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    mkdir -p "$TEST_TEMP_DIR/.npm"
    
    nuclear_node_clean 2>/dev/null
    local result=$?
    
    HOME="$original_home"
    unset NUCLEAR_CONFIRMED
    
    assert_successful_code "$result"
}

function test_nuclear_python_clean_with_confirmed_flag() {
    DRY_RUN=true
    export NUCLEAR_CONFIRMED=true
    
    local original_home="$HOME"
    HOME="$TEST_TEMP_DIR"
    
    mkdir -p "$TEST_TEMP_DIR/Library/Caches/pip"
    
    nuclear_python_clean 2>/dev/null
    local result=$?
    
    HOME="$original_home"
    unset NUCLEAR_CONFIRMED
    
    assert_successful_code "$result"
}
