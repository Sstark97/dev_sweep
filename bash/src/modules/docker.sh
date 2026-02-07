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
    log_cleanup_info "Removing stopped containers..."

    if ! is_docker_available; then
        log_cleanup_info "Docker is not running or not installed"
        return 0
    fi

    local stopped_count
    stopped_count=$(docker ps -a --filter "status=exited" -q 2>/dev/null | wc -l | tr -d ' ')

    if [[ "$stopped_count" -eq 0 ]]; then
        log_cleanup_info "No stopped containers to remove"
        return 0
    fi

    # In analyze mode, collect info
    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "Docker" "$stopped_count stopped containers" "~$((stopped_count * 10))MB"
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
    log_cleanup_info "Removing dangling Docker images..."

    if ! is_docker_available; then
        return 0
    fi

    local dangling_count
    dangling_count=$(docker images -f "dangling=true" -q 2>/dev/null | wc -l | tr -d ' ')

    if [[ "$dangling_count" -eq 0 ]]; then
        log_cleanup_info "No dangling images to remove"
        return 0
    fi

    # In analyze mode, estimate size dynamically
    if [[ "$ANALYZE_MODE" == true ]]; then
        local estimated_size="~$((dangling_count * 100))MB"
        add_analyze_item "Docker" "$dangling_count dangling images" "$estimated_size"
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
# In ANALYZE_MODE: Counts but doesn't remove
# Returns: 0 on success
function prune_unused_docker_volumes() {
    if [[ "$ANALYZE_MODE" != true ]]; then
        log_info "Pruning unused Docker volumes..."
    fi

    if ! is_docker_available; then
        return 0
    fi

    local volume_count
    volume_count=$(docker volume ls -q 2>/dev/null | wc -l | tr -d ' ')

    if [[ "$ANALYZE_MODE" == true ]]; then
        if [[ "$volume_count" -gt 0 ]]; then
            add_analyze_item "Docker" "$volume_count unused volumes" "~$((volume_count * 10))MB"
        fi
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
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
    log_cleanup_info "Clearing Docker build cache..."

    if ! is_docker_available; then
        return 0
    fi

    if [[ "$ANALYZE_MODE" == true ]]; then
        # Get actual build cache size from docker system df
        local cache_size
        cache_size=$(docker system df 2>/dev/null | grep "Build Cache" | awk '{print $4}' || echo "0B")
        if [[ "$cache_size" != "0B" ]]; then
            add_analyze_item "Docker" "Build cache" "$cache_size"
        fi
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
# LIMPIEZA AGRESIVA
# ============================================================

# Remueve TODAS las imágenes no utilizadas (no solo dangling)
# A diferencia de remove_dangling_docker_images, esto elimina cualquier imagen
# que no esté siendo usada por un contenedor activo.
# Returns: 0 on success
function remove_all_unused_docker_images() {
    log_cleanup_info "Checking for unused Docker images..."

    if ! is_docker_available; then
        return 0
    fi

    local total_images
    total_images=$(docker images -q 2>/dev/null | wc -l | tr -d ' ')

    if [[ "$total_images" -eq 0 ]]; then
        log_cleanup_info "No images found"
        return 0
    fi

    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "Docker" "All unused images (aggressive, $total_images total)" "~$((total_images * 200))MB"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would prune all unused images ($total_images total, only unused removed)"
        return 0
    fi

    if ! confirm_dangerous "Remove ALL unused Docker images (not just dangling)"; then
        log_info "Aggressive image cleanup skipped"
        return 0
    fi

    docker image prune -af 2>/dev/null || {
        log_warn "Some images could not be removed"
        return 0
    }

    log_success "All unused Docker images removed"
}

# Remueve networks de Docker que no están en uso
# Solo elimina networks custom (preserve bridge, host, none)
# Returns: 0 on success
function prune_unused_docker_networks() {
    log_cleanup_info "Checking for unused Docker networks..."

    if ! is_docker_available; then
        return 0
    fi

    local network_count
    network_count=$(docker network ls --format '{{.Name}}' 2>/dev/null | grep -v -E '^(bridge|host|none)$' | wc -l | tr -d ' ')

    if [[ "$network_count" -eq 0 ]]; then
        log_cleanup_info "No custom networks to prune"
        return 0
    fi

    if [[ "$ANALYZE_MODE" == true ]]; then
        add_analyze_item "Docker" "$network_count custom network(s)" "negligible"
        return 0
    fi

    if [[ "$DRY_RUN" == true ]]; then
        log_info "[DRY-RUN] Would prune $network_count unused custom network(s)"
        return 0
    fi

    if ! confirm_dangerous "Prune unused Docker networks ($network_count custom)"; then
        log_info "Network pruning skipped"
        return 0
    fi

    docker network prune -f 2>/dev/null || {
        log_warn "Some networks could not be pruned"
        return 0
    }

    log_success "Pruned unused Docker networks"
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
    log_cleanup_section "Docker & OrbStack Cleanup"

    # Safe cleanup (always run, even in analyze mode)
    cleanup_docker_safely

    # Analyze mode: collect all items then return
    if [[ "$ANALYZE_MODE" == true ]]; then
        if is_orbstack_installed; then
            local orbstack_cache="${HOME}/.orbstack/cache"
            if [[ -d "$orbstack_cache" ]]; then
                register_if_analyzing "Docker" "OrbStack cache" "$orbstack_cache"
            fi
        fi
        remove_all_unused_docker_images
        prune_unused_docker_networks
        return 0
    fi

    echo ""

    # Aggressive cleanup (only offered interactively, not with --force)
    if is_docker_available && [[ "$FORCE" != true ]]; then
        remove_all_unused_docker_images
        echo ""
        prune_unused_docker_networks
        echo ""
    fi

    # Deep cleanup: Docker Desktop reset
    if is_docker_available && [[ "$FORCE" != true ]]; then
        log_warn "Advanced: Complete Docker Desktop reset available"
        log_warn "This will delete ALL containers, images, and volumes"
        echo ""

        if confirm_action "Perform complete Docker reset? (DESTRUCTIVE)"; then
            reset_docker_desktop_data
        fi
    fi

    # OrbStack cleanup
    if is_orbstack_installed; then
        echo ""
        cleanup_orbstack_data
    fi

    log_success "Docker module completed"
    return 0
}

# Public API: docker_clean
