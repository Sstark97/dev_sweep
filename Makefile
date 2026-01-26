.PHONY: help install install-local uninstall uninstall-local test test-unit test-integration lint check clean setup install-bashunit

# ============================================================
# VARIABLES
# ============================================================
PREFIX ?= /usr/local
BINDIR = $(PREFIX)/bin
INSTALL_DIR = $(PREFIX)/lib/devsweep

# Para instalación local sin sudo
LOCAL_PREFIX ?= $(HOME)/.local
LOCAL_BINDIR = $(LOCAL_PREFIX)/bin
LOCAL_INSTALL_DIR = $(LOCAL_PREFIX)/lib/devsweep

BASHUNIT_VERSION = 0.32.0
BASHUNIT_URL = https://github.com/TypedDevs/bashunit/releases/download/$(BASHUNIT_VERSION)/bashunit

# Colors for output
BLUE = \033[0;34m
GREEN = \033[0;32m
YELLOW = \033[1;33m
NC = \033[0m # No Color

# ============================================================
# DEFAULT TARGET
# ============================================================
help:
	@echo "$(BLUE)DevSweep - Makefile Commands$(NC)"
	@echo ""
	@echo "$(GREEN)Setup:$(NC)"
	@echo "  make setup              - Install all dependencies"
	@echo "  make install-bashunit   - Install bashunit test framework"
	@echo ""
	@echo "$(GREEN)Testing:$(NC)"
	@echo "  make test               - Run all tests"
	@echo "  make test-unit          - Run unit tests only"
	@echo "  make test-integration   - Run integration tests only"
	@echo ""
	@echo "$(GREEN)Quality:$(NC)"
	@echo "  make lint               - Run shellcheck on all scripts"
	@echo "  make check              - Run syntax checks"
	@echo ""
	@echo "$(GREEN)Installation:$(NC)"
	@echo "  sudo make install       - Install to /usr/local (requires sudo)"
	@echo "  make install-local      - Install to ~/.local (no sudo needed)"
	@echo "  sudo make uninstall     - Remove from /usr/local"
	@echo "  make uninstall-local    - Remove from ~/.local"
	@echo ""
	@echo "$(GREEN)Cleanup:$(NC)"
	@echo "  make clean              - Remove temporary files and test artifacts"

# ============================================================
# SETUP
# ============================================================
setup: install-bashunit
	@echo "$(GREEN)✓ Setup complete$(NC)"

install-bashunit:
	@if command -v bashunit >/dev/null 2>&1; then \
		echo "$(GREEN)✓ bashunit already installed$(NC)"; \
	else \
		echo "$(BLUE)Installing bashunit...$(NC)"; \
		curl -fsSL "$(BASHUNIT_URL)" -o ./bashunit; \
		chmod +x ./bashunit; \
		echo "$(GREEN)✓ bashunit installed locally$(NC)"; \
	fi

# ============================================================
# TESTING
# ============================================================
test: test-unit
	@echo "$(GREEN)✓ All tests passed$(NC)"

test-unit:
	@echo "$(BLUE)Running unit tests...$(NC)"
	@if command -v bashunit >/dev/null 2>&1; then \
		bashunit tests/unit/; \
	elif [ -f ./bashunit ]; then \
		./bashunit tests/unit/; \
	else \
		echo "$(YELLOW)⚠ bashunit not found. Run 'make install-bashunit' first.$(NC)"; \
		exit 1; \
	fi

test-integration:
	@echo "$(BLUE)Running integration tests...$(NC)"
	@if [ -d tests/integration ] && [ -n "$$(ls -A tests/integration/*.sh 2>/dev/null)" ]; then \
		if command -v bashunit >/dev/null 2>&1; then \
			bashunit tests/integration/; \
		elif [ -f ./bashunit ]; then \
			./bashunit tests/integration/; \
		else \
			echo "$(YELLOW)⚠ bashunit not found$(NC)"; \
			exit 1; \
		fi; \
	else \
		echo "$(YELLOW)⚠ No integration tests found$(NC)"; \
	fi

# ============================================================
# QUALITY CHECKS
# ============================================================
lint:
	@echo "$(BLUE)Running shellcheck...$(NC)"
	@if command -v shellcheck >/dev/null 2>&1; then \
		find bin src tests -type f -name "*.sh" -o -name "devsweep" | while read -r file; do \
			echo "Checking $$file..."; \
			shellcheck -x "$$file" || exit 1; \
		done; \
		echo "$(GREEN)✓ Lint checks passed$(NC)"; \
	else \
		echo "$(YELLOW)⚠ shellcheck not installed. Install with: brew install shellcheck$(NC)"; \
	fi

check:
	@echo "$(BLUE)Running syntax checks...$(NC)"
	@bash -n bin/devsweep
	@find src tests -type f -name "*.sh" -exec bash -n {} \;
	@echo "$(GREEN)✓ Syntax checks passed$(NC)"

# ============================================================
# INSTALLATION
# ============================================================
install:
	@echo "$(BLUE)Installing DevSweep to $(PREFIX)...$(NC)"
	@echo "$(YELLOW)Note: This requires sudo permissions$(NC)"
	@mkdir -p $(INSTALL_DIR)
	@mkdir -p $(INSTALL_DIR)/src/modules
	@mkdir -p $(INSTALL_DIR)/src/utils
	@cp -r src/* $(INSTALL_DIR)/src/
	@cp bin/devsweep $(INSTALL_DIR)/
	@chmod +x $(INSTALL_DIR)/devsweep
	@mkdir -p $(BINDIR)
	@ln -sf $(INSTALL_DIR)/devsweep $(BINDIR)/devsweep
	@echo "$(GREEN)✓ DevSweep installed to $(BINDIR)/devsweep$(NC)"
	@echo ""
	@echo "Run 'devsweep --help' to get started"

install-local:
	@echo "$(BLUE)Installing DevSweep locally to $(HOME)/.local...$(NC)"
	@mkdir -p $(LOCAL_INSTALL_DIR)
	@mkdir -p $(LOCAL_INSTALL_DIR)/src/modules
	@mkdir -p $(LOCAL_INSTALL_DIR)/src/utils
	@cp -r src/* $(LOCAL_INSTALL_DIR)/src/
	@cp bin/devsweep $(LOCAL_INSTALL_DIR)/
	@chmod +x $(LOCAL_INSTALL_DIR)/devsweep
	@mkdir -p $(LOCAL_BINDIR)
	@ln -sf $(LOCAL_INSTALL_DIR)/devsweep $(LOCAL_BINDIR)/devsweep
	@echo "$(GREEN)✓ DevSweep installed to $(LOCAL_BINDIR)/devsweep$(NC)"
	@echo ""
	@if [[ ":$(PATH):" != *":$(LOCAL_BINDIR):"* ]]; then \
		echo "$(YELLOW)⚠  Add $(LOCAL_BINDIR) to your PATH:$(NC)"; \
		echo ""; \
		echo "  echo 'export PATH=\"\$$HOME/.local/bin:\$$PATH\"' >> ~/.bashrc"; \
		echo "  source ~/.bashrc"; \
		echo ""; \
	fi
	@echo "Run '$(LOCAL_BINDIR)/devsweep --help' to get started"

uninstall:
	@echo "$(BLUE)Uninstalling DevSweep...$(NC)"
	@rm -f $(BINDIR)/devsweep
	@rm -rf $(INSTALL_DIR)
	@echo "$(GREEN)✓ DevSweep uninstalled$(NC)"

uninstall-local:
	@echo "$(BLUE)Uninstalling local DevSweep...$(NC)"
	@rm -f $(LOCAL_BINDIR)/devsweep
	@rm -rf $(LOCAL_INSTALL_DIR)
	@echo "$(GREEN)✓ DevSweep uninstalled from $(HOME)/.local$(NC)"

# ============================================================
# CLEANUP
# ============================================================
clean:
	@echo "$(BLUE)Cleaning up...$(NC)"
	@rm -rf .bashunit/
	@rm -f ./bashunit
	@find . -name "*.tmp" -delete
	@find . -name ".DS_Store" -delete
	@echo "$(GREEN)✓ Cleanup complete$(NC)"

# ============================================================
# DEVELOPMENT
# ============================================================
dev-test:
	@echo "$(BLUE)Running quick development test...$(NC)"
	@./bin/devsweep --dry-run --jetbrains

watch-test:
	@echo "$(BLUE)Watching for changes and running tests...$(NC)"
	@echo "$(YELLOW)Note: Requires 'fswatch' (brew install fswatch)$(NC)"
	@if command -v fswatch >/dev/null 2>&1; then \
		fswatch -o src/ tests/ | xargs -n1 -I{} make test-unit; \
	else \
		echo "$(YELLOW)⚠ fswatch not installed$(NC)"; \
		exit 1; \
	fi
