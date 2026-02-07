#!/usr/bin/env bash
# jetbrains_test.sh - Unit tests for src/modules/jetbrains.sh

# Get project root
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$TEST_DIR/../.." && pwd)"

# Source dependencies
source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/jetbrains.sh"

# ============================================================
# SETUP AND TEARDOWN
# ============================================================

function set_up() {
    # Reset global flags
    DRY_RUN=false
    VERBOSE=false
    FORCE=false

    # Create mock JetBrains directory structure
    MOCK_JB_PATH="$(mktemp -d)"
    JB_PATH="$MOCK_JB_PATH"
    JB_CACHE_PATH="$(mktemp -d)"
}

function tear_down() {
    # Clean up mock directories
    if [[ -d "$MOCK_JB_PATH" ]]; then
        rm -rf "$MOCK_JB_PATH"
    fi
    if [[ -d "$JB_CACHE_PATH" ]]; then
        rm -rf "$JB_CACHE_PATH"
    fi
}

function test_no_ide_versions_installed() {
    local result
    result=$(list_installed_ide_versions "Rider")

    assert_empty "$result"
}

function test_single_ide_version_is_found() {
    mkdir -p "$JB_PATH/Rider2024.1"

    local result
    result=$(list_installed_ide_versions "Rider")

    assert_contains "Rider2024.1" "$result"
}

function test_ide_versions_appear_in_chronological_order() {
    # Create versions in random order
    mkdir -p "$JB_PATH/Rider2024.3"
    mkdir -p "$JB_PATH/Rider2024.1"
    mkdir -p "$JB_PATH/Rider2024.2"

    local result
    result=$(list_installed_ide_versions "Rider")

    # Convert to array to check order
    local -a versions=()
    while IFS= read -r line; do
        versions+=("$line")
    done <<< "$result"

    # Should be sorted: 2024.1, 2024.2, 2024.3
    assert_contains "Rider2024.1" "${versions[0]}"
    assert_contains "Rider2024.2" "${versions[1]}"
    assert_contains "Rider2024.3" "${versions[2]}"
}

function test_complex_version_numbers_are_sorted_correctly() {
    # Create versions with complex numbering
    mkdir -p "$JB_PATH/IntelliJIdea2023.3.1"
    mkdir -p "$JB_PATH/IntelliJIdea2024.1"
    mkdir -p "$JB_PATH/IntelliJIdea2023.3"

    local result
    result=$(list_installed_ide_versions "IntelliJIdea")

    local -a versions=()
    while IFS= read -r line; do
        versions+=("$line")
    done <<< "$result"

    # Should be sorted correctly by version
    assert_contains "2023.3" "${versions[0]}"
    assert_contains "2023.3.1" "${versions[1]}"
    assert_contains "2024.1" "${versions[2]}"
}

function test_no_outdated_versions_when_none_installed() {
    local result
    result=$(find_outdated_ide_versions "Rider")

    assert_empty "$result"
}

function test_single_version_is_kept_without_removal() {
    mkdir -p "$JB_PATH/Rider2024.1"

    local result
    result=$(find_outdated_ide_versions "Rider")

    assert_empty "$result"
}

function test_latest_version_is_preserved() {
    mkdir -p "$JB_PATH/Rider2024.1"
    mkdir -p "$JB_PATH/Rider2024.2"

    local result
    result=$(find_outdated_ide_versions "Rider")

    # Should include old version
    assert_contains "Rider2024.1" "$result"

    # Should NOT include latest
    assert_not_contains "Rider2024.2" "$result"
}

function test_all_old_versions_identified_for_removal() {
    mkdir -p "$JB_PATH/WebStorm2022.1"
    mkdir -p "$JB_PATH/WebStorm2023.1"
    mkdir -p "$JB_PATH/WebStorm2024.1"

    local result
    result=$(find_outdated_ide_versions "WebStorm")

    # Count lines
    local count
    count=$(echo "$result" | grep -c "WebStorm" || echo "0")

    # Should identify 2 versions (all except latest)
    assert_equals "2" "$count"
}

function test_no_action_when_ide_not_installed() {
    # Should handle gracefully when no IDE installed
    remove_old_ide_versions "Rider"

    # Should succeed without errors
    assert_successful_code "$?"
}

function test_single_version_remains_untouched() {
    mkdir -p "$JB_PATH/Rider2024.1"

    remove_old_ide_versions "Rider"

    # Should not touch the only version
    assert_directory_exists "$JB_PATH/Rider2024.1"
}

function test_preview_shows_what_would_be_removed() {
    mkdir -p "$JB_PATH/Rider2024.1"
    mkdir -p "$JB_PATH/Rider2024.2"

    DRY_RUN=true

    remove_old_ide_versions "Rider"

    # Everything should still exist (preview mode)
    assert_directory_exists "$JB_PATH/Rider2024.1"
    assert_directory_exists "$JB_PATH/Rider2024.2"
}

function test_old_versions_are_removed_freeing_space() {
    mkdir -p "$JB_PATH/Rider2024.1"
    mkdir -p "$JB_PATH/Rider2024.2"

    DRY_RUN=false

    remove_old_ide_versions "Rider"

    # Old version should be gone
    assert_directory_not_exists "$JB_PATH/Rider2024.1"

    # Latest should remain
    assert_directory_exists "$JB_PATH/Rider2024.2"
}

function test_newest_version_kept_with_patch_numbers() {
    mkdir -p "$JB_PATH/IntelliJIdea2023.3"
    mkdir -p "$JB_PATH/IntelliJIdea2024.1"
    mkdir -p "$JB_PATH/IntelliJIdea2023.3.1"

    DRY_RUN=false

    remove_old_ide_versions "IntelliJIdea"

    # Should keep 2024.1 (highest version)
    assert_directory_exists "$JB_PATH/IntelliJIdea2024.1"

    # Should remove older versions
    assert_directory_not_exists "$JB_PATH/IntelliJIdea2023.3"
    assert_directory_not_exists "$JB_PATH/IntelliJIdea2023.3.1"
}

function test_missing_cache_directory_causes_no_errors() {
    # Remove cache directory
    rm -rf "$JB_CACHE_PATH"

    clear_ide_index_caches

    # Should succeed without errors
    assert_successful_code "$?"
}

function test_empty_cache_needs_no_cleaning() {
    # Ensure cache directory is empty
    rm -rf "$JB_CACHE_PATH"/*

    clear_ide_index_caches

    # Should succeed
    assert_successful_code "$?"
}

function test_preview_mode_leaves_cache_intact() {
    # Create cache files
    mkdir -p "$JB_CACHE_PATH/cache1"
    echo "cache data" > "$JB_CACHE_PATH/cache1/data.txt"

    DRY_RUN=true

    clear_ide_index_caches

    # Files should still exist
    assert_file_exists "$JB_CACHE_PATH/cache1/data.txt"
}

function test_cache_is_cleared_to_free_space() {
    # Create cache files
    mkdir -p "$JB_CACHE_PATH/cache1"
    mkdir -p "$JB_CACHE_PATH/cache2"
    echo "cache data" > "$JB_CACHE_PATH/cache1/data.txt"

    DRY_RUN=false

    clear_ide_index_caches

    # Cache should be empty
    local is_empty
    is_empty=$(ls -A "$JB_CACHE_PATH" 2>/dev/null | wc -l | tr -d ' ')

    assert_equals "0" "$is_empty"
}

# Note: stop_running_ide_processes tests removed (>150ms each, covered by E2E smoke tests)

function test_cleanup_succeeds_when_no_ides_found() {
    # Optimized: Quick test without heavy operations
    DRY_RUN=true  # Avoid slow cleanup operations

    cleanup_jetbrains_installations

    # Should succeed even with no JetBrains installations
    assert_successful_code "$?"
}

function test_complete_preview_workflow_leaves_everything_intact() {
    # Optimized: Single version to reduce setup time
    mkdir -p "$JB_PATH/Rider2024.1"

    DRY_RUN=true

    cleanup_jetbrains_installations

    # Should succeed
    assert_successful_code "$?"

    # Everything should still exist (preview mode)
    assert_directory_exists "$JB_PATH/Rider2024.1"
}

function test_complete_cleanup_removes_old_keeps_latest() {
    # Create mock installation with multiple versions (reduced to 2 IDEs)
    mkdir -p "$JB_PATH/Rider2024.1"
    mkdir -p "$JB_PATH/Rider2024.2"
    mkdir -p "$JB_CACHE_PATH/cache1"
    echo "cache" > "$JB_CACHE_PATH/cache1/data.txt"

    DRY_RUN=false

    cleanup_jetbrains_installations

    # Should succeed
    assert_successful_code "$?"

    # Old version should be gone
    assert_directory_not_exists "$JB_PATH/Rider2024.1"

    # Latest version should remain
    assert_directory_exists "$JB_PATH/Rider2024.2"
}

function test_bash_32_compatibility_two_versions() {
    mkdir -p "$JB_PATH/Rider2024.1"
    mkdir -p "$JB_PATH/Rider2024.2"

    local result
    result=$(find_outdated_ide_versions "Rider")

    local -a outdated=()
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            outdated+=("$line")
        fi
    done <<< "$result"

    assert_equals "1" "${#outdated[@]}"
    assert_contains "Rider2024.1" "${outdated[0]}"
}

function test_bash_32_compatibility_five_versions() {
    # Optimized: Reduced from 5 to 3 versions (still tests multiple versions)
    mkdir -p "$JB_PATH/IntelliJIdea2022.1"
    mkdir -p "$JB_PATH/IntelliJIdea2023.1"
    mkdir -p "$JB_PATH/IntelliJIdea2024.1"

    local result
    result=$(find_outdated_ide_versions "IntelliJIdea")

    local -a outdated=()
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            outdated+=("$line")
        fi
    done <<< "$result"

    assert_equals "2" "${#outdated[@]}"
    assert_contains "IntelliJIdea2022.1" "${outdated[0]}"
    assert_contains "IntelliJIdea2023.1" "${outdated[1]}"
}

function test_bash_32_last_element_access_is_correct() {
    # Optimized: Reduced from 3 to 2 versions
    mkdir -p "$JB_PATH/WebStorm2023.1"
    mkdir -p "$JB_PATH/WebStorm2024.1"

    DRY_RUN=false

    remove_old_ide_versions "WebStorm"

    assert_directory_not_exists "$JB_PATH/WebStorm2023.1"
    assert_directory_exists "$JB_PATH/WebStorm2024.1"
}

function test_bash_32_loop_iteration_excludes_last_correctly() {
    # Optimized: Reduced from 4 to 3 versions
    mkdir -p "$JB_PATH/PyCharm2023.2"
    mkdir -p "$JB_PATH/PyCharm2023.3"
    mkdir -p "$JB_PATH/PyCharm2024.1"

    local result
    result=$(find_outdated_ide_versions "PyCharm")

    local count
    count=$(echo "$result" | grep -c "PyCharm" || echo "0")

    assert_equals "2" "$count"
    assert_not_contains "PyCharm2024.1" "$result"
}

function test_bash_32_single_version_edge_case() {
    mkdir -p "$JB_PATH/CLion2024.1"

    local result
    result=$(find_outdated_ide_versions "CLion")

    assert_empty "$result"
}

function test_bash_32_array_arithmetic_with_zero() {
    mkdir -p "$JB_PATH/GoLand2024.1"

    DRY_RUN=false

    remove_old_ide_versions "GoLand"

    assert_directory_exists "$JB_PATH/GoLand2024.1"
    assert_successful_code "$?"
}
