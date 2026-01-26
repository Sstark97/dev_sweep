#!/usr/bin/env bash
# menu.sh - Interactive menu system for DevSweep
# DO NOT execute this file directly - source it from main script

set -euo pipefail

# ============================================================
# MENU SYSTEM
# ============================================================

# Show interactive menu and return selected modules
# Usage: show_interactive_menu
# Sets global array: SELECTED_MODULES
show_interactive_menu() {
    print_banner

    echo -e "${CYAN}Select cleanup modules to run:${NC}"
    echo ""
    echo "  [1] JetBrains IDEs - Clean old versions & caches"
    echo "  [2] Docker/OrbStack - Reset containers ${RED}(DESTRUCTIVE)${NC}"
    echo "  [3] Homebrew - Cleanup & prune cache"
    echo "  [4] Dev Tools - Maven, Gradle, Node caches"
    echo "  [5] System - Logs, caches, Spotlight rebuild ${RED}(REQUIRES SUDO)${NC}"
    echo ""
    echo "  [a] All modules"
    echo "  [d] Dry-run all (preview only)"
    echo "  [q] Quit"
    echo ""
    echo -en "${YELLOW}Enter your choice (1-5, a, d, q):${NC} "

    read -r choice

    case "$choice" in
        1)
            SELECTED_MODULES=("jetbrains")
            ;;
        2)
            SELECTED_MODULES=("docker")
            ;;
        3)
            SELECTED_MODULES=("homebrew")
            ;;
        4)
            SELECTED_MODULES=("devtools")
            ;;
        5)
            SELECTED_MODULES=("system")
            ;;
        a|A)
            SELECTED_MODULES=("jetbrains" "docker" "homebrew" "devtools" "system")
            ;;
        d|D)
            DRY_RUN=true
            SELECTED_MODULES=("jetbrains" "docker" "homebrew" "devtools" "system")
            log_info "Dry-run mode enabled - no files will be deleted"
            ;;
        q|Q)
            log_info "Operation cancelled by user"
            exit 0
            ;;
        *)
            log_error "Invalid choice: $choice"
            exit 1
            ;;
    esac

    echo ""
    log_success "Selected modules: ${SELECTED_MODULES[*]}"
    echo ""

    # Confirm before proceeding
    if ! confirm_action "Proceed with cleanup?"; then
        log_info "Operation cancelled"
        exit 0
    fi
}

# Alternative: Multi-select menu using checkboxes (more advanced)
# This version allows toggling multiple options with space
# NOTE: Requires Bash 4.0+ for associative arrays
show_multiselect_menu() {
    # Check bash version
    if ((BASH_VERSINFO[0] < 4)); then
        log_error "Multi-select menu requires Bash 4.0+"
        log_info "Using simple menu instead..."
        show_interactive_menu
        return
    fi

    print_banner

    declare -A selected=(
        [jetbrains]=true
        [docker]=false
        [homebrew]=true
        [devtools]=true
        [system]=false
    )

    local -a options=(
        "jetbrains:JetBrains IDEs - Clean old versions & caches"
        "docker:Docker/OrbStack - Reset containers (DESTRUCTIVE)"
        "homebrew:Homebrew - Cleanup & prune cache"
        "devtools:Dev Tools - Maven, Gradle, Node caches"
        "system:System - Logs, caches, Spotlight rebuild (SUDO)"
    )

    echo -e "${CYAN}Select modules to run (space to toggle, enter to confirm):${NC}"
    echo ""

    # Use bash select for simple menu
    PS3=$'\n'"${YELLOW}Enter option number to toggle (or 'done' to proceed):${NC} "

    local option
    select option in "$(echo -e "${GREEN}[x]${NC} JetBrains IDEs")" \
                     "$(echo -e "${RED}[ ]${NC} Docker/OrbStack (DESTRUCTIVE)")" \
                     "$(echo -e "${GREEN}[x]${NC} Homebrew")" \
                     "$(echo -e "${GREEN}[x]${NC} Dev Tools")" \
                     "$(echo -e "${RED}[ ]${NC} System (SUDO)")" \
                     "Done - Run selected" \
                     "Dry-run mode" \
                     "Cancel"; do
        case "$option" in
            *JetBrains*)
                selected[jetbrains]=$(toggle_bool "${selected[jetbrains]}")
                ;;
            *Docker*)
                selected[docker]=$(toggle_bool "${selected[docker]}")
                ;;
            *Homebrew*)
                selected[homebrew]=$(toggle_bool "${selected[homebrew]}")
                ;;
            *Dev\ Tools*)
                selected[devtools]=$(toggle_bool "${selected[devtools]}")
                ;;
            *System*)
                selected[system]=$(toggle_bool "${selected[system]}")
                ;;
            "Done - Run selected")
                break
                ;;
            "Dry-run mode")
                DRY_RUN=true
                log_info "Dry-run mode enabled"
                break
                ;;
            "Cancel")
                log_info "Operation cancelled"
                exit 0
                ;;
            *)
                echo "Invalid option"
                ;;
        esac
    done

    # Build selected modules array
    SELECTED_MODULES=()
    for module in jetbrains docker homebrew devtools system; do
        if [[ "${selected[$module]}" == true ]]; then
            SELECTED_MODULES+=("$module")
        fi
    done

    if [[ ${#SELECTED_MODULES[@]} -eq 0 ]]; then
        log_error "No modules selected"
        exit 1
    fi

    log_success "Selected modules: ${SELECTED_MODULES[*]}"
}

# Toggle boolean value
toggle_bool() {
    local value="$1"
    if [[ "$value" == true ]]; then
        echo false
    else
        echo true
    fi
}
