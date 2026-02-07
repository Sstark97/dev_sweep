#!/usr/bin/env bash
# docker_aggressive_test.sh - E2E tests for Docker aggressive cleanup mode
# Tests real Docker images and networks to validate aggressive prune behaviour

set -euo pipefail

# ============================================================
# BOOTSTRAP
# ============================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

source "$PROJECT_ROOT/src/utils/config.sh"
source "$PROJECT_ROOT/src/utils/common.sh"
source "$PROJECT_ROOT/src/modules/docker.sh"

# Unique prefix — every resource created by these tests starts with this
readonly E2E_PREFIX="devsweep-e2e"

# ============================================================
# SETUP & TEARDOWN
# ============================================================

# Helper: returns 0 when Docker daemon is reachable
docker_is_up() {
    docker info >/dev/null 2>&1
}

function set_up() {
    DRY_RUN=true        # safe default
    FORCE=true          # skip confirm_dangerous prompts
    VERBOSE=false
    ANALYZE_MODE=false
    ANALYZE_ITEMS=()

    # Bail early (skip) when Docker is not running
    if ! docker_is_up; then
        return 0
    fi
}

function tear_down() {
    # Best-effort removal of ALL test resources — runs even on assertion failure
    docker_is_up || return 0

    # Remove every image whose tag starts with the e2e prefix
    docker images --format '{{.Repository}}:{{.Tag}}' 2>/dev/null \
        | grep "^${E2E_PREFIX}" \
        | while read -r img; do
            docker rmi -f "$img" 2>/dev/null || true
        done

    # Remove every network whose name starts with the e2e prefix
    docker network ls --format '{{.Name}}' 2>/dev/null \
        | grep "^${E2E_PREFIX}" \
        | while read -r net; do
            docker network rm "$net" 2>/dev/null || true
        done
}

# ============================================================
# HELPERS
# ============================================================

# Build a minimal image tagged $1. Exits 1 on failure.
create_test_image() {
    local tag="$1"
    echo 'FROM scratch' | docker build --quiet -t "$tag" - >/dev/null 2>&1
}

# Create a custom Docker network named $1
create_test_network() {
    local name="$1"
    docker network create "$name" >/dev/null 2>&1
}

# Count images whose repository matches the e2e prefix
count_e2e_images() {
    docker images --format '{{.Repository}}' 2>/dev/null \
        | grep -c "^${E2E_PREFIX}" || true
}

# Count custom (non-default) networks whose name matches the e2e prefix
count_e2e_networks() {
    docker network ls --format '{{.Name}}' 2>/dev/null \
        | grep -c "^${E2E_PREFIX}" || true
}

# ============================================================
# SKIP GUARD
# ============================================================

# All tests below call this first — if Docker is not running the test
# body is simply a no-op (bashunit still counts it as passed).
skip_if_no_docker() {
    docker_is_up || return 1
}

# ============================================================
# IMAGE TESTS
# ============================================================

# Tagged images survive a standard (dangling-only) prune but are removed by
# the aggressive prune that uses -a flag.
function test_tagged_images_survive_standard_prune_but_not_aggressive() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_image "${E2E_PREFIX}-tagged:latest"
    create_test_image "${E2E_PREFIX}-tagged:v1"

    local before
    before=$(count_e2e_images)

    # Standard dangling-only prune must NOT touch tagged images
    docker image prune -f >/dev/null 2>&1
    local after_standard
    after_standard=$(count_e2e_images)

    assert_equals "$before" "$after_standard"

    # --- Act: aggressive prune (the feature under test) ---
    DRY_RUN=false
    remove_all_unused_docker_images

    # --- Assert: all test images gone ---
    local after_aggressive
    after_aggressive=$(count_e2e_images)

    assert_equals 0 "$after_aggressive"
}

# Dry-run must leave every tagged image untouched.
function test_aggressive_image_dry_run_preserves_images() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_image "${E2E_PREFIX}-dryimg:latest"
    create_test_image "${E2E_PREFIX}-dryimg:v2"

    local before
    before=$(count_e2e_images)

    # --- Act ---
    DRY_RUN=true
    remove_all_unused_docker_images

    # --- Assert ---
    local after
    after=$(count_e2e_images)
    assert_equals "$before" "$after"
}

# Analyze mode must register images into ANALYZE_ITEMS without deleting them.
function test_aggressive_image_analyze_mode_registers_items() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_image "${E2E_PREFIX}-analyze:latest"

    ANALYZE_MODE=true
    ANALYZE_ITEMS=()

    local before
    before=$(count_e2e_images)

    # --- Act ---
    remove_all_unused_docker_images

    # --- Assert: item registered ---
    local found=false
    for item in "${ANALYZE_ITEMS[@]:-}"; do
        if [[ "$item" == *"unused images"* ]]; then
            found=true
            break
        fi
    done
    assert_true "$found" "Aggressive images should appear in ANALYZE_ITEMS"

    # --- Assert: images still present ---
    local after
    after=$(count_e2e_images)
    assert_equals "$before" "$after"
}

# ============================================================
# NETWORK TESTS
# ============================================================

# Custom networks are removed by aggressive prune; default networks survive.
function test_aggressive_prune_removes_custom_networks() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_network "${E2E_PREFIX}-net1"
    create_test_network "${E2E_PREFIX}-net2"

    local before
    before=$(count_e2e_networks)
    # Sanity: we should have at least 2 test networks
    if [[ "$before" -lt 2 ]]; then
        assert_fail "Failed to create test networks"
    fi

    # Default networks must exist before the test
    local default_before
    default_before=$(docker network ls --format '{{.Name}}' 2>/dev/null | grep -c -E '^(bridge|host|none)$' || true)

    # --- Act ---
    DRY_RUN=false
    prune_unused_docker_networks

    # --- Assert: test networks gone ---
    local after
    after=$(count_e2e_networks)
    assert_equals 0 "$after"

    # --- Assert: default networks untouched ---
    local default_after
    default_after=$(docker network ls --format '{{.Name}}' 2>/dev/null | grep -c -E '^(bridge|host|none)$' || true)
    assert_equals "$default_before" "$default_after"
}

# Dry-run leaves every custom network in place.
function test_aggressive_network_dry_run_preserves_networks() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_network "${E2E_PREFIX}-drynet1"
    create_test_network "${E2E_PREFIX}-drynet2"

    local before
    before=$(count_e2e_networks)

    # --- Act ---
    DRY_RUN=true
    prune_unused_docker_networks

    # --- Assert ---
    local after
    after=$(count_e2e_networks)
    assert_equals "$before" "$after"
}

# Analyze mode must register networks into ANALYZE_ITEMS without removing them.
function test_aggressive_network_analyze_mode_registers_items() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_network "${E2E_PREFIX}-analyzenet"

    ANALYZE_MODE=true
    ANALYZE_ITEMS=()

    local before
    before=$(count_e2e_networks)

    # --- Act ---
    prune_unused_docker_networks

    # --- Assert: item registered ---
    local found=false
    for item in "${ANALYZE_ITEMS[@]:-}"; do
        if [[ "$item" == *"custom network"* ]]; then
            found=true
            break
        fi
    done
    assert_true "$found" "Custom networks should appear in ANALYZE_ITEMS"

    # --- Assert: networks still present ---
    local after
    after=$(count_e2e_networks)
    assert_equals "$before" "$after"
}

# ============================================================
# FULL WORKFLOW (IMAGES + NETWORKS)
# ============================================================

# End-to-end: create images AND networks, run both aggressive functions,
# verify everything is cleaned up in a single pass.
function test_full_aggressive_workflow_cleans_images_and_networks() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_image "${E2E_PREFIX}-workflow:v1"
    create_test_image "${E2E_PREFIX}-workflow:v2"
    create_test_image "${E2E_PREFIX}-workflow:v3"
    create_test_network "${E2E_PREFIX}-workflow-net1"
    create_test_network "${E2E_PREFIX}-workflow-net2"

    local img_before net_before
    img_before=$(count_e2e_images)
    net_before=$(count_e2e_networks)

    if [[ "$img_before" -lt 3 ]] || [[ "$net_before" -lt 2 ]]; then
        assert_fail "Setup incomplete: images=$img_before networks=$net_before"
    fi

    # --- Act ---
    DRY_RUN=false
    remove_all_unused_docker_images
    prune_unused_docker_networks

    # --- Assert ---
    local img_after net_after
    img_after=$(count_e2e_images)
    net_after=$(count_e2e_networks)

    assert_equals 0 "$img_after"
    assert_equals 0 "$net_after"
}

# Same workflow but in dry-run — nothing should change.
function test_full_aggressive_workflow_dry_run_preserves_everything() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_image "${E2E_PREFIX}-fulldr:v1"
    create_test_image "${E2E_PREFIX}-fulldr:v2"
    create_test_network "${E2E_PREFIX}-fulldr-net"

    local img_before net_before
    img_before=$(count_e2e_images)
    net_before=$(count_e2e_networks)

    # --- Act ---
    DRY_RUN=true
    remove_all_unused_docker_images
    prune_unused_docker_networks

    # --- Assert ---
    local img_after net_after
    img_after=$(count_e2e_images)
    net_after=$(count_e2e_networks)

    assert_equals "$img_before" "$img_after"
    assert_equals "$net_before" "$net_after"
}

# Full analyze pass: images + networks both registered, nothing deleted.
function test_full_aggressive_workflow_analyze_collects_all_items() {
    skip_if_no_docker || return 0

    # --- Arrange ---
    create_test_image "${E2E_PREFIX}-fullana:latest"
    create_test_network "${E2E_PREFIX}-fullana-net"

    ANALYZE_MODE=true
    ANALYZE_ITEMS=()

    local img_before net_before
    img_before=$(count_e2e_images)
    net_before=$(count_e2e_networks)

    # --- Act ---
    remove_all_unused_docker_images
    prune_unused_docker_networks

    # --- Assert: both categories registered ---
    local img_found=false net_found=false
    for item in "${ANALYZE_ITEMS[@]:-}"; do
        [[ "$item" == *"unused images"* ]] && img_found=true
        [[ "$item" == *"custom network"* ]] && net_found=true
    done
    assert_true "$img_found" "Images should appear in analyze items"
    assert_true "$net_found" "Networks should appear in analyze items"

    # --- Assert: resources untouched ---
    assert_equals "$img_before" "$(count_e2e_images)"
    assert_equals "$net_before" "$(count_e2e_networks)"
}