#!/usr/bin/env bash
# devtools.sh - Development tools cache cleanup
# Frees disk space by removing cached dependencies and build artifacts

set -euo pipefail

# Prevent double-sourcing (important for test environments)
if [[ -n "${DEVSWEEP_DEVTOOLS_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_DEVTOOLS_LOADED=true

# ============================================================
# CONSTANTES
# ============================================================

readonly NODE_CACHE_SIZE_THRESHOLD_MB=1000
readonly GRADLE_OLD_VERSION_PATTERN="^[67]\."  # Versions 6.x and 7.x

# ============================================================
# MAVEN
# ============================================================

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
    log_info "Clearing Maven dependency cache..."

    if ! should_clean_maven_cache; then
        log_info "Maven cache is empty or doesn't exist"
        return 0
    fi

    local cache_size
    cache_size=$(estimate_maven_cache_size)
    log_info "Current Maven cache: $cache_size"

    if ! confirm_action "Clear Maven cache ($cache_size)?"; then
        log_info "Maven cache cleanup skipped"
        return 0
    fi

    safe_rm "$MAVEN_REPO_PATH" "Maven dependency cache"
    log_success "Maven cache cleared ($cache_size freed)"
}

# ============================================================
# GRADLE
# ============================================================

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
    log_info "Removing outdated Gradle caches..."

    if [[ ! -d "$GRADLE_CACHE_PATH" ]]; then
        log_info "Gradle cache not found"
        return 0
    fi

    local old_caches
    old_caches=$(identify_old_gradle_versions)

    if [[ -z "$old_caches" ]]; then
        log_info "No outdated Gradle caches to remove"
        return 0
    fi

    local count=0
    while IFS= read -r old_cache; do
        if [[ -n "$old_cache" ]] && [[ -d "$old_cache" ]]; then
            safe_rm "$old_cache" "Old Gradle cache: $(basename "$old_cache")"
            ((count++))
        fi
    done <<< "$old_caches"

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

# ============================================================
# NODE/NPM
# ============================================================

# ============================================================
# CACHE INFORMATION HELPERS
# ============================================================

# Get NPM cache information and append to summary
# Args: $1 = current cache_summary
# Returns: Updated cache_summary via stdout, sets has_cache_found=true if cache exists
function get_npm_cache_info() {
    local current_summary="$1"
    
    if [[ -d "$NPM_CACHE_PATH" ]]; then
        local npm_size=$(get_size "$NPM_CACHE_PATH")
        if [[ -n "$current_summary" ]]; then
            echo "$current_summary, NPM: $npm_size"
        else
            echo "NPM: $npm_size"
        fi
        has_cache_found=true
    else
        echo "$current_summary"
    fi
}

# Get Yarn cache information and append to summary  
# Args: $1 = current cache_summary
# Returns: Updated cache_summary via stdout, sets has_cache_found=true if cache exists
function get_yarn_cache_info() {
    local current_summary="$1"
    
    # Use default Yarn cache paths instead of running slow 'yarn cache dir'
    local yarn_cache_dirs=(
        "${HOME}/Library/Caches/Yarn"
        "${HOME}/.yarn/cache"
        "${HOME}/.cache/yarn"
    )
    
    for yarn_cache_dir in "${yarn_cache_dirs[@]}"; do
        if [[ -d "$yarn_cache_dir" ]]; then
            local yarn_size=$(get_size "$yarn_cache_dir")
            if [[ -n "$current_summary" ]]; then
                echo "$current_summary, Yarn: $yarn_size"
            else
                echo "Yarn: $yarn_size"
            fi
            has_cache_found=true
            return
        fi
    done
    
    echo "$current_summary"
}

# Get pnpm cache information and append to summary
# Args: $1 = current cache_summary  
# Returns: Updated cache_summary via stdout, sets has_cache_found=true if found
function get_pnpm_cache_info() {
    local current_summary="$1"
    
    if command -v pnpm >/dev/null 2>&1; then
        if [[ -n "$current_summary" ]]; then
            echo "$current_summary, pnpm"
        else
            echo "pnpm"
        fi
        has_cache_found=true
    else
        echo "$current_summary"
    fi
}

# Get pip cache information and append to summary
# Args: $1 = current cache_summary
# Returns: Updated cache_summary via stdout, sets has_cache_found=true if cache exists
function get_pip_cache_info() {
    local current_summary="$1"
    
    # Use default pip cache paths instead of running slow 'pip3 cache dir'
    local pip_cache_dirs=(
        "${HOME}/Library/Caches/pip"
        "${HOME}/.cache/pip"
    )
    
    for pip_cache_dir in "${pip_cache_dirs[@]}"; do
        if [[ -d "$pip_cache_dir" ]]; then
            local pip_size=$(get_size "$pip_cache_dir")
            if [[ -n "$current_summary" ]]; then
                echo "$current_summary, pip: $pip_size"
            else
                echo "pip: $pip_size"
            fi
            has_cache_found=true
            return
        fi
    done
    
    echo "$current_summary"
}

# Get poetry cache information and append to summary
# Args: $1 = current cache_summary
# Returns: Updated cache_summary via stdout, sets has_cache_found=true if found
function get_poetry_cache_info() {
    local current_summary="$1"
    
    if command -v poetry >/dev/null 2>&1; then
        if [[ -n "$current_summary" ]]; then
            echo "$current_summary, poetry"
        else
            echo "poetry"
        fi
        has_cache_found=true
    else
        echo "$current_summary"
    fi
}

# Limpia cachés de paquetes de Node.js
# Returns: 0 on success
function clear_node_package_caches() {
    log_info "Clearing Node.js package caches..."

    local cleaned=false
    local cache_summary=""
    has_cache_found=false

    # Build cache summary using helper functions
    cache_summary=$(get_npm_cache_info "$cache_summary")
    cache_summary=$(get_yarn_cache_info "$cache_summary")
    cache_summary=$(get_pnpm_cache_info "$cache_summary")

    # If no caches found, exit early
    if [[ "$has_cache_found" == false ]]; then
        log_info "No Node.js caches found"
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

    # Yarn cache - clean directories directly instead of slow yarn command
    local yarn_cache_dirs=(
        "${HOME}/Library/Caches/Yarn"
        "${HOME}/.yarn/cache"
        "${HOME}/.cache/yarn"
    )
    for yarn_cache_dir in "${yarn_cache_dirs[@]}"; do
        if [[ -d "$yarn_cache_dir" ]]; then
            safe_rm "$yarn_cache_dir" "Yarn cache"
            cleaned=true
        fi
    done

    # pnpm cache - clean directory directly instead of slow pnpm command
    local pnpm_cache_dirs=(
        "${HOME}/Library/pnpm/store"
        "${HOME}/.local/share/pnpm/store"
        "${HOME}/.pnpm-store"
    )
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

# ============================================================
# SDKMAN
# ============================================================

# Limpia archivos temporales de SDKMAN
# Returns: 0 on success
function clear_sdkman_temp_files() {
    log_info "Clearing SDKMAN temp files..."

    if [[ ! -d "$SDKMAN_TMP_PATH" ]]; then
        log_info "SDKMAN not found"
        return 0
    fi

    local sdkman_size
    sdkman_size=$(get_size "$SDKMAN_TMP_PATH")

    if [[ "$sdkman_size" == "0B" ]]; then
        log_info "SDKMAN temp is empty"
        return 0
    fi

    log_info "SDKMAN temp: $sdkman_size"

    if ! confirm_action "Clear SDKMAN temp files ($sdkman_size)?"; then
        log_info "SDKMAN temp cleanup skipped"
        return 0
    fi

    safe_rm "$SDKMAN_TMP_PATH" "SDKMAN temp files"
    log_success "SDKMAN temp files cleared ($sdkman_size freed)"
}

# ============================================================
# PYTHON
# ============================================================

# Limpia cachés de pip y poetry
# Returns: 0 on success
function clear_python_package_caches() {
    log_info "Clearing Python package caches..."

    local cleaned=false
    local cache_summary=""
    has_cache_found=false

    # Build cache summary using helper functions
    cache_summary=$(get_pip_cache_info "$cache_summary")
    cache_summary=$(get_poetry_cache_info "$cache_summary")

    # If no package managers found, exit early
    if [[ "$has_cache_found" == false ]]; then
        log_info "No Python package managers found"
        return 0
    fi

    # Ask for confirmation with cache information
    if ! confirm_action "Clear Python package caches ($cache_summary)?"; then
        log_info "Python cache cleanup skipped"
        return 0
    fi

    # pip cache - clean directories directly instead of slow pip command
    local pip_cache_dirs=(
        "${HOME}/Library/Caches/pip"
        "${HOME}/.cache/pip"
    )
    for pip_cache_dir in "${pip_cache_dirs[@]}"; do
        if [[ -d "$pip_cache_dir" ]]; then
            safe_rm "$pip_cache_dir" "pip cache"
            cleaned=true
        fi
    done

    # poetry cache - clean directories directly instead of slow poetry command
    local poetry_cache_dirs=(
        "${HOME}/Library/Caches/pypoetry"
        "${HOME}/.cache/pypoetry"
    )
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

# ============================================================
# PUNTO DE ENTRADA
# ============================================================

# Limpieza de herramientas de desarrollo
# Usage: devtools_clean
# Returns: 0 on success
function devtools_clean() {
    log_section "Development Tools Cleanup"

    # Maven
    clear_maven_dependency_cache
    echo ""

    # Gradle
    remove_outdated_gradle_caches
    echo ""
    
    # Optional: Clear complete Gradle cache
    if [[ "$FORCE" != true ]] && [[ -d "$GRADLE_CACHE_PATH" ]]; then
        clear_gradle_cache_completely
        echo ""
    fi

    # Node.js
    clear_node_package_caches
    echo ""

    # NVM
    clear_nvm_cache
    echo ""

    # SDKMAN
    clear_sdkman_temp_files
    echo ""

    # Python
    clear_python_package_caches

    log_success "Development tools cleanup completed"
    return 0
}

# Public API: devtools_clean
