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
