#!/usr/bin/env bash
# gradle.sh - Gradle cache cleanup module

set -euo pipefail

# Prevent double-sourcing
if [[ -n "${DEVSWEEP_GRADLE_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_GRADLE_LOADED=true

readonly GRADLE_OLD_VERSION_PATTERN="^[67]\."  # Versions 6.x and 7.x

# Identifica versiones antiguas de caché de Gradle
# Returns: List of old cache directories
function identify_old_gradle_versions() {
    if [[ ! -d "$GRADLE_CACHE_PATH" ]]; then
        return 0
    fi

    find "$GRADLE_CACHE_PATH" -maxdepth 1 -type d -name "$GRADLE_OLD_VERSION_PATTERN*" 2>/dev/null || true
}

# Remueve cachés de versiones antiguas de Gradle
# Returns: 0 on success
function remove_outdated_gradle_caches() {
    log_cleanup_info "Removing outdated Gradle caches..."

    if [[ ! -d "$GRADLE_CACHE_PATH" ]]; then
        log_cleanup_info "Gradle cache not found"
        return 0
    fi

    local outdated_gradle_versions
    outdated_gradle_versions=$(identify_old_gradle_versions)

    if [[ -z "$outdated_gradle_versions" ]]; then
        log_cleanup_info "No outdated Gradle caches to remove"
        return 0
    fi

    # Register for analysis if in analyze mode
    if [[ "$ANALYZE_MODE" == true ]]; then
        while IFS= read -r gradle_cache_path; do
            if [[ -n "$gradle_cache_path" ]] && [[ -d "$gradle_cache_path" ]]; then
                local cache_version=$(basename "$gradle_cache_path")
                register_if_analyzing "Dev Tools" "Gradle $cache_version" "$gradle_cache_path"
            fi
        done <<< "$outdated_gradle_versions"
        return 0
    fi

    # Normal cleanup mode
    local count=0
    while IFS= read -r gradle_cache_path; do
        if [[ -n "$gradle_cache_path" ]] && [[ -d "$gradle_cache_path" ]]; then
            safe_rm "$gradle_cache_path" "Old Gradle cache: $(basename "$gradle_cache_path")"
            ((count++))
        fi
    done <<< "$outdated_gradle_versions"

    if [[ $count -gt 0 ]]; then
        log_success "Removed $count outdated Gradle cache(s)"
    fi
}

# Limpia la caché completa de Gradle (si el usuario lo solicita)
# Returns: 0 on success
function clear_gradle_cache_completely() {
    log_info "Clearing complete Gradle cache..."

    if [[ ! -d "$GRADLE_CACHE_PATH" ]]; then
        log_info "Gradle cache not found"
        return 0
    fi

    local cache_size
    cache_size=$(get_size "$GRADLE_CACHE_PATH")
    log_info "Current Gradle cache: $cache_size"

    if ! confirm_action "Clear complete Gradle cache ($cache_size)?"; then
        log_info "Gradle cache cleanup skipped"
        return 0
    fi

    safe_rm "$GRADLE_CACHE_PATH" "Gradle cache"
    log_success "Gradle cache cleared ($cache_size freed)"
}

# Nuclear cleanup for Gradle - removes caches AND wrapper
# Returns: 0 on success
function nuclear_gradle_clean() {
    local has_targets=false
    
    if [[ -d "$GRADLE_CACHE_PATH" ]] || [[ -d "$GRADLE_WRAPPER_PATH" ]]; then
        has_targets=true
    fi
    
    if [[ "$has_targets" == false ]]; then
        return 0
    fi
    
    # Skip confirmation if already confirmed at orchestrator level
    if [[ "${NUCLEAR_CONFIRMED:-false}" == "true" ]]; then
        if [[ -d "$GRADLE_CACHE_PATH" ]]; then
            safe_rm "$GRADLE_CACHE_PATH" "Gradle caches (nuclear)"
        fi
        if [[ -d "$GRADLE_WRAPPER_PATH" ]]; then
            safe_rm "$GRADLE_WRAPPER_PATH" "Gradle wrapper (nuclear)"
        fi
        return 0
    fi
    
    # Fallback: individual confirmation (shouldn't reach here in normal flow)
    local gradle_cache_size=$(get_size "$GRADLE_CACHE_PATH" 2>/dev/null || echo "0B")
    local gradle_wrapper_size=$(get_size "$GRADLE_WRAPPER_PATH" 2>/dev/null || echo "0B")
    
    if confirm_nuclear "Delete Gradle caches ($gradle_cache_size) and wrapper ($gradle_wrapper_size)"; then
        if [[ -d "$GRADLE_CACHE_PATH" ]]; then
            safe_rm "$GRADLE_CACHE_PATH" "Gradle caches (nuclear)"
        fi
        if [[ -d "$GRADLE_WRAPPER_PATH" ]]; then
            safe_rm "$GRADLE_WRAPPER_PATH" "Gradle wrapper (nuclear)"
        fi
        return 0
    fi
    
    return 1
}
