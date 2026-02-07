#!/usr/bin/env bash
# smoke_test.sh - Minimal E2E smoke tests
# Following testing pyramid: Only critical end-to-end validations

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DEVSWEEP_BIN="$PROJECT_ROOT/bin/devsweep"

# ============================================================
# SMOKE TEST 1: CLI Mode Works
# ============================================================

function test_cli_mode_executes_successfully() {
    # Smoke: Verify binary executes with flags (most common usage)
    local output
    output=$("$DEVSWEEP_BIN" --jetbrains --dry-run --yes 2>&1 || true)

    # Assert: Should complete cleanup
    if ! echo "$output" | grep -q "CLEANUP COMPLETE"; then
        echo "CLI mode should complete cleanup"
        return 1
    fi

    # Assert: Should NOT show menu (CLI mode)
    if echo "$output" | grep -q "Select cleanup modules"; then
        echo "CLI mode should not show interactive menu"
        return 1
    fi
}

# ============================================================
# SMOKE TEST 2: Interactive Menu Works
# ============================================================

function test_interactive_menu_basic_flow() {
    # Smoke: Verify interactive menu launches and accepts quit
    local output
    output=$(echo "q" | "$DEVSWEEP_BIN" 2>&1 || true)

    # Assert: Should show menu
    if ! echo "$output" | grep -q "Select cleanup modules"; then
        echo "Should show interactive menu"
        return 1
    fi

    # Assert: Should handle quit gracefully
    if ! echo "$output" | grep -q "Operation cancelled"; then
        echo "Should show cancellation message"
        return 1
    fi
}

# ============================================================
# SMOKE TEST 3: Analyze Mode Works
# ============================================================

function test_analyze_mode_shows_preview() {
    # Smoke: Verify --analyze flag works
    local output
    output=$(echo "n" | "$DEVSWEEP_BIN" --analyze --jetbrains 2>&1 || true)

    # Assert: Should show analyze mode enabled
    if ! echo "$output" | grep -q "Analyze mode enabled"; then
        echo "Should show analyze mode message"
        return 1
    fi

    # Assert: Should show preview
    if ! echo "$output" | grep -q "Cleanup Preview"; then
        echo "Should show cleanup preview"
        return 1
    fi
}
