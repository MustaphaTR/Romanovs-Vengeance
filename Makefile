# Makefile for Mishmash OpenRA mod

MOD_NAME := Mishmash
VERSION ?= dev
DIST_DIR := dist

.PHONY: all clean build check package package-windows package-linux

all: clean build check package

# Clean previous builds
clean:
	rm -rf $(DIST_DIR)

# Build the solution using .NET SDK
build:
	dotnet restore OpenRA.sln
	dotnet build OpenRA.sln -c Release

# Run rule checks using OpenRA.Utility — but do not fail the build if errors are found
check:
	@echo "🔎 Running OpenRA.Utility check (non-blocking)..."
	- ./engine/bin/OpenRA.Utility.exe /run-all-rules || true
	@echo "✅ Check completed (errors ignored)."

# Package mod into zip archives
package: package-windows

package-windows:
	mkdir -p $(DIST_DIR)/windows/$(MOD_NAME)
	cp -r mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/ $(DIST_DIR)/windows/$(MOD_NAME)/
	cd $(DIST_DIR)/windows && zip -r $(MOD_NAME)-windows-$(VERSION).zip $(MOD_NAME)

# Optional: Linux packaging target (if needed later)
package-linux:
	mkdir -p $(DIST_DIR)/linux/$(MOD_NAME)
	cp -r mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/ $(DIST_DIR)/linux/$(MOD_NAME)/
	cd $(DIST_DIR)/linux && zip -r $(MOD_NAME)-linux-$(VERSION).zip $(MOD_NAME)
