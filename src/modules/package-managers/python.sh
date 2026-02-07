#!/usr/bin/env bash
# python.sh - Python package caches cleanup module (pip, poetry)

set -euo pipefail

# Prevent double-sourcing
if [[ -n "${DEVSWEEP_PYTHON_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_PYTHON_LOADED=true

# Limpia cachés de pip y poetry
# Returns: 0 on success
function clear_python_package_caches() {
    log_cleanup_info "Clearing Python package caches..."

    local cleaned=false
    local cache_summary=""
    local has_cache=false
    local detected_pip_cache=""
    local detected_poetry_cache=""

    # Check if pip cache exists
    local pip_cache_dirs=(
        "${HOME}/Library/Caches/pip"
        "${HOME}/.cache/pip"
    )
    for pip_cache_dir in "${pip_cache_dirs[@]}"; do
        if [[ -d "$pip_cache_dir" ]]; then
            detected_pip_cache="$pip_cache_dir"
            local pip_size=$(get_size "$pip_cache_dir")
            cache_summary="pip: $pip_size"
            has_cache=true
            break
        fi
    done

    # Check if poetry cache exists
    local poetry_cache_dirs=(
        "${HOME}/Library/Caches/pypoetry"
        "${HOME}/.cache/pypoetry"
    )
    for poetry_cache_dir in "${poetry_cache_dirs[@]}"; do
        if [[ -d "$poetry_cache_dir" ]]; then
            detected_poetry_cache="$poetry_cache_dir"
            if [[ -n "$cache_summary" ]]; then
                cache_summary="$cache_summary, poetry"
            else
                cache_summary="poetry"
            fi
            has_cache=true
            break
        fi
    done

    # If no package managers found, exit early
    if [[ "$has_cache" == false ]]; then
        log_cleanup_info "No Python package managers found"
        return 0
    fi

    # Register for analysis if in analyze mode
    if [[ "$ANALYZE_MODE" == true ]]; then
        if [[ -n "$detected_pip_cache" ]]; then
            register_if_analyzing "Dev Tools" "pip cache" "$detected_pip_cache"
        fi
        if [[ -n "$detected_poetry_cache" ]]; then
            register_if_analyzing "Dev Tools" "poetry cache" "$detected_poetry_cache"
        fi
        return 0
    fi

    # Ask for confirmation with cache information
    if ! confirm_action "Clear Python package caches ($cache_summary)?"; then
        log_info "Python cache cleanup skipped"
        return 0
    fi

    # pip cache
    for pip_cache_dir in "${pip_cache_dirs[@]}"; do
        if [[ -d "$pip_cache_dir" ]]; then
            safe_rm "$pip_cache_dir" "pip cache"
            cleaned=true
        fi
    done

    # poetry cache
    for poetry_cache_dir in "${poetry_cache_dirs[@]}"; do
        if [[ -d "$poetry_cache_dir" ]]; then
            safe_rm "$poetry_cache_dir" "poetry cache"
            cleaned=true
        fi
    done

    if [[ "$cleaned" == true ]]; then
        log_success "Python caches cleared"
    else
        log_info "No Python package managers found"
    fi
}

# Nuclear cleanup for Python - removes pip and poetry caches including virtualenvs
# Returns: 0 on success
function nuclear_python_clean() {
    local has_targets=false
    
    # Check pip
    local pip_cache_dirs=(
        "${HOME}/Library/Caches/pip"
        "${HOME}/.cache/pip"
    )
    for pip_cache_dir in "${pip_cache_dirs[@]}"; do
        if [[ -d "$pip_cache_dir" ]]; then
            has_targets=true
            break
        fi
    done
    
    # Check poetry
    local poetry_cache_dirs=(
        "${HOME}/Library/Caches/pypoetry"
        "${HOME}/.cache/pypoetry"
    )
    for poetry_cache_dir in "${poetry_cache_dirs[@]}"; do
        if [[ -d "$poetry_cache_dir" ]]; then
            has_targets=true
            break
        fi
    done
    
    if [[ "$has_targets" == false ]]; then
        return 0
    fi
    
    # Skip confirmation if already confirmed at orchestrator level
    if [[ "${NUCLEAR_CONFIRMED:-false}" == "true" ]]; then
        # pip (all locations)
        for pip_cache_dir in "${pip_cache_dirs[@]}"; do
            if [[ -d "$pip_cache_dir" ]]; then
                safe_rm "$pip_cache_dir" "pip cache (nuclear)"
            fi
        done
        
        # poetry (all locations, includes virtualenvs)
        for poetry_cache_dir in "${poetry_cache_dirs[@]}"; do
            if [[ -d "$poetry_cache_dir" ]]; then
                safe_rm "$poetry_cache_dir" "poetry cache with virtualenvs (nuclear)"
            fi
        done
        
        return 0
    fi
    
    # Fallback: individual confirmation (shouldn't reach here in normal flow)
    local pip_size=""
    for pip_cache_dir in "${pip_cache_dirs[@]}"; do
        if [[ -d "$pip_cache_dir" ]]; then
            pip_size=$(get_size "$pip_cache_dir")
            break
        fi
    done
    
    if confirm_nuclear "Delete pip cache ($pip_size) and poetry (including virtualenvs)"; then
        # pip (all locations)
        for pip_cache_dir in "${pip_cache_dirs[@]}"; do
            if [[ -d "$pip_cache_dir" ]]; then
                safe_rm "$pip_cache_dir" "pip cache (nuclear)"
            fi
        done
        
        # poetry (all locations, includes virtualenvs)
        for poetry_cache_dir in "${poetry_cache_dirs[@]}"; do
            if [[ -d "$poetry_cache_dir" ]]; then
                safe_rm "$poetry_cache_dir" "poetry cache with virtualenvs (nuclear)"
            fi
        done
        
        return 0
    fi
    
    return 1
}
