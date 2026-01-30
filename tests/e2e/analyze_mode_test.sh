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

function test_analyze_shows_preview_and_cancels_cleanly() {
    # Test: --analyze shows preview and cancels on 'n'
    local output
    local exit_code=0
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --jetbrains 2>&1 || exit_code=$?)
    
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

function test_analyze_acceptance_executes_cleanup() {
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

function test_analyze_works_with_all_modules() {
    # Test: --analyze --all shows combined preview from multiple modules
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --jetbrains --devtools 2>&1 || true)
    
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

function test_analyze_combines_with_other_flags() {
    # Test: --analyze works with --dry-run and short flag -a
    local output1 output2
    output1=$(echo "n" | "$DEVSWEEP_BIN" --analyze --jetbrains --dry-run 2>&1 || true)
    output2=$(echo "n" | "$DEVSWEEP_BIN" -a --jetbrains 2>&1 || true)
    
    # Assert: Should show both modes with --dry-run
    if ! echo "$output1" | grep -q "Analyze mode enabled"; then
        echo "Should show analyze mode with --dry-run"
        return 1
    fi
    
    if ! echo "$output1" | grep -q "Dry-run mode enabled"; then
        echo "Should show dry-run mode"
        return 1
    fi
    
    # Assert: Short flag should work same as --analyze
    if ! echo "$output2" | grep -q "Analyze mode enabled"; then
        echo "Short flag -a should work"
        return 1
    fi
}

function test_analyze_auto_confirms_with_force() {
    # Test: --force with --analyze auto-confirms after preview
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
