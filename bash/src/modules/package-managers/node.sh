#!/usr/bin/env bash
# node.sh - Node.js package caches and NVM cleanup module

set -euo pipefail

# Prevent double-sourcing
if [[ -n "${DEVSWEEP_NODE_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_NODE_LOADED=true

# Limpia cachés de paquetes de Node.js (npm, yarn, pnpm)
# Returns: 0 on success
function clear_node_package_caches() {
    log_cleanup_info "Clearing Node.js package caches..."

    local cleaned=false
    local cache_summary=""
    local has_cache=false

    # Check if any Node.js caches exist
    if [[ -d "$NPM_CACHE_PATH" ]]; then
        local npm_size=$(get_size "$NPM_CACHE_PATH")
        cache_summary="npm: $npm_size"
        has_cache=true
    fi

    local yarn_cache_dirs=(
        "${HOME}/Library/Caches/Yarn"
        "${HOME}/.yarn/cache"
        "${HOME}/.cache/yarn"
    )
    for yarn_cache_dir in "${yarn_cache_dirs[@]}"; do
        if [[ -d "$yarn_cache_dir" ]]; then
            local yarn_size=$(get_size "$yarn_cache_dir")
            if [[ -n "$cache_summary" ]]; then
                cache_summary="$cache_summary, yarn: $yarn_size"
            else
                cache_summary="yarn: $yarn_size"
            fi
            has_cache=true
            break
        fi
    done

    local pnpm_cache_dirs=(
        "${HOME}/Library/pnpm/store"
        "${HOME}/.local/share/pnpm/store"
        "${HOME}/.pnpm-store"
    )
    for pnpm_cache_dir in "${pnpm_cache_dirs[@]}"; do
        if [[ -d "$pnpm_cache_dir" ]]; then
            local pnpm_size=$(get_size "$pnpm_cache_dir")
            if [[ -n "$cache_summary" ]]; then
                cache_summary="$cache_summary, pnpm: $pnpm_size"
            else
                cache_summary="pnpm: $pnpm_size"
            fi
            has_cache=true
            break
        fi
    done

    # If no caches found, exit early
    if [[ "$has_cache" == false ]]; then
        log_cleanup_info "No Node.js caches found"
        return 0
    fi

    # In analyze mode, collect info
    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "Dev Tools" "Node.js caches ($cache_summary)" "calculated"
        return 0
    fi

    # Ask for confirmation with complete cache information
    if ! confirm_action "Clear Node.js caches ($cache_summary)?"; then
        log_info "Node.js cache cleanup skipped"
        return 0
    fi

    # NPM cache
    if [[ -d "$NPM_CACHE_PATH" ]]; then
        safe_rm "$NPM_CACHE_PATH" "NPM cache"
        cleaned=true
    fi

    # Yarn cache
    for yarn_cache_dir in "${yarn_cache_dirs[@]}"; do
        if [[ -d "$yarn_cache_dir" ]]; then
            safe_rm "$yarn_cache_dir" "Yarn cache"
            cleaned=true
        fi
    done

    # pnpm cache
    for pnpm_cache_dir in "${pnpm_cache_dirs[@]}"; do
        if [[ -d "$pnpm_cache_dir" ]]; then
            safe_rm "$pnpm_cache_dir" "pnpm store"
            cleaned=true
        fi
    done

    if [[ "$cleaned" == true ]]; then
        log_success "Node.js caches cleared"
    else
        log_info "No Node.js caches found"
    fi
}

# Limpia caché de NVM (Node Version Manager)
# Returns: 0 on success
function clear_nvm_cache() {
    log_info "Clearing NVM cache..."

    if [[ ! -d "$NVM_CACHE_PATH" ]]; then
        log_info "NVM cache not found"
        return 0
    fi

    local nvm_size
    nvm_size=$(get_size "$NVM_CACHE_PATH")

    if [[ "$nvm_size" == "0B" ]]; then
        log_info "NVM cache is empty"
        return 0
    fi

    log_info "NVM cache: $nvm_size"

    if ! confirm_action "Clear NVM cache ($nvm_size)?"; then
        log_info "NVM cache cleanup skipped"
        return 0
    fi

    safe_rm "$NVM_CACHE_PATH" "NVM cache"
    log_success "NVM cache cleared ($nvm_size freed)"
}

# Nuclear cleanup for Node.js - removes complete npm, yarn, and pnpm directories
# Returns: 0 on success
function nuclear_node_clean() {
    local has_targets=false
    
    # Check npm
    if [[ -d "$NPM_FULL_PATH" ]]; then
        has_targets=true
    fi
    
    # Check yarn
    local yarn_cache_dirs=(
        "${HOME}/Library/Caches/Yarn"
        "${HOME}/.yarn/cache"
        "${HOME}/.cache/yarn"
    )
    for yarn_cache_dir in "${yarn_cache_dirs[@]}"; do
        if [[ -d "$yarn_cache_dir" ]]; then
            has_targets=true
            break
        fi
    done
    
    # Check pnpm
    local pnpm_cache_dirs=(
        "${HOME}/Library/pnpm/store"
        "${HOME}/.local/share/pnpm/store"
        "${HOME}/.pnpm-store"
    )
    for pnpm_cache_dir in "${pnpm_cache_dirs[@]}"; do
        if [[ -d "$pnpm_cache_dir" ]]; then
            has_targets=true
            break
        fi
    done
    
    if [[ "$has_targets" == false ]]; then
        return 0
    fi
    
    # Skip confirmation if already confirmed at orchestrator level
    if [[ "${NUCLEAR_CONFIRMED:-false}" == "true" ]]; then
        # npm (complete directory)
        if [[ -d "$NPM_FULL_PATH" ]]; then
            safe_rm "$NPM_FULL_PATH" "npm directory (nuclear)"
        fi
        
        # Yarn (all locations)
        for yarn_cache_dir in "${yarn_cache_dirs[@]}"; do
            if [[ -d "$yarn_cache_dir" ]]; then
                safe_rm "$yarn_cache_dir" "Yarn cache (nuclear)"
            fi
        done
        
        # pnpm (all locations)
        for pnpm_cache_dir in "${pnpm_cache_dirs[@]}"; do
            if [[ -d "$pnpm_cache_dir" ]]; then
                safe_rm "$pnpm_cache_dir" "pnpm store (nuclear)"
            fi
        done
        
        return 0
    fi
    
    # Fallback: individual confirmation (shouldn't reach here in normal flow)
    local npm_size=$(get_size "$NPM_FULL_PATH" 2>/dev/null || echo "0B")
    
    if confirm_nuclear "Delete complete npm ($npm_size), yarn, and pnpm directories"; then
        # npm (complete directory)
        if [[ -d "$NPM_FULL_PATH" ]]; then
            safe_rm "$NPM_FULL_PATH" "npm directory (nuclear)"
        fi
        
        # Yarn (all locations)
        for yarn_cache_dir in "${yarn_cache_dirs[@]}"; do
            if [[ -d "$yarn_cache_dir" ]]; then
                safe_rm "$yarn_cache_dir" "Yarn cache (nuclear)"
            fi
        done
        
        # pnpm (all locations)
        for pnpm_cache_dir in "${pnpm_cache_dirs[@]}"; do
            if [[ -d "$pnpm_cache_dir" ]]; then
                safe_rm "$pnpm_cache_dir" "pnpm store (nuclear)"
            fi
        done
        
        return 0
    fi
    
    return 1
}
