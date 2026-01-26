#!/usr/bin/env bash
# config.sh - Global configuration and constants for DevSweep
# DO NOT execute this file directly - source it from main script

set -euo pipefail

# Prevent double-sourcing (important for test environments)
if [[ -n "${DEVSWEEP_CONFIG_LOADED:-}" ]]; then
    return 0
fi
readonly DEVSWEEP_CONFIG_LOADED=true

# ============================================================
# VERSION INFORMATION
# ============================================================
readonly VERSION="1.0.0"
readonly SCRIPT_NAME="DevSweep"

# ============================================================
# GLOBAL FLAGS (Set by argument parser in main script)
# ============================================================
# Note: Using simple assignment instead of 'declare -g' for bash 3.2 compatibility
DRY_RUN=false
VERBOSE=false
FORCE=false

# ============================================================
# PATH CONSTANTS
# ============================================================
readonly HOME_DIR="${HOME}"
# Note: JB_PATH and JB_CACHE_PATH are not readonly to allow test mocking
JB_PATH="${HOME}/Library/Application Support/JetBrains"
JB_CACHE_PATH="${HOME}/Library/Caches/JetBrains"

readonly DOCKER_CONTAINER_PATH="${HOME}/Library/Containers/com.docker.docker"
readonly DOCKER_CONFIG_PATH="${HOME}/.docker"

readonly HOMEBREW_CACHE_PATH="$(brew --cache 2>/dev/null || echo '/tmp')"

readonly MAVEN_REPO_PATH="${HOME}/.m2/repository"
readonly GRADLE_CACHE_PATH="${HOME}/.gradle/caches"
readonly NPM_CACHE_PATH="${HOME}/.npm/_cacache"
readonly NVM_CACHE_PATH="${HOME}/.nvm/.cache"
readonly SDKMAN_TMP_PATH="${HOME}/.sdkman/tmp"

readonly CHROME_AI_MODEL_PATH="${HOME}/Library/Application Support/Google/Chrome/OptGuideOnDeviceModel"

readonly SYSTEM_LOG_PATH="/private/var/log"
readonly USER_LOG_PATH="${HOME}/Library/Logs"
readonly USER_CACHE_PATH="${HOME}/Library/Caches"
readonly SYSTEM_CACHE_PATH="/Library/Caches"
readonly TRASH_PATH="${HOME}/.Trash"

# ============================================================
# JETBRAINS PRODUCTS
# ============================================================
readonly -a JETBRAINS_PRODUCTS=(
    "Rider"
    "IntelliJIdea"
    "WebStorm"
    "DataGrip"
    "RustRover"
    "IdeaIC"
    "JetBrainsClient"
    "PyCharm"
    "GoLand"
    "CLion"
    "PhpStorm"
)

# ============================================================
# PROCESSES TO TERMINATE
# ============================================================
readonly -a DEV_PROCESSES=(
    "Rider"
    "IntelliJ"
    "WebStorm"
    "DataGrip"
    "RustRover"
    "Docker"
    "OrbStack"
    "java"
)

# ============================================================
# EXIT CODES
# ============================================================
readonly EXIT_SUCCESS=0
readonly EXIT_ERROR=1
readonly EXIT_USER_CANCEL=2
readonly EXIT_INVALID_ARGS=3
readonly EXIT_NOT_MACOS=4
readonly EXIT_MISSING_DEPS=5
