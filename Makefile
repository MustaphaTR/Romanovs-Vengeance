# Makefile for Mishmash OpenRA mod

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
	- dotnet run --project engine/OpenRA.Utility/OpenRA.Utility.csproj -- /run-all-rules || true
	@echo "✅ Check completed (errors ignored)."

# Package mod into zip archives
package: package-windows

package-windows:
	mkdir -p $(DIST_DIR)/windows/mishmash
	cp -r mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/ $(DIST_DIR)/windows/mishmash/
	cd $(DIST_DIR)/windows && zip -r mishmash-windows-$(VERSION).zip mishmash

# Optional: Linux packaging target (if needed later)
package-linux:
	mkdir -p $(DIST_DIR)/linux/mishmash
	cp -r mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/ $(DIST_DIR)/linux/mishmash/
	cd $(DIST_DIR)/linux && zip -r mishmash-linux-$(VERSION).zip mishmash
