.PHONY: help install install-local uninstall uninstall-local test test-unit test-integration lint check clean setup install-bashunit release tag formula test-formula publish

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

# Release variables
VERSION ?= 1.0.0
GITHUB_USER ?= Sstark97
GITHUB_REPO ?= dev_sweep
GITHUB_URL = https://github.com/$(GITHUB_USER)/$(GITHUB_REPO)
RELEASE_NAME = devsweep-$(VERSION)
RELEASE_DIR = dist/$(RELEASE_NAME)

BASHUNIT_VERSION = 0.32.0
BASHUNIT_URL = https://github.com/TypedDevs/bashunit/releases/download/$(BASHUNIT_VERSION)/bashunit

# Colors for output
BLUE = \033[0;34m
GREEN = \033[0;32m
YELLOW = \033[1;33m
RED = \033[0;31m
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
	@echo ""
	@echo "$(GREEN)Release:$(NC)"
	@echo "  make release VERSION=X.Y.Z  - Create release tarball"
	@echo "  make tag VERSION=X.Y.Z      - Create and push git tag"
	@echo "  make formula VERSION=X.Y.Z  - Update Homebrew formula"
	@echo "  make test-formula           - Test Homebrew formula locally"
	@echo "  make publish                - Full release workflow"

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
# RELEASE
# ============================================================
release:
	@echo "$(BLUE)Creating release $(VERSION)...$(NC)"
	@rm -rf dist
	@mkdir -p $(RELEASE_DIR)
	@echo "$(BLUE)Copying files...$(NC)"
	@cp -r bin src $(RELEASE_DIR)/
	@cp LICENSE README.md CONTRIBUTING.md QUICKSTART.md $(RELEASE_DIR)/
	@cp Makefile $(RELEASE_DIR)/
	@echo "$(BLUE)Creating tarball...$(NC)"
	@cd dist && tar -czf $(RELEASE_NAME).tar.gz $(RELEASE_NAME)
	@echo ""
	@echo "$(GREEN)✓ Release created: dist/$(RELEASE_NAME).tar.gz$(NC)"
	@echo "$(BLUE)SHA256:$(NC)"
	@shasum -a 256 dist/$(RELEASE_NAME).tar.gz | awk '{print $$1}'
	@echo ""
	@echo "$(YELLOW)Next: Create GitHub release and run 'make formula VERSION=$(VERSION)'$(NC)"

tag:
	@echo "$(BLUE)Creating git tag v$(VERSION)...$(NC)"
	@if git rev-parse "v$(VERSION)" >/dev/null 2>&1; then \
		echo "$(RED)✗ Tag v$(VERSION) already exists$(NC)"; \
		exit 1; \
	fi
	@git tag -a "v$(VERSION)" -m "Release version $(VERSION)"
	@git push origin "v$(VERSION)"
	@echo "$(GREEN)✓ Tag v$(VERSION) created and pushed$(NC)"

formula:
	@echo "$(BLUE)Updating Homebrew formula for version $(VERSION)...$(NC)"
	@echo "$(BLUE)Downloading GitHub release tarball...$(NC)"
	@curl -fsSL "$(GITHUB_URL)/archive/refs/tags/v$(VERSION).tar.gz" -o /tmp/devsweep-github.tar.gz || { \
		echo "$(RED)✗ Failed to download release. Make sure:$(NC)"; \
		echo "  1. Tag v$(VERSION) exists: git tag -l"; \
		echo "  2. GitHub release is published"; \
		exit 1; \
	}
	@GITHUB_SHA256=$$(shasum -a 256 /tmp/devsweep-github.tar.gz | awk '{print $$1}'); \
	echo "$(GREEN)✓ GitHub SHA256: $$GITHUB_SHA256$(NC)"; \
	sed -i.bak \
		-e "s|url \".*\"|url \"$(GITHUB_URL)/archive/refs/tags/v$(VERSION).tar.gz\"|" \
		-e "s|sha256 \".*\"|sha256 \"$$GITHUB_SHA256\"|" \
		devsweep.rb && rm devsweep.rb.bak
	@rm /tmp/devsweep-github.tar.gz
	@echo "$(GREEN)✓ Formula updated$(NC)"
	@echo "$(YELLOW)Test with: make test-formula$(NC)"

test-formula:
	@echo "$(BLUE)Testing Homebrew formula...$(NC)"
	@brew install --build-from-source ./devsweep.rb
	@echo "$(GREEN)✓ Installation successful$(NC)"
	@echo ""
	@echo "$(BLUE)Running formula tests...$(NC)"
	@brew test devsweep
	@echo "$(GREEN)✓ Tests passed$(NC)"
	@echo ""
	@echo "$(BLUE)Running audit...$(NC)"
	@brew audit --strict --online ./devsweep.rb
	@echo "$(GREEN)✓ Audit passed$(NC)"
	@echo ""
	@brew uninstall devsweep
	@echo "$(GREEN)✓ Formula is ready!$(NC)"

publish:
	@echo "$(BLUE)═══════════════════════════════════════$(NC)"
	@echo "$(BLUE)  DevSweep Release Workflow$(NC)"
	@echo "$(BLUE)═══════════════════════════════════════$(NC)"
	@echo ""
	@if [ -z "$(VERSION)" ]; then \
		echo "$(RED)✗ Please specify VERSION=X.Y.Z$(NC)"; \
		echo "  Example: make publish VERSION=1.0.0"; \
		exit 1; \
	fi
	@echo "$(YELLOW)Version: $(VERSION)$(NC)"
	@echo ""
	@echo "$(BLUE)[1/5] Running tests...$(NC)"
	@make test-unit
	@echo "$(GREEN)✓ Tests passed$(NC)"
	@echo ""
	@echo "$(BLUE)[2/5] Creating release tarball...$(NC)"
	@make release VERSION=$(VERSION)
	@echo ""
	@echo "$(BLUE)[3/5] Creating git tag...$(NC)"
	@make tag VERSION=$(VERSION)
	@echo ""
	@echo "$(YELLOW)════════════════════════════════════════$(NC)"
	@echo "$(YELLOW)  MANUAL STEP REQUIRED$(NC)"
	@echo "$(YELLOW)════════════════════════════════════════$(NC)"
	@echo ""
	@echo "Create GitHub release:"
	@echo "  1. Go to: $(GITHUB_URL)/releases/new"
	@echo "  2. Select tag: v$(VERSION)"
	@echo "  3. Title: DevSweep v$(VERSION)"
	@echo "  4. Describe changes"
	@echo "  5. Publish release (GitHub generates tarball automatically)"
	@echo ""
	@read -p "Press ENTER once GitHub release is published..." dummy
	@echo ""
	@echo "$(BLUE)[4/5] Updating Homebrew formula...$(NC)"
	@make formula VERSION=$(VERSION)
	@echo ""
	@echo "$(BLUE)[5/5] Testing formula...$(NC)"
	@make test-formula
	@echo ""
	@echo "$(GREEN)════════════════════════════════════════$(NC)"
	@echo "$(GREEN)  ✓ Release $(VERSION) Complete!$(NC)"
	@echo "$(GREEN)════════════════════════════════════════$(NC)"
	@echo ""
	@echo "Next steps for Homebrew Core:"
	@echo "  1. Fork: https://github.com/Homebrew/homebrew-core"
	@echo "  2. Copy formula: cp devsweep.rb <homebrew-core>/Formula/"
	@echo "  3. Commit: git commit -m 'devsweep $(VERSION) (new formula)'"
	@echo "  4. Create PR to Homebrew/homebrew-core"
	@echo ""
	@echo "See: $(BLUE)HOMEBREW_CORE_SUBMISSION.md$(NC)"

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
