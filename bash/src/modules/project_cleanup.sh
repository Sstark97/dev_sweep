#!/usr/bin/env bash
# project_cleanup.sh - Stale node_modules cleanup module
# Scans HOME for node_modules directories in inactive projects and removes them

set -euo pipefail

# Prevent double-sourcing
if [[ -n "${DEVSWEEP_PROJECT_CLEANUP_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_PROJECT_CLEANUP_LOADED=true

# ============================================================
# HELPERS
# ============================================================

# Replace HOME prefix with ~ for display
shorten_path() {
    echo "${1/$HOME/~}"
}

# Returns 0 when no project file has been touched within NODE_MODULES_STALE_DAYS.
# node_modules and .git directories are excluded from the activity check.
# Args: $1=project_dir
is_project_inactive() {
    local project_dir="$1"

    # head -1 short-circuits as soon as one recent file is found
    local active_file
    active_file=$(find "$project_dir" \
        -maxdepth 5 -type f \
        ! -path "*/node_modules/*" \
        ! -path "*/.git/*" \
        -mtime -"${NODE_MODULES_STALE_DAYS}" 2>/dev/null | head -1)

    [[ -z "$active_file" ]]
}

# Print the number of days since the most recent source-file modification.
# Returns 9999 when the project contains no source files at all.
# Args: $1=project_dir
get_project_inactive_days() {
    local project_dir="$1"

    local latest_epoch
    latest_epoch=$(find "$project_dir" \
        -type f \
        ! -path "*/node_modules/*" \
        ! -path "*/.git/*" \
        -exec stat -f %m {} + 2>/dev/null | sort -rn | head -1)

    if [[ -z "$latest_epoch" ]]; then
        echo 9999
        return
    fi

    local now
    now=$(date +%s)
    echo $(( (now - latest_epoch) / 86400 ))
}

# ============================================================
# SCAN
# ============================================================

# Print every top-level node_modules directory found under HOME.
# Skips: nested node_modules, macOS Library, .Trash.
find_node_modules() {
    find "${HOME}" \
        -maxdepth "$NODE_MODULES_MAX_DEPTH" \
        -type d -name "node_modules" \
        ! -path "*/node_modules/*" \
        ! -path "${HOME}/Library/*" \
        ! -path "${HOME}/.Trash/*" \
        2>/dev/null
}

# ============================================================
# CLEANUP
# ============================================================

# Scan, list, confirm and remove stale node_modules.
# Respects DRY_RUN, ANALYZE_MODE, and FORCE.
# Returns: 0 on success
cleanup_stale_node_modules() {
    log_cleanup_section "Stale node_modules Cleanup"
    log_cleanup_info "Scanning for inactive projects..."

    # Collect stale projects while staying in the current shell
    local stale_projects=()
    local stale_sizes=()
    local stale_days=()
    local total_size_kb=0

    while IFS= read -r nm_dir; do
        [[ -z "$nm_dir" ]] && continue

        local project_dir
        project_dir="$(dirname "$nm_dir")"

        # Skip empty node_modules
        if [[ -z "$(ls -A "$nm_dir" 2>/dev/null)" ]]; then
            continue
        fi

        if is_project_inactive "$project_dir"; then
            local size days size_kb
            size=$(get_size "$nm_dir")
            days=$(get_project_inactive_days "$project_dir")
            size_kb=$(parse_size_to_kb "$size")

            stale_projects+=("$project_dir")
            stale_sizes+=("$size")
            stale_days+=("$days")
            total_size_kb=$((total_size_kb + size_kb))
        fi
    done < <(find_node_modules)

    if [[ ${#stale_projects[@]} -eq 0 ]]; then
        log_cleanup_info "No inactive projects with node_modules found"
        return 0
    fi

    local total_count=${#stale_projects[@]}
    local total_size_human
    total_size_human=$(format_kb_to_human "$total_size_kb")

    # Display & analyze-mode registration
    log_cleanup_info "Found $total_count project(s) with stale node_modules:"
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    local i=0
    for project in "${stale_projects[@]}"; do
        local short_path
        short_path=$(shorten_path "$project")

        if [[ "$ANALYZE_MODE" == true ]]; then
            add_analyze_item "Projects" "node_modules in ${short_path} (inactive ${stale_days[$i]}d)" "${stale_sizes[$i]}"
        else
            echo "  ${short_path} (inactive ${stale_days[$i]} days, ${stale_sizes[$i]})"
        fi
        i=$((i + 1))
    done

    [[ "$ANALYZE_MODE" == true ]] && return 0

    echo ""

    # Dry-run
    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would remove node_modules from $total_count project(s) ($total_size_human total)"
        return 0
    fi

    # Confirm before bulk delete
    if ! confirm_action "Remove node_modules from $total_count project(s) ($total_size_human total)?"; then
        log_info "Stale node_modules cleanup skipped"
        return 0
    fi

    # Delete
    for project in "${stale_projects[@]}"; do
        local short_path
        short_path=$(shorten_path "$project")
        safe_rm "${project}/node_modules" "node_modules in ${short_path}"
    done

    log_success "Stale node_modules cleaned ($total_size_human freed)"
}

# ============================================================
# ENTRY POINT
# ============================================================

# Usage: project_cleanup_clean
# Returns: 0 on success
project_cleanup_clean() {
    cleanup_stale_node_modules
}
