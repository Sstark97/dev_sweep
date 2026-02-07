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
# NUCLEAR MODE
# ============================================================

# Limpieza nuclear - elimina TODOS los cachés sin excepciones
# Returns: 0 on success, 1 if cancelled
function nuclear_devtools_clean() {
    log_cleanup_section "NUCLEAR MODE - Development Tools"
    
    # Collect paths and calculate sizes
    local gradle_size=""
    local npm_size=""
    local cargo_size=""
    local composer_size=""
    local pip_size=""
    local poetry_size=""
    local maven_size=""
    
    local has_targets=false
    
    # Check Gradle (caches + wrapper)
    if [[ -d "$GRADLE_CACHE_PATH" ]] || [[ -d "$GRADLE_WRAPPER_PATH" ]]; then
        has_targets=true
        local gradle_cache_size=$(get_size "$GRADLE_CACHE_PATH" 2>/dev/null || echo "0B")
        local gradle_wrapper_size=$(get_size "$GRADLE_WRAPPER_PATH" 2>/dev/null || echo "0B")
        gradle_size="Gradle: $gradle_cache_size (caches) + $gradle_wrapper_size (wrapper)"
    fi
    
    # Check npm (complete directory)
    if [[ -d "$NPM_FULL_PATH" ]]; then
        has_targets=true
        npm_size="npm: $(get_size "$NPM_FULL_PATH")"
    fi
    
    # Check Cargo
    if [[ -d "$CARGO_REGISTRY_PATH" ]] || [[ -d "$CARGO_GIT_PATH" ]]; then
        has_targets=true
        local cargo_registry_size=$(get_size "$CARGO_REGISTRY_PATH" 2>/dev/null || echo "0B")
        local cargo_git_size=$(get_size "$CARGO_GIT_PATH" 2>/dev/null || echo "0B")
        cargo_size="Cargo: $cargo_registry_size (registry) + $cargo_git_size (git)"
    fi
    
    # Check Composer
    if [[ -d "$COMPOSER_CACHE_PATH" ]]; then
        has_targets=true
        composer_size="Composer: $(get_size "$COMPOSER_CACHE_PATH")"
    fi
    
    # Check pip (both possible locations)
    local pip_found=false
    for pip_cache in "${HOME}/Library/Caches/pip" "${HOME}/.cache/pip"; do
        if [[ -d "$pip_cache" ]]; then
            pip_found=true
            pip_size="pip: $(get_size "$pip_cache")"
            break
        fi
    done
    if [[ "$pip_found" == true ]]; then
        has_targets=true
    fi
    
    # Check poetry (both possible locations)
    local poetry_found=false
    for poetry_cache in "${HOME}/Library/Caches/pypoetry" "${HOME}/.cache/pypoetry"; do
        if [[ -d "$poetry_cache" ]]; then
            poetry_found=true
            poetry_size="poetry: $(get_size "$poetry_cache") (includes virtualenvs)"
            break
        fi
    done
    if [[ "$poetry_found" == true ]]; then
        has_targets=true
    fi
    
    # Check Maven (included for completeness)
    if [[ -d "$MAVEN_REPO_PATH" ]]; then
        has_targets=true
        maven_size="Maven: $(get_size "$MAVEN_REPO_PATH")"
    fi
    
    # Guard clause: nothing to clean
    if [[ "$has_targets" == false ]]; then
        log_cleanup_info "No development tool caches found"
        return 0
    fi
    
    # Analyze mode
    if [[ "$ANALYZE_MODE" == true ]]; then
        [[ -n "$gradle_size" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Gradle complete" "$GRADLE_CACHE_PATH"
        [[ -d "$GRADLE_WRAPPER_PATH" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Gradle wrapper" "$GRADLE_WRAPPER_PATH"
        [[ -n "$npm_size" ]] && register_if_analyzing "Dev Tools (Nuclear)" "npm complete" "$NPM_FULL_PATH"
        [[ -n "$cargo_size" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Cargo registry" "$CARGO_REGISTRY_PATH"
        [[ -d "$CARGO_GIT_PATH" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Cargo git" "$CARGO_GIT_PATH"
        [[ -n "$composer_size" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Composer cache" "$COMPOSER_CACHE_PATH"
        [[ -n "$pip_size" ]] && register_if_analyzing "Dev Tools (Nuclear)" "pip cache" "${HOME}/Library/Caches/pip"
        [[ -n "$poetry_size" ]] && register_if_analyzing "Dev Tools (Nuclear)" "poetry complete" "${HOME}/Library/Caches/pypoetry"
        [[ -n "$maven_size" ]] && register_if_analyzing "Dev Tools (Nuclear)" "Maven repository" "$MAVEN_REPO_PATH"
        return 0
    fi
    
    # Show what will be deleted
    log_warn "The following will be COMPLETELY DELETED:"
    echo ""
    [[ -n "$gradle_size" ]] && echo "  • $gradle_size"
    [[ -n "$npm_size" ]] && echo "  • $npm_size"
    [[ -n "$cargo_size" ]] && echo "  • $cargo_size"
    [[ -n "$composer_size" ]] && echo "  • $composer_size"
    [[ -n "$pip_size" ]] && echo "  • $pip_size"
    [[ -n "$poetry_size" ]] && echo "  • $poetry_size"
    [[ -n "$maven_size" ]] && echo "  • $maven_size"
    echo ""
    
    # Confirm (NEVER auto-confirms)
    if ! confirm_nuclear "Delete ALL development tool caches"; then
        return 1
    fi
    
    # Execute cleanup
    local cleaned=false
    
    # Gradle
    if [[ -d "$GRADLE_CACHE_PATH" ]]; then
        safe_rm "$GRADLE_CACHE_PATH" "Gradle caches"
        cleaned=true
    fi
    if [[ -d "$GRADLE_WRAPPER_PATH" ]]; then
        safe_rm "$GRADLE_WRAPPER_PATH" "Gradle wrapper"
        cleaned=true
    fi
    
    # npm
    if [[ -d "$NPM_FULL_PATH" ]]; then
        safe_rm "$NPM_FULL_PATH" "npm directory"
        cleaned=true
    fi
    
    # Cargo
    if [[ -d "$CARGO_REGISTRY_PATH" ]]; then
        safe_rm "$CARGO_REGISTRY_PATH" "Cargo registry"
        cleaned=true
    fi
    if [[ -d "$CARGO_GIT_PATH" ]]; then
        safe_rm "$CARGO_GIT_PATH" "Cargo git"
        cleaned=true
    fi
    
    # Composer
    if [[ -d "$COMPOSER_CACHE_PATH" ]]; then
        safe_rm "$COMPOSER_CACHE_PATH" "Composer cache"
        cleaned=true
    fi
    
    # pip (both locations)
    for pip_cache in "${HOME}/Library/Caches/pip" "${HOME}/.cache/pip"; do
        if [[ -d "$pip_cache" ]]; then
            safe_rm "$pip_cache" "pip cache"
            cleaned=true
        fi
    done
    
    # poetry (both locations)
    for poetry_cache in "${HOME}/Library/Caches/pypoetry" "${HOME}/.cache/pypoetry"; do
        if [[ -d "$poetry_cache" ]]; then
            safe_rm "$poetry_cache" "poetry cache"
            cleaned=true
        fi
    done
    
    # Maven
    if [[ -d "$MAVEN_REPO_PATH" ]]; then
        safe_rm "$MAVEN_REPO_PATH" "Maven repository"
        cleaned=true
    fi
    
    if [[ "$cleaned" == true ]]; then
        log_success "Nuclear cleanup completed - ALL development tool caches destroyed"
    else
        log_info "No caches were found to clean"
    fi
    
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
