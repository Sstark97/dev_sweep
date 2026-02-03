#!/usr/bin/env bash
# sdkman.sh - SDKMAN temp files cleanup module

set -euo pipefail

# Prevent double-sourcing
if [[ -n "${DEVSWEEP_SDKMAN_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_SDKMAN_LOADED=true

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
