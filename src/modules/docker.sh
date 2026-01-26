#!/usr/bin/env bash
# docker.sh - Docker & OrbStack cleanup module
# Reclaims disk space by removing unused containers, images, and volumes

set -euo pipefail

# Prevent double-sourcing (important for test environments)
if [[ -n "${DEVSWEEP_DOCKER_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_DOCKER_LOADED=true

# ============================================================
# CONSTANTES
# ============================================================

readonly MINIMUM_FREE_SPACE_GB=5

# ============================================================
# VALIDACIÓN
# ============================================================

# Verifica si Docker está instalado y ejecutándose
# Returns: 0 if Docker is available, 1 otherwise
function is_docker_available() {
    command -v docker >/dev/null 2>&1 && docker ps >/dev/null 2>&1
}

# Verifica si OrbStack está instalado
# Returns: 0 if OrbStack is installed, 1 otherwise
function is_orbstack_installed() {
    [[ -d "${HOME}/.orbstack" ]] || command -v orbctl >/dev/null 2>&1
}

# Calcula cuánto espacio se puede liberar
# Returns: Estimated reclaimable space in MB
function estimate_reclaimable_docker_space() {
    if ! is_docker_available; then
        echo "0"
        return
    fi

    local total_bytes=0

    # Containers stopped
    local stopped_size
    stopped_size=$(docker ps -a --filter "status=exited" -q 2>/dev/null | wc -l | tr -d ' ')

    # Images unused
    local unused_images
    unused_images=$(docker images -f "dangling=true" -q 2>/dev/null | wc -l | tr -d ' ')

    # Rough estimate: 100MB per container, 500MB per image
    echo $(( (stopped_size * 100) + (unused_images * 500) ))
}

# ============================================================
# LIMPIEZA DE DOCKER
# ============================================================

# Detiene y remueve contenedores que no están en uso
# Returns: 0 on success
function remove_stopped_containers() {
    log_info "Removing stopped containers..."

    if ! is_docker_available; then
        log_warn "Docker is not running or not installed"
        return 0
    fi

    local stopped_count
    stopped_count=$(docker ps -a --filter "status=exited" -q 2>/dev/null | wc -l | tr -d ' ')

    if [[ "$stopped_count" -eq 0 ]]; then
        log_info "No stopped containers to remove"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would remove $stopped_count stopped containers"
        return 0
    fi

    docker container prune -f 2>/dev/null || {
        log_warn "Some containers could not be removed"
        return 0
    }

    log_success "Removed $stopped_count stopped containers"
}

# Remueve imágenes de Docker que no están en uso
# Returns: 0 on success
function remove_dangling_docker_images() {
    log_info "Removing dangling Docker images..."

    if ! is_docker_available; then
        return 0
    fi

    local dangling_count
    dangling_count=$(docker images -f "dangling=true" -q 2>/dev/null | wc -l | tr -d ' ')

    if [[ "$dangling_count" -eq 0 ]]; then
        log_info "No dangling images to remove"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would remove $dangling_count dangling images"
        return 0
    fi

    docker image prune -f 2>/dev/null || {
        log_warn "Some images could not be removed"
        return 0
    }

    log_success "Removed $dangling_count dangling images"
}

# Limpia volúmenes de Docker no utilizados
# Returns: 0 on success
function prune_unused_docker_volumes() {
    log_info "Pruning unused Docker volumes..."

    if ! is_docker_available; then
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        local volume_count
        volume_count=$(docker volume ls -q 2>/dev/null | wc -l | tr -d ' ')
        log_info "[DRY-RUN] Would prune unused volumes (total: $volume_count)"
        return 0
    fi

    docker volume prune -f 2>/dev/null || {
        log_warn "Some volumes could not be pruned"
        return 0
    }

    log_success "Docker volumes pruned"
}

# Limpia la caché de construcción de Docker
# Returns: 0 on success
function clear_docker_build_cache() {
    log_info "Clearing Docker build cache..."

    if ! is_docker_available; then
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would clear Docker build cache"
        return 0
    fi

    docker builder prune -af 2>/dev/null || {
        log_warn "Build cache could not be cleared"
        return 0
    }

    log_success "Docker build cache cleared"
}

# ============================================================
# LIMPIEZA PROFUNDA (DESTRUCTIVA)
# ============================================================

# Resetea completamente Docker Desktop eliminando todos los datos
# Returns: 0 on success, 1 if cancelled
function reset_docker_desktop_data() {
    log_warn "This will DELETE ALL Docker data (containers, images, volumes)"

    if ! confirm_dangerous "Reset Docker Desktop (delete all data)"; then
        log_info "Docker reset cancelled"
        return 0
    fi

    # Stop Docker first
    if pgrep -f "Docker" >/dev/null 2>&1; then
        log_info "Stopping Docker Desktop..."
        safe_kill "Docker"
        sleep 3
    fi

    # Delete data directory
    if [[ -d "$DOCKER_CONTAINER_PATH" ]]; then
        safe_rm "$DOCKER_CONTAINER_PATH" "Docker Desktop data"
        log_success "Docker Desktop data removed"
    else
        log_info "Docker Desktop data directory not found"
    fi
}

# Limpia datos de OrbStack
# Returns: 0 on success
function cleanup_orbstack_data() {
    if ! is_orbstack_installed; then
        log_debug "OrbStack not installed"
        return 0
    fi

    log_info "OrbStack detected"

    if ! confirm_action "Clean OrbStack cache?"; then
        return 0
    fi

    # Stop OrbStack if running
    if pgrep -f "OrbStack" >/dev/null 2>&1; then
        log_info "Stopping OrbStack..."
        safe_kill "OrbStack"
        sleep 2
    fi

    # Clean OrbStack cache if exists
    local orbstack_cache="${HOME}/.orbstack/cache"
    if [[ -d "$orbstack_cache" ]]; then
        safe_rm "$orbstack_cache" "OrbStack cache"
    fi

    log_success "OrbStack cleanup completed"
}

# ============================================================
# PUNTO DE ENTRADA
# ============================================================

# Limpieza estándar de Docker (segura)
# Returns: 0 on success
function cleanup_docker_safely() {
    log_section "Docker Cleanup (Safe Mode)"

    if ! is_docker_available; then
        log_warn "Docker is not running or not installed"
        log_info "Skipping Docker cleanup"
        return 0
    fi

    # Estimate space
    local reclaimable
    reclaimable=$(estimate_reclaimable_docker_space)
    log_info "Estimated reclaimable space: ${reclaimable}MB"

    # Safe cleanup operations
    remove_stopped_containers
    remove_dangling_docker_images
    prune_unused_docker_volumes
    clear_docker_build_cache

    log_success "Docker cleanup completed"
}

# Punto de entrada principal con opción de reset completo
# Usage: docker_clean
# Returns: 0 on success
function docker_clean() {
    log_section "Docker & OrbStack Cleanup"

    # Option 1: Safe cleanup
    cleanup_docker_safely

    echo ""

    # Option 2: Deep cleanup (ask user)
    if is_docker_available && [[ "$FORCE" != true ]]; then
        log_warn "Advanced: Complete Docker Desktop reset available"
        log_warn "This will delete ALL containers, images, and volumes"
        echo ""

        if confirm_action "Perform complete Docker reset? (DESTRUCTIVE)"; then
            reset_docker_desktop_data
        fi
    fi

    # Option 3: OrbStack
    if is_orbstack_installed; then
        echo ""
        cleanup_orbstack_data
    fi

    log_success "Docker module completed"
    return 0
}

# Public API: docker_clean
