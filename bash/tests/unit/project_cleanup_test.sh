#!/usr/bin/env bash
# project_cleanup_test.sh - Tests for stale node_modules cleanup module

# Get project root
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$TEST_DIR/../.." && pwd)"

# Source dependencies
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/project_cleanup.sh"

# ============================================================
# SETUP AND TEARDOWN
# ============================================================

ORIGINAL_HOME=""

function set_up() {
    DRY_RUN=true
    VERBOSE=false
    FORCE=true
    ANALYZE_MODE=false
    ANALYZE_ITEMS=()
    TOTAL_SPACE_FREED_KB=0

    TEST_TEMP_DIR="$(mktemp -d)"
    ORIGINAL_HOME="$HOME"
}

function tear_down() {
    HOME="$ORIGINAL_HOME"
    if [[ -d "$TEST_TEMP_DIR" ]]; then
        rm -rf "$TEST_TEMP_DIR"
    fi
}

# ============================================================
# HELPER: create a project with node_modules and control timestamps
# Args: $1=base_dir $2=project_name $3=timestamp (touch -t format, empty = now)
# ============================================================

create_project() {
    local base="$1"
    local name="$2"
    local ts="$3"  # e.g. "202507010000" or empty for current time

    local dir="$base/$name"
    mkdir -p "$dir/node_modules/some-pkg"
    echo '{"name":"some-pkg"}' > "$dir/node_modules/some-pkg/package.json"
    echo '{"name":"'"$name"'"}' > "$dir/package.json"
    echo "console.log('hi')" > "$dir/index.js"

    if [[ -n "$ts" ]]; then
        touch -t "$ts" "$dir/package.json" "$dir/index.js"
    fi
}

# ============================================================
# HELPER FUNCTION TESTS
# ============================================================

function test_is_project_inactive_detects_stale_project() {
    local project_dir="$TEST_TEMP_DIR/stale"
    mkdir -p "$project_dir"
    echo "x" > "$project_dir/index.js"
    # Jul 2025 \u2192 well over 90 days ago from 2026-02-05
    touch -t 202507010000 "$project_dir/index.js"

    is_project_inactive "$project_dir"
    assert_successful_code "$?"
}

function test_is_project_inactive_skips_active_project() {
    local project_dir="$TEST_TEMP_DIR/active"
    mkdir -p "$project_dir"
    echo "x" > "$project_dir/index.js"
    # Default timestamp = now \u2192 active

    is_project_inactive "$project_dir"
    local result=$?

    if [[ "$result" -eq 0 ]]; then
        assert_fail "Active project should not be detected as inactive"
    fi
}

function test_is_project_inactive_ignores_node_modules_timestamps() {
    # Project source files are old, but node_modules has a recent file.
    # The recent file inside node_modules must NOT count as activity.
    local project_dir="$TEST_TEMP_DIR/nm-ignore"
    mkdir -p "$project_dir/node_modules"
    echo "x" > "$project_dir/index.js"
    touch -t 202507010000 "$project_dir/index.js"
    # Recent file inside node_modules \u2014 should be ignored
    echo "y" > "$project_dir/node_modules/recent.txt"

    is_project_inactive "$project_dir"
    assert_successful_code "$?"
}

function test_is_project_inactive_ignores_git_timestamps() {
    # .git directory has recent files but source is old
    local project_dir="$TEST_TEMP_DIR/git-ignore"
    mkdir -p "$project_dir/.git"
    echo "x" > "$project_dir/index.js"
    touch -t 202507010000 "$project_dir/index.js"
    echo "y" > "$project_dir/.git/HEAD"

    is_project_inactive "$project_dir"
    assert_successful_code "$?"
}

function test_get_project_inactive_days_returns_positive_number() {
    local project_dir="$TEST_TEMP_DIR/days-calc"
    mkdir -p "$project_dir"
    echo "x" > "$project_dir/README.md"
    touch -t 202507010000 "$project_dir/README.md"

    local days
    days=$(get_project_inactive_days "$project_dir")

    # Jul 2025 to Feb 2026 > 200 days
    if [[ "$days" -lt 200 ]]; then
        assert_fail "Expected >200 inactive days, got $days"
    fi
}

function test_get_project_inactive_days_returns_9999_for_empty_project() {
    local project_dir="$TEST_TEMP_DIR/no-source"
    mkdir -p "$project_dir/node_modules"
    # No source files at all

    local days
    days=$(get_project_inactive_days "$project_dir")

    assert_equals 9999 "$days"
}

# ============================================================
# SCAN & CLEANUP TESTS (override HOME \u2192 temp dir)
# ============================================================

function test_dry_run_preserves_stale_node_modules() {
    HOME="$TEST_TEMP_DIR"
    create_project "$TEST_TEMP_DIR" "old-app" "202507010000"

    DRY_RUN=true
    cleanup_stale_node_modules

    if [[ ! -d "$TEST_TEMP_DIR/old-app/node_modules" ]]; then
        assert_fail "node_modules was deleted in dry-run mode"
    fi
}

function test_analyze_mode_registers_stale_projects() {
    HOME="$TEST_TEMP_DIR"
    create_project "$TEST_TEMP_DIR" "analyze-app" "202507010000"

    ANALYZE_MODE=true
    ANALYZE_ITEMS=()

    cleanup_stale_node_modules

    local found=false
    for item in "${ANALYZE_ITEMS[@]:-}"; do
        if [[ "$item" == *"node_modules"* ]]; then
            found=true
            break
        fi
    done
    assert_true "$found" "Stale project should be registered in ANALYZE_ITEMS"
}

function test_skips_empty_node_modules() {
    HOME="$TEST_TEMP_DIR"

    local project_dir="$TEST_TEMP_DIR/empty-nm"
    mkdir -p "$project_dir/node_modules"   # empty dir
    echo "x" > "$project_dir/package.json"
    touch -t 202507010000 "$project_dir/package.json"

    ANALYZE_MODE=true
    ANALYZE_ITEMS=()

    cleanup_stale_node_modules

    # Nothing should be registered \u2014 node_modules is empty
    if [[ ${#ANALYZE_ITEMS[@]} -gt 0 ]]; then
        assert_fail "Empty node_modules should not be reported"
    fi
}

function test_active_project_node_modules_not_removed() {
    HOME="$TEST_TEMP_DIR"
    # Active project: source files have current timestamps
    create_project "$TEST_TEMP_DIR" "active-app" ""

    DRY_RUN=false
    FORCE=true
    cleanup_stale_node_modules

    if [[ ! -d "$TEST_TEMP_DIR/active-app/node_modules" ]]; then
        assert_fail "node_modules was removed from an active project"
    fi
}

function test_handles_no_projects_gracefully() {
    HOME="$TEST_TEMP_DIR"
    # Empty temp dir \u2014 nothing to find

    DRY_RUN=true
    cleanup_stale_node_modules
    assert_successful_code "$?"
}

function test_removes_stale_node_modules_when_confirmed() {
    HOME="$TEST_TEMP_DIR"
    create_project "$TEST_TEMP_DIR" "confirmed-app" "202507010000"

    DRY_RUN=false
    FORCE=true   # auto-confirms
    cleanup_stale_node_modules

    if [[ -d "$TEST_TEMP_DIR/confirmed-app/node_modules" ]]; then
        assert_fail "Stale node_modules should have been removed"
    fi

    # Project root must survive \u2014 only node_modules deleted
    if [[ ! -d "$TEST_TEMP_DIR/confirmed-app" ]]; then
        assert_fail "Project root directory should not be deleted"
    fi
}

function test_multiple_stale_projects_all_cleaned() {
    HOME="$TEST_TEMP_DIR"
    create_project "$TEST_TEMP_DIR" "proj-a" "202507010000"
    create_project "$TEST_TEMP_DIR" "proj-b" "202506010000"
    create_project "$TEST_TEMP_DIR" "proj-c" "202504010000"

    DRY_RUN=false
    FORCE=true
    cleanup_stale_node_modules

    local all_gone=true
    for p in proj-a proj-b proj-c; do
        if [[ -d "$TEST_TEMP_DIR/$p/node_modules" ]]; then
            all_gone=false
        fi
    done

    assert_true "$all_gone" "All stale node_modules should be removed"
}

function test_mixed_active_and_stale_only_stale_removed() {
    HOME="$TEST_TEMP_DIR"
    create_project "$TEST_TEMP_DIR" "stale-mix" "202507010000"
    create_project "$TEST_TEMP_DIR" "active-mix" ""   # current timestamps

    DRY_RUN=false
    FORCE=true
    cleanup_stale_node_modules

    # Stale must be gone
    if [[ -d "$TEST_TEMP_DIR/stale-mix/node_modules" ]]; then
        assert_fail "Stale project node_modules should be removed"
    fi

    # Active must survive
    if [[ ! -d "$TEST_TEMP_DIR/active-mix/node_modules" ]]; then
        assert_fail "Active project node_modules should not be removed"
    fi
}
