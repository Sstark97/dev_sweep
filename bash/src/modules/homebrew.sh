#!/usr/bin/env bash
# homebrew.sh - Homebrew package manager cleanup
# Frees disk space by removing outdated packages and clearing download caches

set -euo pipefail

# Prevent double-sourcing (important for test environments)
if [[ -n "${DEVSWEEP_HOMEBREW_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_HOMEBREW_LOADED=true

# ============================================================
# CONSTANTES
# ============================================================

readonly HOMEBREW_PRUNE_DAYS=30  # Remove downloads older than 30 days

# ============================================================
# VALIDACIÓN
# ============================================================

# Verifica si Homebrew está instalado
# Returns: 0 if Homebrew is available, 1 otherwise
function is_homebrew_installed() {
    command -v brew >/dev/null 2>&1
}

# Obtiene la ruta de la caché de Homebrew
# Returns: Cache path or empty string
function get_homebrew_cache_path() {
    if is_homebrew_installed; then
        brew --cache 2>/dev/null || echo ""
    else
        echo ""
    fi
}

# Estima cuánto espacio ocupa la caché de Homebrew
# Returns: Cache size or "0B"
function estimate_homebrew_cache_size() {
    local cache_path
    cache_path=$(get_homebrew_cache_path)

    if [[ -z "$cache_path" ]] || [[ ! -d "$cache_path" ]]; then
        echo "0B"
        return
    fi

    get_size "$cache_path"
}

# ============================================================
# LIMPIEZA DE PAQUETES
# ============================================================

# Remueve versiones desactualizadas de paquetes instalados
# Returns: 0 on success
function remove_outdated_package_versions() {
    log_cleanup_info "Removing outdated Homebrew packages..."

    if ! is_homebrew_installed; then
        log_cleanup_info "Homebrew is not installed"
        return 0
    fi

    # Check if there are any outdated packages
    local outdated_count
    outdated_count=$(brew list --versions 2>/dev/null | grep -c " " || echo "0")

    if [[ "$outdated_count" -eq 0 ]]; then
        log_cleanup_info "No outdated packages to remove"
        return 0
    fi

    if [[ "$ANALYZE_MODE" == true ]]; then
        # Estimate size of outdated packages
        add_analyze_item "Homebrew" "$outdated_count outdated packages" "~$((outdated_count * 50))MB"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would clean $outdated_count packages"
        brew cleanup --dry-run 2>/dev/null || true
        return 0
    fi

    log_debug "Running brew cleanup..."
    brew cleanup --prune="$HOMEBREW_PRUNE_DAYS" 2>/dev/null || {
        log_warn "Some packages could not be cleaned"
        return 0
    }

    log_success "Outdated packages removed"
}

# Remueve paquetes huérfanos (dependencies no utilizadas)
# Returns: 0 on success
function remove_unused_dependencies() {
    log_cleanup_info "Checking for unused dependencies..."

    if ! is_homebrew_installed; then
        return 0
    fi

    # Check for unused dependencies
    local unused_deps
    unused_deps=$(brew autoremove --dry-run 2>/dev/null | grep -c "^Would remove" 2>/dev/null || echo "0")
    # Remove all whitespace and newlines to get clean number
    unused_deps=$(echo "$unused_deps" | tr -d ' \n\r' || echo "0")

    if [[ "$unused_deps" -eq 0 ]] 2>/dev/null; then
        log_cleanup_info "No unused dependencies found"
        return 0
    fi

    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "Homebrew" "$unused_deps unused dependencies" "~$((unused_deps * 20))MB"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would remove $unused_deps unused dependencies"
        brew autoremove --dry-run 2>/dev/null || true
        return 0
    fi

    log_debug "Removing unused dependencies..."
    brew autoremove 2>/dev/null || {
        log_warn "Some dependencies could not be removed"
        return 0
    }

    log_success "Unused dependencies removed"
}

# ============================================================
# LIMPIEZA DE CACHÉ
# ============================================================

# Limpia la caché de descargas de Homebrew
# Returns: 0 on success
function clear_homebrew_download_cache() {
    log_cleanup_info "Clearing Homebrew download cache..."

    if ! is_homebrew_installed; then
        return 0
    fi

    local cache_size
    cache_size=$(estimate_homebrew_cache_size)

    if [[ "$cache_size" == "0B" ]]; then
        log_cleanup_info "Homebrew cache is empty"
        return 0
    fi

    if [[ "$ANALYZE_MODE" != true ]]; then
        log_info "Current cache size: $cache_size"
    fi

    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "Homebrew" "Download cache" "$cache_size"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would clear Homebrew cache ($cache_size)"
        return 0
    fi

    local cache_path
    cache_path=$(get_homebrew_cache_path)

    if [[ -n "$cache_path" ]] && [[ -d "$cache_path" ]]; then
        # Use brew cleanup -s to scrub cache
        log_debug "Scrubbing Homebrew cache..."
        brew cleanup -s 2>/dev/null || {
            log_warn "Cache could not be fully cleared"
            return 0
        }
        log_success "Homebrew cache cleared ($cache_size freed)"
    fi
}

# ============================================================
# ACTUALIZACIÓN Y DIAGNÓSTICO
# ============================================================

# Actualiza Homebrew y muestra estadísticas
# Returns: 0 on success
function update_and_diagnose_homebrew() {
    log_info "Updating Homebrew..."

    if ! is_homebrew_installed; then
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would update Homebrew"
        return 0
    fi

    # Update Homebrew
    log_debug "Running brew update..."
    brew update 2>/dev/null || {
        log_warn "Homebrew update failed"
        return 0
    }

    # Show diagnostics
    log_debug "Running brew doctor..."
    local doctor_output
    doctor_output=$(brew doctor 2>&1 || true)

    if echo "$doctor_output" | grep -q "Your system is ready to brew"; then
        log_success "Homebrew is healthy"
    else
        log_warn "Homebrew has some warnings (run 'brew doctor' for details)"
    fi
}

# ============================================================
# PUNTO DE ENTRADA
# ============================================================

# Limpieza completa de Homebrew
# Usage: homebrew_clean
# Returns: 0 on success
function homebrew_clean() {
    log_cleanup_section "Homebrew Cleanup"

    if ! is_homebrew_installed; then
        log_cleanup_info "Homebrew is not installed"
        log_cleanup_info "Skipping Homebrew cleanup"
        return 0
    fi

    # Show initial cache size (skip in analyze mode)
    if [[ "$ANALYZE_MODE" != true ]]; then
        local initial_cache_size
        initial_cache_size=$(estimate_homebrew_cache_size)
        log_info "Initial cache size: $initial_cache_size"
        echo ""
    fi

    # Cleanup operations
    remove_outdated_package_versions
    
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    remove_unused_dependencies
    
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
    fi

    clear_homebrew_download_cache

    # Skip update/diagnose in analyze mode
    if [[ "$ANALYZE_MODE" != true ]]; then
        echo ""
        # Optional: Update and diagnose
        if [[ "$VERBOSE" == true ]] && [[ "$DRY_RUN" != true ]]; then
            update_and_diagnose_homebrew
            echo ""
        fi
        log_success "Homebrew cleanup completed"
    fi
    
    return 0
}

# Public API: homebrew_clean
