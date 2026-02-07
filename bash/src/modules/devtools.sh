#!/usr/bin/env bash
# devtools.sh - Development tools cache cleanup orchestrator
# Coordinates cleanup across all package manager modules

set -euo pipefail

# Prevent double-sourcing (important for test environments)
if [[ -n "${DEVSWEEP_DEVTOOLS_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_DEVTOOLS_LOADED=true

# ============================================================
# SOURCE PACKAGE MANAGER MODULES
# ============================================================

DEVTOOLS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

source "$DEVTOOLS_DIR/package-managers/maven.sh"
source "$DEVTOOLS_DIR/package-managers/gradle.sh"
source "$DEVTOOLS_DIR/package-managers/node.sh"
source "$DEVTOOLS_DIR/package-managers/python.sh"
source "$DEVTOOLS_DIR/package-managers/sdkman.sh"

# ============================================================
# NUCLEAR MODE ORCHESTRATOR
# ============================================================

# Nuclear cleanup orchestrator - coordinates complete cache destruction across all modules
# Returns: 0 on success, 1 if cancelled
function nuclear_devtools_clean() {
    log_cleanup_section "NUCLEAR MODE - Development Tools"
    
    # Collect sizes from all modules for summary
    local summary_items=()
    local has_targets=false
    
    # Maven
    if [[ -d "$MAVEN_REPO_PATH" ]]; then
        local maven_size=$(get_size "$MAVEN_REPO_PATH")
        summary_items+=("Maven: $maven_size")
        has_targets=true
    fi
    
    # Gradle (caches + wrapper)
    if [[ -d "$GRADLE_CACHE_PATH" ]] || [[ -d "$GRADLE_WRAPPER_PATH" ]]; then
        local gradle_cache_size=$(get_size "$GRADLE_CACHE_PATH" 2>/dev/null || echo "0B")
        local gradle_wrapper_size=$(get_size "$GRADLE_WRAPPER_PATH" 2>/dev/null || echo "0B")
        summary_items+=("Gradle: $gradle_cache_size (caches) + $gradle_wrapper_size (wrapper)")
        has_targets=true
    fi
    
    # Node.js (npm + yarn + pnpm)
    if [[ -d "$NPM_FULL_PATH" ]]; then
        local npm_size=$(get_size "$NPM_FULL_PATH")
        summary_items+=("npm: $npm_size (complete directory)")
        has_targets=true
    fi
    
    # Yarn
    local yarn_dirs=("${HOME}/Library/Caches/Yarn" "${HOME}/.yarn/cache" "${HOME}/.cache/yarn")
    for yarn_dir in "${yarn_dirs[@]}"; do
        if [[ -d "$yarn_dir" ]]; then
            local yarn_size=$(get_size "$yarn_dir")
            summary_items+=("Yarn: $yarn_size")
            has_targets=true
            break
        fi
    done
    
    # pnpm
    local pnpm_dirs=("${HOME}/Library/pnpm/store" "${HOME}/.local/share/pnpm/store" "${HOME}/.pnpm-store")
    for pnpm_dir in "${pnpm_dirs[@]}"; do
        if [[ -d "$pnpm_dir" ]]; then
            local pnpm_size=$(get_size "$pnpm_dir")
            summary_items+=("pnpm: $pnpm_size")
            has_targets=true
            break
        fi
    done
    
    # Python (pip + poetry with virtualenvs)
    local pip_dirs=("${HOME}/Library/Caches/pip" "${HOME}/.cache/pip")
    for pip_dir in "${pip_dirs[@]}"; do
        if [[ -d "$pip_dir" ]]; then
            local pip_size=$(get_size "$pip_dir")
            summary_items+=("pip: $pip_size")
            has_targets=true
            break
        fi
    done
    
    local poetry_dirs=("${HOME}/Library/Caches/pypoetry" "${HOME}/.cache/pypoetry")
    for poetry_dir in "${poetry_dirs[@]}"; do
        if [[ -d "$poetry_dir" ]]; then
            local poetry_size=$(get_size "$poetry_dir")
            summary_items+=("poetry: $poetry_size (includes virtualenvs)")
            has_targets=true
            break
        fi
    done
    
    # Guard clause: nothing to clean
    if [[ "$has_targets" == false ]]; then
        log_cleanup_info "No development tool caches found"
        return 0
    fi
    
    # Analyze mode
    if [[ "$ANALYZE_MODE" == true ]]; then
        [[ -d "$MAVEN_REPO_PATH" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Maven repository" "$MAVEN_REPO_PATH"
        [[ -d "$GRADLE_CACHE_PATH" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Gradle caches" "$GRADLE_CACHE_PATH"
        [[ -d "$GRADLE_WRAPPER_PATH" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Gradle wrapper" "$GRADLE_WRAPPER_PATH"
        [[ -d "$NPM_FULL_PATH" ]] && register_if_analyzing "Dev Tools (Nuclear)" "npm complete" "$NPM_FULL_PATH"
        
        for yarn_dir in "${yarn_dirs[@]}"; do
            [[ -d "$yarn_dir" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Yarn cache" "$yarn_dir" && break
        done
        
        for pnpm_dir in "${pnpm_dirs[@]}"; do
            [[ -d "$pnpm_dir" ]] && register_if_analyzing "Dev Tools (Nuclear)" "pnpm store" "$pnpm_dir" && break
        done
        
        for pip_dir in "${pip_dirs[@]}"; do
            [[ -d "$pip_dir" ]] && register_if_analyzing "Dev Tools (Nuclear)" "pip cache" "$pip_dir" && break
        done
        
        for poetry_dir in "${poetry_dirs[@]}"; do
            [[ -d "$poetry_dir" ]] && register_if_analyzing "Dev Tools (Nuclear)" "poetry complete" "$poetry_dir" && break
        done
        
        return 0
    fi
    
    # Show summary of what will be deleted
    log_warn "The following will be COMPLETELY DELETED:"
    echo ""
    for item in "${summary_items[@]}"; do
        echo "  • $item"
    done
    echo ""
    
    # Single confirmation (NEVER auto-confirms)
    if ! confirm_nuclear "Delete ALL development tool caches"; then
        return 1
    fi
    
    # Set flag to skip individual module confirmations
    export NUCLEAR_CONFIRMED=true
    
    # Execute cleanup by calling each module's nuclear function
    nuclear_maven_clean
    nuclear_gradle_clean
    nuclear_node_clean
    nuclear_python_clean
    
    # Cleanup flag
    unset NUCLEAR_CONFIRMED
    
    log_success "Nuclear cleanup completed - ALL development tool caches destroyed"
    return 0
}

# ============================================================
# PUNTO DE ENTRADA
# ============================================================

# Limpieza de herramientas de desarrollo
# Usage: devtools_clean
# Returns: 0 on success
function devtools_clean() {
    log_cleanup_section "Development Tools Cleanup"

    # Maven
    clear_maven_dependency_cache
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    # Gradle
    remove_outdated_gradle_caches
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    # Optional: Clear complete Gradle cache (skip in analyze mode)
    if [[ "$ANALYZE_MODE" != true ]] && [[ "$FORCE" != true ]] && [[ -d "$GRADLE_CACHE_PATH" ]]; then
        clear_gradle_cache_completely
        echo ""
    fi

    # Node.js
    clear_node_package_caches
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    # NVM
    clear_nvm_cache
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    # SDKMAN
    clear_sdkman_temp_files
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    # Python
    clear_python_package_caches

    if [[ "$ANALYZE_MODE" != true ]]; then
        log_success "Development tools cleanup completed"
    fi
    return 0
}

# Public API: devtools_clean
