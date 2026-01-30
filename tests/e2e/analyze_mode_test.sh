#!/usr/bin/env bash
# analyze_mode_test.sh - End-to-end tests for --analyze mode
# Tests real command execution and preview functionality

set -euo pipefail

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

DEVSWEEP_BIN="$PROJECT_ROOT/bin/devsweep"

# ============================================================
# HELPER FUNCTIONS
# ============================================================

function count_preview_displays() {
    local output="$1"
    local count
    count=$(echo "$output" | grep -o "DevSweep - Cleanup Preview" | wc -l | tr -d ' ')
    echo "${count:-0}"
}

function count_cleanup_executions() {
    local output="$1"
    local count
    count=$(echo "$output" | grep -o "Starting cleanup..." | wc -l | tr -d ' ')
    echo "${count:-0}"
}


# ============================================================
# TEST CASES
# ============================================================

function test_analyze_flag_shows_preview() {
    # Test: --analyze shows preview before cleanup
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --jetbrains 2>&1 || true)
    
    # Assert: Should show analyze mode message
    if ! echo "$output" | grep -q "Analyze mode enabled"; then
        echo "Should show 'Analyze mode enabled' message"
        return 1
    fi
    
    # Assert: Should show preview box
    if ! echo "$output" | grep -q "DevSweep - Cleanup Preview"; then
        echo "Should show cleanup preview"
        return 1
    fi
    
    # Assert: Should ask for confirmation
    if ! echo "$output" | grep -q "Proceed with cleanup?"; then
        echo "Should ask for confirmation"
        return 1
    fi
}

function test_analyze_cancellation_exits_cleanly() {
    # Test: User declines analyze preview exits gracefully
    local output
    local exit_code=0
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --jetbrains 2>&1 || exit_code=$?)
    
    # Assert: Should exit with 0
    if [[ $exit_code -ne 0 ]]; then
        echo "Should exit with code 0, got $exit_code"
        return 1
    fi
    
    # Assert: Should show cancellation message
    if ! echo "$output" | grep -q "Action cancelled by user"; then
        echo "Should show cancellation message"
        return 1
    fi
}

function test_analyze_with_all_modules() {
    # Test: --analyze --all shows combined preview
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --all 2>&1 || true)
    
    # Assert: Should show preview
    local preview_count=$(count_preview_displays "$output")
    if [[ $preview_count -ne 1 ]]; then
        echo "Expected 1 preview display, got $preview_count"
        return 1
    fi
    
    # Assert: Should show total space
    if ! echo "$output" | grep -q "Total estimated space to free"; then
        echo "Should show total estimated space"
        return 1
    fi
}

function test_analyze_acceptance_triggers_cleanup() {
    # Test: Accepting analyze preview executes cleanup
    local output
    output=$(echo "y" | "$DEVSWEEP_BIN" --analyze --jetbrains --dry-run 2>&1 || true)
    
    # Assert: Should show preview first
    if ! echo "$output" | grep -q "DevSweep - Cleanup Preview"; then
        echo "Should show preview"
        return 1
    fi
    
    # Assert: Should execute cleanup
    local cleanup_count=$(count_cleanup_executions "$output")
    if [[ $cleanup_count -ne 1 ]]; then
        echo "Expected 1 cleanup execution, got $cleanup_count"
        return 1
    fi
    
    # Assert: Should show module execution
    if ! echo "$output" | grep -q "JetBrains IDEs Cleanup"; then
        echo "Should execute JetBrains module"
        return 1
    fi
}

function test_analyze_with_dry_run() {
    # Test: --analyze works with --dry-run
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --jetbrains --dry-run 2>&1 || true)
    
    # Assert: Should show both modes
    if ! echo "$output" | grep -q "Analyze mode enabled"; then
        echo "Should show analyze mode"
        return 1
    fi
    
    if ! echo "$output" | grep -q "Dry-run mode enabled"; then
        echo "Should show dry-run mode"
        return 1
    fi
}

function test_analyze_with_docker_module() {
    # Test: --analyze works with docker module
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --docker 2>&1 || true)
    
    # Assert: Should complete without errors
    if ! echo "$output" | grep -q "DevSweep - Cleanup Preview"; then
        echo "Should show preview"
        return 1
    fi
}

function test_analyze_with_homebrew_module() {
    # Test: --analyze works with homebrew module
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --homebrew 2>&1 || true)
    
    # Assert: Should show preview
    if ! echo "$output" | grep -q "DevSweep - Cleanup Preview"; then
        echo "Should show preview"
        return 1
    fi
}

function test_analyze_with_devtools_module() {
    # Test: --analyze works with devtools module
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --devtools 2>&1 || true)
    
    # Assert: Should show preview
    if ! echo "$output" | grep -q "DevSweep - Cleanup Preview"; then
        echo "Should show preview"
        return 1
    fi
}

function test_analyze_short_flag() {
    # Test: -a short flag works
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" -a --jetbrains 2>&1 || true)
    
    # Assert: Should work same as --analyze
    if ! echo "$output" | grep -q "Analyze mode enabled"; then
        echo "Short flag should work"
        return 1
    fi
}

function test_analyze_with_force_flag() {
    # Test: --force with --analyze auto-confirms
    local output
    output=$("$DEVSWEEP_BIN" --analyze --jetbrains --force --dry-run 2>&1 || true)
    
    # Assert: Should show preview
    if ! echo "$output" | grep -q "Analyze mode enabled"; then
        echo "Should show analyze mode"
        return 1
    fi
    
    # Assert: Should auto-confirm and execute
    local cleanup_count=$(count_cleanup_executions "$output")
    if [[ $cleanup_count -ne 1 ]]; then
        echo "Force flag should auto-confirm, expected 1 cleanup, got $cleanup_count"
        return 1
    fi
}
