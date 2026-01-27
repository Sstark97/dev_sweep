#!/usr/bin/env bash
# jetbrains.sh - JetBrains IDE cleanup module
# Frees disk space by removing outdated IDE versions while preserving the latest
# Clears corrupted index caches that slow down IDE performance
# DO NOT execute this file directly - source it from main script

set -euo pipefail

# ============================================================
# CONSTANTES DEL MÓDULO
# ============================================================

# Número mínimo de versiones a mantener (siempre guardamos la última)
readonly MINIMUM_IDE_VERSIONS_TO_KEEP=1

# Profundidad de búsqueda en el directorio de JetBrains
readonly IDE_SEARCH_DEPTH=1

# ============================================================
# JETBRAINS MODULE
# ============================================================

# Lists all installed versions of an IDE to identify which can be safely removed
# Sorts by version number so we can keep the latest and remove older ones
# Usage: list_installed_ide_versions <ide_name>
# Outputs: Paths to IDE installations, sorted chronologically (oldest to newest)
list_installed_ide_versions() {
    local product="$1"
    local -a versions=()

    # Check if JetBrains directory exists
    if [[ ! -d "$JB_PATH" ]]; then
        return 0
    fi

    # Collect all matching directories
    while IFS= read -r -d '' dir; do
        versions+=("$dir")
    done < <(find "$JB_PATH" -maxdepth "$IDE_SEARCH_DEPTH" -name "${product}*" -type d -print0 2>/dev/null)

    # Return sorted versions (using natural version sort)
    if [[ ${#versions[@]} -gt 0 ]]; then
        printf '%s\n' "${versions[@]}" | sort -V
    fi
}

# Identifies IDE versions that are outdated and can be safely removed
# Keeps the latest version for continued use, marks older ones for deletion
# Usage: find_outdated_ide_versions <ide_name>
# Outputs: Paths to outdated IDE installations that should be removed
find_outdated_ide_versions() {
    local ide_name="$1"
    local -a all_versions=()

    # Get all versions
    while IFS= read -r version_path; do
        if [[ -n "$version_path" ]]; then
            all_versions+=("$version_path")
        fi
    done < <(list_installed_ide_versions "$ide_name")

    local version_count=${#all_versions[@]}

    # If 0 or 1 version, nothing to delete
    if ((version_count <= MINIMUM_IDE_VERSIONS_TO_KEEP)); then
        return 0
    fi

    # Return all except the last one (latest) - Bash 3.2 compatible
    local last_index=$((version_count - 1))
    local i
    for ((i = 0; i < last_index; i++)); do
        printf '%s\n' "${all_versions[$i]}"
    done
}

# Removes outdated IDE versions to free disk space
# Preserves the latest version to ensure the developer can continue working
# Usage: remove_old_ide_versions <ide_name>
# Returns: 0 on success, 1 on error
remove_old_ide_versions() {
    local ide_name="$1"
    local -a all_versions=()
    local -a outdated_versions_to_remove=()
    local latest_version=""

    log_debug "Checking for outdated versions of $ide_name..."

    # Get all installed versions
    while IFS= read -r version_path; do
        if [[ -n "$version_path" ]]; then
            all_versions+=("$version_path")
        fi
    done < <(list_installed_ide_versions "$ide_name")

    local version_count=${#all_versions[@]}

    # Handle different installation scenarios
    if ((version_count == 0)); then
        log_debug "  No installations found for $ide_name"
        return 0
    elif ((version_count == MINIMUM_IDE_VERSIONS_TO_KEEP)); then
        log_debug "  Only one version exists for $ide_name: $(basename "${all_versions[0]}")"
        log_info "  ${GREEN}✓${NC} Keeping: $(basename "${all_versions[0]}")"
        return 0
    fi

    # Multiple versions found - keep latest, remove outdated ones
    # Bash 3.2 compatible: use arithmetic to get last index
    local last_index=$((version_count - 1))
    latest_version="${all_versions[$last_index]}"

    # Build array of outdated versions (all except last)
    local i
    for ((i = 0; i < last_index; i++)); do
        outdated_versions_to_remove+=("${all_versions[$i]}")
    done

    log_info "  ${GREEN}✓${NC} Keeping latest: $(basename "$latest_version")"

    # Remove outdated versions to free disk space
    for outdated_version in "${outdated_versions_to_remove[@]}"; do
        local version_display_name
        version_display_name="$(basename "$outdated_version")"
        safe_rm "$outdated_version" "$ide_name: $version_display_name"
    done

    return 0
}

# Clears corrupted or outdated IDE index caches to improve performance
# These caches will be automatically rebuilt when the IDE starts next time
# Usage: clear_ide_index_caches
clear_ide_index_caches() {
    log_section "Clearing IDE Index Caches"

    if [[ ! -d "$JB_CACHE_PATH" ]]; then
        log_debug "JetBrains cache directory does not exist"
        return 0
    fi

    if ! is_dir_not_empty "$JB_CACHE_PATH"; then
        log_info "JetBrains cache is already empty"
        return 0
    fi

    log_info "Clearing index caches (will be rebuilt on next IDE launch)..."

    # Delete all cache contents using safe_rm to track space
    safe_rm "$JB_CACHE_PATH" "Index caches cleared"

    return 0
}

# Stops all running IDE processes so files can be safely deleted
# Required before cleanup to prevent file-in-use errors
# Usage: stop_running_ide_processes
stop_running_ide_processes() {
    log_section "Stopping Running IDE Processes"

    local -a ide_process_names=(
        "Rider"
        "IntelliJ"
        "WebStorm"
        "DataGrip"
        "RustRover"
        "PyCharm"
        "GoLand"
        "CLion"
        "PhpStorm"
    )

    local any_processes_stopped=false

    for process_name in "${ide_process_names[@]}"; do
        if pgrep -f "$process_name" >/dev/null 2>&1; then
            safe_kill "$process_name"
            any_processes_stopped=true
        else
            log_debug "IDE not running: $process_name"
        fi
    done

    if [[ "$any_processes_stopped" == false ]]; then
        log_info "No IDE processes were running"
    fi

    return 0
}

# Main entry point: Reclaims disk space from outdated JetBrains IDEs
# Stops running processes, removes old versions, and clears corrupted caches
# Usage: cleanup_jetbrains_installations
# Returns: 0 on success, 1 on error
cleanup_jetbrains_installations() {
    log_section "JetBrains IDEs Cleanup"

    # Stop running IDE processes before cleanup
    stop_running_ide_processes

    # Check if any JetBrains IDEs are installed
    if [[ ! -d "$JB_PATH" ]]; then
        log_warn "JetBrains directory not found: $JB_PATH"
        log_info "No JetBrains IDEs detected on this system"
        return 0
    fi

    log_info "Analyzing JetBrains installations..."
    log_info "Strategy: Keep latest version, remove older versions"
    echo ""

    # Process each IDE product
    local found_items_to_clean=false
    for ide_product in "${JETBRAINS_PRODUCTS[@]}"; do
        if remove_old_ide_versions "$ide_product"; then
            found_items_to_clean=true
        fi
    done

    if [[ "$found_items_to_clean" == false ]]; then
        log_info "No outdated IDE versions found"
    fi

    echo ""

    # Clear index caches to improve performance
    clear_ide_index_caches

    log_success "JetBrains cleanup completed"
    return 0
}

# Export the main function
# (In bash, functions are automatically available when sourced, but this documents the public API)
# Public API: cleanup_jetbrains_installations
