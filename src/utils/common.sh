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

# Log section header (highlighted)
log_section() {
    local message="$1"
    echo ""
    echo -e "${CYAN}━━━ ${message} ━━━${NC}"
    echo ""
}

# Nuclear mode confirmation - NEVER auto-confirms, even with --force
# Usage: confirm_nuclear <action_description>
# Returns: 0 if user types "yes", 1 otherwise
confirm_nuclear() {
    local action="$1"

    echo ""
    log_warn "⚠️  NUCLEAR MODE: $action"
    log_warn "This will DELETE ALL caches and dependencies."
    log_warn "You will need to re-download everything on next build."
    log_warn "The --force flag does NOT skip this confirmation."
    echo ""
    echo -en "${RED}Type 'yes' to confirm:${NC} "
    read -r response

    if [[ "$response" == "yes" ]]; then
        return 0
    else
        log_info "Nuclear action cancelled"
        return 1
    fi
}

# ============================================================
# SAFETY WRAPPERS
# ============================================================

# Executes a cleanup action respecting all modes (ANALYZE, DRY_RUN, normal)
# In ANALYZE mode: Only collects info, doesn't execute
# In DRY_RUN mode: Shows what would happen
# In normal mode: Executes the action
# Usage: execute_cleanup_action <module_name> <description> <size_estimate> <cleanup_function>
# Example: execute_cleanup_action "Docker" "stopped containers" "50MB" docker_prune_containers
execute_cleanup_action() {
    local module="$1"
    local description="$2"
    local size_estimate="$3"
    local cleanup_function="$4"
    
    # In analyze mode, just collect the item
    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "$module" "$description" "$size_estimate"
        return 0
    fi
    
    # In dry-run or normal mode, execute the cleanup function
    "$cleanup_function"
}

# Conditionally logs info message (suppressed in ANALYZE mode)
# Usage: log_cleanup_info <message>
log_cleanup_info() {
    if [[ "$ANALYZE_MODE" != true ]]; then
        log_info "$@"
    fi
}

# Conditionally logs section header (suppressed in ANALYZE mode)
# Usage: log_cleanup_section <title>
log_cleanup_section() {
    if [[ "$ANALYZE_MODE" != true ]]; then
        log_section "$@"
    fi
}

# Safe rm wrapper - respects DRY_RUN and ANALYZE_MODE flags
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

    # In analyze mode, don't delete - just return (items should be collected via add_analyze_item separately)
    if [[ "$ANALYZE_MODE" == true ]]; then
        log_debug "[ANALYZE] Would delete: $description ($size_before)"
        return 0
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
    local freed_space

    if [[ $TOTAL_SPACE_FREED_KB -gt 0 ]]; then
        freed_space=$(format_kb_to_human "$TOTAL_SPACE_FREED_KB")
    else
        freed_space=""
    fi

    echo ""
    echo -e "${GREEN}╭─────────────────────────────────────────╮${NC}"
    echo -e "${GREEN}│${NC}           ${GREEN}✨ CLEANUP COMPLETE ✨${NC}         ${GREEN}│${NC}"

    if [[ -n "$freed_space" ]]; then
        echo -e "${GREEN}│${NC}      ${CYAN}Total space freed: ${freed_space}${NC}       ${GREEN}│${NC}"
    else
        if [[ "$DRY_RUN" == true ]]; then
            echo -e "${GREEN}│${NC}   ${CYAN}No space freed (dry-run mode)${NC}       ${GREEN}│${NC}"
        fi
    fi

    echo -e "${GREEN}╰─────────────────────────────────────────╯${NC}"
    echo ""
}

# ============================================================
# ANALYZE MODE FUNCTIONS
# ============================================================

# Add item to analysis collection
# Usage: add_analyze_item <module_name> <description> <size_estimate>
# Example: add_analyze_item "JetBrains" "IntelliJIdea2023.1" "2.4GB"
add_analyze_item() {
    local module="$1"
    local description="$2"
    local size="$3"
    
    # Format: "module|description|size"
    ANALYZE_ITEMS+=("${module}|${description}|${size}")
}

# Registers item for analysis if in ANALYZE_MODE, otherwise returns false to continue with cleanup
# Usage: register_if_analyzing <module_name> <description> <size_or_path>
# Returns: 0 if analyzing (caller should return), 1 if not analyzing (caller should continue)
# Example: register_if_analyzing "Docker" "OrbStack cache" "$orbstack_cache" && return 0
register_if_analyzing() {
    local module="$1"
    local description="$2"
    local size_or_path="$3"
    
    if [[ "$ANALYZE_MODE" != true ]]; then
        return 1  # Not analyzing, continue with cleanup
    fi
    
    # If path is provided and exists, get size; otherwise use as-is
    local size="$size_or_path"
    if [[ -e "$size_or_path" ]]; then
        size=$(get_size "$size_or_path")
    fi
    
    add_analyze_item "$module" "$description" "$size"
    return 0  # Analyzing, caller should return early
}

# Display analysis preview in formatted box
# Shows what would be cleaned grouped by module with size estimates
# Usage: show_analyze_preview
show_analyze_preview() {
    if [[ ${#ANALYZE_ITEMS[@]} -eq 0 ]]; then
        echo ""
        log_info "No items found to clean"
        return 0
    fi
    
    # Group items by module
    declare -A module_items
    declare -A module_total_kb
    local total_space_kb=0
    
    # Process all items
    local item
    for item in "${ANALYZE_ITEMS[@]}"; do
        IFS='|' read -r module desc size <<< "$item"
        
        # Convert size to KB for total calculation
        local size_kb
        size_kb=$(parse_size_to_kb "$size")
        
        # Add to module group
        if [[ -z "${module_items[$module]:-}" ]]; then
            module_items[$module]="$desc ($size)"
            module_total_kb[$module]=$size_kb
        else
            module_items[$module]="${module_items[$module]}"$'\n'"$desc ($size)"
            module_total_kb[$module]=$((${module_total_kb[$module]} + size_kb))
        fi
        
        total_space_kb=$((total_space_kb + size_kb))
    done
    
    # Display preview box
    echo ""
    echo -e "${BLUE}╔════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${NC}  ${CYAN}DevSweep - Cleanup Preview${NC}                       ${BLUE}║${NC}"
    echo -e "${BLUE}╠════════════════════════════════════════════════════╣${NC}"
    
    # Display each module's items
    local module
    for module in "${!module_items[@]}"; do
        local module_size
        module_size=$(format_kb_to_human "${module_total_kb[$module]}")
        
        echo -e "${BLUE}║${NC}  ${GREEN}${module}:${NC}                                          ${BLUE}║${NC}"
        
        # Display each item in module
        while IFS= read -r line; do
            if [[ -n "$line" ]]; then
                # Pad line to fit in box (adjust spacing)
                local padded_line
                padded_line=$(printf "    • %-40s" "$line")
                echo -e "${BLUE}║${NC}  ${padded_line:0:48} ${BLUE}║${NC}"
            fi
        done <<< "${module_items[$module]}"
        
        echo -e "${BLUE}║${NC}                                                    ${BLUE}║${NC}"
    done
    
    # Display total
    local total_human
    total_human=$(format_kb_to_human "$total_space_kb")
    echo -e "${BLUE}╠════════════════════════════════════════════════════╣${NC}"
    echo -e "${BLUE}║${NC}  ${YELLOW}Total estimated space to free: ${total_human}${NC}           ${BLUE}║${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════════════════╝${NC}"
    echo ""
}
