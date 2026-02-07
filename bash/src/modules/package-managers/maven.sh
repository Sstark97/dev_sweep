#!/usr/bin/env bash
# maven.sh - Maven cache cleanup module

set -euo pipefail

# Prevent double-sourcing
if [[ -n "${DEVSWEEP_MAVEN_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_MAVEN_LOADED=true

# Estima el tamaño de la caché de Maven
# Returns: Cache size or "0B"
function estimate_maven_cache_size() {
    if [[ ! -d "$MAVEN_REPO_PATH" ]]; then
        echo "0B"
        return
    fi
    get_size "$MAVEN_REPO_PATH"
}

# Verifica si se debe limpiar la caché de Maven
# Returns: 0 if should clean, 1 otherwise
function should_clean_maven_cache() {
    [[ -d "$MAVEN_REPO_PATH" ]] && is_dir_not_empty "$MAVEN_REPO_PATH"
}

# Limpia la caché de dependencias de Maven
# Returns: 0 on success
function clear_maven_dependency_cache() {
    log_cleanup_info "Clearing Maven dependency cache..."

    if ! should_clean_maven_cache; then
        log_cleanup_info "Maven cache is empty or doesn't exist"
        return 0
    fi

    local cache_size
    cache_size=$(estimate_maven_cache_size)

    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "Dev Tools" "Maven cache" "$cache_size"
        return 0
    fi

    log_info "Current Maven cache: $cache_size"

    if ! confirm_action "Clear Maven cache ($cache_size)?"; then
        log_info "Maven cache cleanup skipped"
        return 0
    fi

    safe_rm "$MAVEN_REPO_PATH" "Maven dependency cache"
    log_success "Maven cache cleared ($cache_size freed)"
}

# Nuclear cleanup for Maven - removes entire repository
# Returns: 0 on success
function nuclear_maven_clean() {
    if [[ ! -d "$MAVEN_REPO_PATH" ]]; then
        return 0
    fi
    
    # Skip confirmation if already confirmed at orchestrator level
    if [[ "${NUCLEAR_CONFIRMED:-false}" == "true" ]]; then
        safe_rm "$MAVEN_REPO_PATH" "Maven repository (nuclear)"
        return 0
    fi
    
    # Fallback: individual confirmation (shouldn't reach here in normal flow)
    local cache_size=$(get_size "$MAVEN_REPO_PATH")
    if confirm_nuclear "Delete entire Maven repository ($cache_size)"; then
        safe_rm "$MAVEN_REPO_PATH" "Maven repository (nuclear)"
        return 0
    fi
    
    return 1
}
