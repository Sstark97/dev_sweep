#!/usr/bin/env bash
# interactive_menu_test.sh - End-to-end tests for interactive menu with loop
# Tests menu behavior, loop functionality, and user interactions

set -euo pipefail

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Path to devsweep binary
readonly DEVSWEEP_BIN="$PROJECT_ROOT/bin/devsweep"

# Test fixtures path (different from cleanup_test.sh to avoid conflicts)
readonly MENU_TEST_FIXTURES_PATH="${HOME}/.devsweep_test_fixtures_menu"

# ============================================================
# SETUP & TEARDOWN
# ============================================================

function setup_test_fixtures() {
    mkdir -p "$MENU_TEST_FIXTURES_PATH"
}

function teardown_test_fixtures() {
    if [[ -d "$MENU_TEST_FIXTURES_PATH" ]]; then
        rm -rf "$MENU_TEST_FIXTURES_PATH"
    fi
}

# ============================================================
# HELPER FUNCTIONS
# ============================================================

function create_test_cache() {
    local path="$1"
    mkdir -p "$path"
    # Create a small file (1KB) to avoid slowness
    dd if=/dev/zero of="$path/test.dat" bs=1024 count=1 2>/dev/null
}

function count_menu_displays() {
    local output="$1"
    local count
    count=$(echo "$output" | grep -o "Select cleanup modules to run:" | wc -l | tr -d ' ')
    # Default to 0 if empty
    echo "${count:-0}"
}

function count_cleanup_completions() {
    local output="$1"
    local count
    count=$(echo "$output" | grep -o "CLEANUP COMPLETE" | wc -l | tr -d ' ')
    echo "${count:-0}"
}

function count_continue_prompts() {
    local output="$1"
    local count
    count=$(echo "$output" | grep -o "Run another cleanup?" | wc -l | tr -d ' ')
    echo "${count:-0}"
}

# ============================================================
# INTERACTIVE MENU TESTS
# ============================================================

function test_menu_exits_on_quit() {
    # Test: User selects 'q' to quit
    local output
    output=$(echo "q" | "$DEVSWEEP_BIN" 2>&1 || true)
    
    # Assert: Should show menu and exit
    local menu_count=$(count_menu_displays "$output")
    if [[ $menu_count -ne 1 ]]; then
        assert_fail "Expected 1 menu display, got $menu_count"
    fi
    
    # Assert: Should show "Operation cancelled"
    if ! echo "$output" | grep -q "Operation cancelled"; then
        assert_fail "Should show 'Operation cancelled' message"
    fi
}

function test_menu_exits_when_user_declines_cleanup() {
    # Test: User selects option but declines confirmation
    local output
    output=$(printf "1\nn\n" | "$DEVSWEEP_BIN" 2>&1 || true)
    
    # Assert: Should show menu once
    local menu_count=$(count_menu_displays "$output")
    if [[ $menu_count -ne 1 ]]; then
        assert_fail "Expected 1 menu display, got $menu_count"
    fi
    
    # Assert: Should show "Operation cancelled"
    if ! echo "$output" | grep -q "Operation cancelled"; then
        assert_fail "Should show 'Operation cancelled' when declining"
    fi
}

function test_menu_loops_when_user_continues() {
    # Setup: Create test cache for JetBrains
    local test_cache="$MENU_TEST_FIXTURES_PATH/JetBrains"
    create_test_cache "$test_cache"
    
    # Test: Select JetBrains twice with continue in between
    local output
    output=$(printf "1\ny\ny\n1\ny\nn\n" | "$DEVSWEEP_BIN" 2>&1 || true)
    
    # Assert: Should show menu twice (initial + after continue)
    local menu_count=$(count_menu_displays "$output")
    if [[ $menu_count -lt 2 ]]; then
        assert_fail "Expected at least 2 menu displays (got $menu_count) - loop not working"
    fi
    
    # Assert: Should complete cleanup twice
    local cleanup_count=$(count_cleanup_completions "$output")
    if [[ $cleanup_count -lt 2 ]]; then
        assert_fail "Expected 2 cleanup completions (got $cleanup_count) - loop not executing"
    fi
    
    # Assert: Should show continue prompt at least once
    local continue_count=$(count_continue_prompts "$output")
    if [[ $continue_count -lt 1 ]]; then
        assert_fail "Expected at least 1 continue prompt (got $continue_count)"
    fi
}

function test_menu_exits_after_declining_continue() {
    # Test: Run cleanup once, then decline to continue
    local output
    output=$(printf "1\ny\nn\n" | "$DEVSWEEP_BIN" 2>&1 || true)
    
    # Assert: Should show menu once
    local menu_count=$(count_menu_displays "$output")
    if [[ $menu_count -ne 1 ]]; then
        assert_fail "Expected 1 menu display after declining continue (got $menu_count)"
    fi
    
    # Assert: Should complete cleanup once
    local cleanup_count=$(count_cleanup_completions "$output")
    if [[ $cleanup_count -ne 1 ]]; then
        assert_fail "Expected 1 cleanup completion (got $cleanup_count)"
    fi
    
    # Assert: Should show "Exiting DevSweep"
    if ! echo "$output" | grep -q "Exiting DevSweep"; then
        assert_fail "Should show 'Exiting DevSweep' when declining continue"
    fi
}

function test_menu_loop_with_different_modules() {
    # Test: Select different modules in each iteration
    local output
    output=$(printf "d\ny\n1\ny\nn\n" | "$DEVSWEEP_BIN" 2>&1 || true)
    
    # Assert: Should show menu twice
    local menu_count=$(count_menu_displays "$output")
    if [[ $menu_count -lt 2 ]]; then
        assert_fail "Expected 2 menu displays for different modules (got $menu_count)"
    fi
    
    # Assert: First iteration should be dry-run
    if ! echo "$output" | grep -q "DRY-RUN MODE"; then
        assert_fail "First iteration should be in dry-run mode"
    fi
    
    # Assert: Should have JetBrains cleanup in second iteration
    if ! echo "$output" | grep -q "JetBrains IDEs Cleanup"; then
        assert_fail "Second iteration should run JetBrains cleanup"
    fi
}

function test_menu_loop_multiple_iterations() {
    # Test: Execute 3 iterations of cleanup
    local output
    output=$(printf "1\ny\ny\n1\ny\ny\n1\ny\nn\n" | "$DEVSWEEP_BIN" 2>&1 || true)
    
    # Assert: Should show menu 3 times
    local menu_count=$(count_menu_displays "$output")
    if [[ $menu_count -lt 3 ]]; then
        assert_fail "Expected 3 menu displays for 3 iterations (got $menu_count)"
    fi
    
    # Assert: Should complete cleanup 3 times
    local cleanup_count=$(count_cleanup_completions "$output")
    if [[ $cleanup_count -lt 3 ]]; then
        assert_fail "Expected 3 cleanup completions (got $cleanup_count)"
    fi
    
    # Assert: Should show continue prompt at least 2 times
    local continue_count=$(count_continue_prompts "$output")
    if [[ $continue_count -lt 2 ]]; then
        assert_fail "Expected at least 2 continue prompts (got $continue_count)"
    fi
}

function test_cli_mode_no_loop() {
    # Test: CLI mode with flags should NOT loop
    local output
    output=$("$DEVSWEEP_BIN" --jetbrains --dry-run --yes 2>&1 || true)
    
    # Assert: Should NOT show menu
    local menu_count=$(count_menu_displays "$output")
    if [[ $menu_count -ne 0 ]]; then
        assert_fail "CLI mode should not show menu (got $menu_count displays)"
    fi
    
    # Assert: Should complete cleanup once
    local cleanup_count=$(count_cleanup_completions "$output")
    if [[ $cleanup_count -ne 1 ]]; then
        assert_fail "CLI mode should complete cleanup once (got $cleanup_count)"
    fi
    
    # Assert: Should NOT ask to continue
    local continue_count=$(count_continue_prompts "$output")
    if [[ $continue_count -ne 0 ]]; then
        assert_fail "CLI mode should not ask to continue (got $continue_count prompts)"
    fi
}

function test_menu_state_resets_between_iterations() {
    # Test: Verify state resets properly between loop iterations
    # First iteration: dry-run, Second iteration: normal
    local output
    output=$(printf "d\ny\n1\ny\nn\n" | "$DEVSWEEP_BIN" 2>&1 || true)
    
    # Count dry-run messages
    local dryrun_count=$(echo "$output" | grep -c "DRY-RUN MODE" || echo "0")
    
    # Assert: Should only have one dry-run message (from first iteration)
    if [[ $dryrun_count -lt 1 ]]; then
        assert_fail "First iteration should be in dry-run mode"
    fi
    
    # Assert: Second iteration should NOT be dry-run
    # Check that "No space freed (dry-run mode)" appears once
    local dryrun_completion=$(echo "$output" | grep -c "No space freed (dry-run mode)" || echo "0")
    if [[ $dryrun_completion -ne 1 ]]; then
        assert_fail "Only first iteration should show dry-run completion"
    fi
}

# ============================================================
# RUN TESTS
# ============================================================

# Setup before all tests
setup_test_fixtures

# Teardown after all tests
trap teardown_test_fixtures EXIT
