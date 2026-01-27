#!/usr/bin/env bash
# common.sh - Core utility functions for DevSweep
# Provides logging, safety wrappers, and user interaction utilities
# DO NOT execute this file directly - source it from main script

set -euo pipefail

# Prevent double-sourcing (important for test environments)
if [[ -n "${DEVSWEEP_COMMON_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_COMMON_LOADED=true

# ============================================================
# COLOR CONSTANTS
# ============================================================
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly BLUE='\033[0;34m'
readonly YELLOW='\033[1;33m'
readonly CYAN='\033[0;36m'
readonly MAGENTA='\033[0;35m'
readonly NC='\033[0m' # No Color

# ============================================================
# LOGGING FUNCTIONS
# ============================================================

# Log info message (always shown)
log_info() {
    local message="$1"
    echo -e "${BLUE}ℹ${NC}  ${message}"
}

# Log success message (always shown)
log_success() {
    local message="$1"
    echo -e "${GREEN}✓${NC}  ${message}"
}

# Log warning message (always shown)
log_warn() {
    local message="$1"
    echo -e "${YELLOW}⚠${NC}  ${message}"
}

# Log error message (always shown)
log_error() {
    local message="$1"
    echo -e "${RED}✗${NC}  ${message}" >&2
}

# Log debug message (only shown in verbose mode)
log_debug() {
    local message="$1"
    if [[ "${VERBOSE}" == true ]]; then
        echo -e "${CYAN}→${NC}  ${message}"
    fi
}

# Print section header
log_section() {
    local title="$1"
    echo ""
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}${title}${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

# ============================================================
# SAFETY WRAPPERS
# ============================================================

# Safe rm wrapper - respects DRY_RUN flag
# Usage: safe_rm <target_path> [description]
safe_rm() {
    local target="$1"
    local description="${2:-$target}"

    # Validate target is not empty or root
    if [[ -z "$target" ]] || [[ "$target" == "/" ]] || [[ "$target" == "$HOME" ]]; then
        log_error "Invalid deletion target: $target"
        return 1
    fi

    # Check if target exists
    if [[ ! -e "$target" ]]; then
        log_debug "Target does not exist (skipping): $target"
        return 0
    fi

    # Capture size BEFORE deletion for tracking
    local size_before="0B"
    if [[ -e "$target" ]]; then
        size_before=$(get_size "$target")
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would delete: $description"
        return 0
    fi

    log_debug "Deleting: $description"
    rm -rf "$target"

    if [[ $? -eq 0 ]]; then
        log_success "Deleted: $description"
        # Track freed space after successful deletion
        track_freed_space "$size_before"
        return 0
    else
        log_error "Failed to delete: $description"
        return 1
    fi
}

# Safe pkill wrapper - respects DRY_RUN flag
# Usage: safe_kill <process_name>
safe_kill() {
    local process_name="$1"

    # Check if process is running
    if ! pgrep -f "$process_name" >/dev/null 2>&1; then
        log_debug "Process not running: $process_name"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would terminate: $process_name"
        return 0
    fi

    log_debug "Terminating process: $process_name"
    pkill -f "$process_name" 2>/dev/null || true

    log_success "Terminated: $process_name"
    return 0
}

# Execute arbitrary command with dry-run support
# Usage: execute_command <description> <command> [args...]
execute_command() {
    local description="$1"
    shift
    local command=("$@")

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would execute: $description"
        log_debug "  Command: ${command[*]}"
        return 0
    fi

    log_debug "Executing: $description"
    "${command[@]}"

    local exit_code=$?
    if [[ $exit_code -eq 0 ]]; then
        log_success "Completed: $description"
    else
        log_error "Failed: $description (exit code: $exit_code)"
    fi

    return $exit_code
}

# ============================================================
# USER INTERACTION
# ============================================================

# Simple yes/no confirmation (skipped with --force)
# Usage: confirm_action <prompt>
# Returns: 0 if confirmed, 1 if declined
confirm_action() {
    local prompt="$1"

    if [[ "$FORCE" == true ]]; then
        log_debug "Auto-confirming (--force enabled): $prompt"
        return 0
    fi

    echo -en "${YELLOW}?${NC}  ${prompt} [y/N]: "
    read -r response

    case "$response" in
        [yY]|[yY][eE][sS])
            return 0
            ;;
        *)
            log_info "Action cancelled by user"
            return 1
            ;;
    esac
}

# Dangerous action confirmation - requires typing "yes" explicitly
# Usage: confirm_dangerous <action_description>
# Returns: 0 if confirmed, 1 if declined
confirm_dangerous() {
    local action="$1"

    if [[ "$FORCE" == true ]]; then
        log_warn "Auto-confirming DESTRUCTIVE action (--force enabled): $action"
        return 0
    fi

    echo ""
    log_warn "⚠️  DESTRUCTIVE ACTION: $action"
    log_warn "This action cannot be undone and may result in data loss."
    echo ""
    echo -en "${RED}Type 'yes' to confirm:${NC} "
    read -r response

    if [[ "$response" == "yes" ]]; then
        return 0
    else
        log_info "Destructive action cancelled"
        return 1
    fi
}

# ============================================================
# ENVIRONMENT VALIDATION
# ============================================================

# Check if running on macOS
# Returns: 0 if macOS, 1 otherwise
require_macos() {
    if [[ "$(uname)" != "Darwin" ]]; then
        log_error "This tool only runs on macOS"
        return 1
    fi
    return 0
}

# Validate sudo access (request if needed)
# Usage: require_sudo
require_sudo() {
    if [[ "$DRY_RUN" == true ]]; then
        log_debug "[DRY-RUN] Would request sudo privileges"
        return 0
    fi

    log_info "Requesting sudo privileges..."
    if sudo -v; then
        log_success "Sudo access granted"
        return 0
    else
        log_error "Failed to obtain sudo privileges"
        return 1
    fi
}

# Keep sudo alive in background
# Usage: keep_sudo_alive
keep_sudo_alive() {
    if [[ "$DRY_RUN" == true ]]; then
        return 0
    fi

    # Update sudo timestamp in background every 60 seconds
    while true; do
        sudo -n true
        sleep 60
        kill -0 "$$" 2>/dev/null || exit
    done 2>/dev/null &
}

# ============================================================
# FILE SYSTEM HELPERS
# ============================================================

# Check if directory exists and is not empty
# Usage: is_dir_not_empty <path>
is_dir_not_empty() {
    local path="$1"
    [[ -d "$path" ]] && [[ -n "$(ls -A "$path" 2>/dev/null)" ]]
}

# Get human-readable size of file/directory
# Usage: get_size <path>
get_size() {
    local path="$1"
    if [[ -e "$path" ]]; then
        du -sh "$path" 2>/dev/null | cut -f1
    else
        echo "0B"
    fi
}

# Calculate total size of multiple paths
# Usage: calculate_total_size <path1> <path2> ...
calculate_total_size() {
    local total=0
    for path in "$@"; do
        if [[ -e "$path" ]]; then
            local size
            size=$(du -sk "$path" 2>/dev/null | cut -f1)
            total=$((total + size))
        fi
    done

    # Convert KB to human-readable format
    if ((total > 1048576)); then
        echo "$((total / 1048576))GB"
    elif ((total > 1024)); then
        echo "$((total / 1024))MB"
    else
        echo "${total}KB"
    fi
}

# ============================================================
# SPACE TRACKING
# ============================================================

# Convert KB to human-readable format
# Usage: format_kb_to_human <kb_value>
format_kb_to_human() {
    local kb="$1"
    if ((kb > 1048576)); then
        echo "$((kb / 1048576))GB"
    elif ((kb > 1024)); then
        echo "$((kb / 1024))MB"
    else
        echo "${kb}KB"
    fi
}

# Parse human-readable size to KB
# Usage: parse_size_to_kb <size_string>
# Examples: "2.3GB" -> 2411724, "500MB" -> 512000, "1024KB" -> 1024, "100B" -> 0
parse_size_to_kb() {
    local size_str="$1"

    # Remove spaces and convert to uppercase
    size_str=$(echo "$size_str" | tr -d ' ' | tr '[:lower:]' '[:upper:]')

    # Extract number and unit
    local number unit
    if [[ "$size_str" =~ ^([0-9.]+)([A-Z]+)$ ]]; then
        number="${BASH_REMATCH[1]}"
        unit="${BASH_REMATCH[2]}"
    else
        echo "0"
        return
    fi

    # Convert to KB based on unit
    case "$unit" in
        GB|G)
            # GB to KB: multiply by 1024*1024
            echo "$number" | awk '{printf "%d", $1 * 1048576}'
            ;;
        MB|M)
            # MB to KB: multiply by 1024
            echo "$number" | awk '{printf "%d", $1 * 1024}'
            ;;
        KB|K)
            # Already in KB
            echo "$number" | awk '{printf "%d", $1}'
            ;;
        B|BYTES)
            # Bytes to KB: divide by 1024 (rounded down)
            echo "$number" | awk '{printf "%d", $1 / 1024}'
            ;;
        *)
            echo "0"
            ;;
    esac
}

# Track freed space and add to running total
# Usage: track_freed_space <size_before>
track_freed_space() {
    local size_before="$1"
    local before_kb

    before_kb=$(parse_size_to_kb "$size_before")

    if [[ $before_kb -gt 0 ]]; then
        TOTAL_SPACE_FREED_KB=$((TOTAL_SPACE_FREED_KB + before_kb))
        log_debug "Tracked ${size_before} (${before_kb}KB) - Total: $(format_kb_to_human $TOTAL_SPACE_FREED_KB)"
    fi
}

# ============================================================
# BANNER / UI
# ============================================================

# Print application banner
print_banner() {
    echo -e "${BLUE}╭─────────────────────────────────────────╮${NC}"
    echo -e "${BLUE}│${NC}        ${GREEN}DevSweep v${VERSION}${NC}                ${BLUE}│${NC}"
    echo -e "${BLUE}│${NC}    ${CYAN}macOS Developer Cache Cleaner${NC}      ${BLUE}│${NC}"
    echo -e "${BLUE}╰─────────────────────────────────────────╯${NC}"
    echo ""
}

# Print completion message
print_completion() {
    local freed_space="${1:-Unknown}"
    echo ""
    echo -e "${GREEN}╭─────────────────────────────────────────╮${NC}"
    echo -e "${GREEN}│${NC}           ${GREEN}✨ CLEANUP COMPLETE ✨${NC}         ${GREEN}│${NC}"
    if [[ "$freed_space" != "Unknown" ]]; then
        echo -e "${GREEN}│${NC}      ${CYAN}Estimated space freed: ${freed_space}${NC}    ${GREEN}│${NC}"
    fi
    echo -e "${GREEN}╰─────────────────────────────────────────╯${NC}"
    echo ""
}
