# Makefile for Mishmash OpenRA SDK-based build

VERSION ?= dev
DIST_DIR := dist

.PHONY: all clean build check package package-windows package-linux

all: clean build check package

# Clean previous builds
clean:
	rm -rf $(DIST_DIR)

# Build the engine and mod via SDK
build:
	dotnet restore engine/OpenRA.sln
	dotnet build engine/OpenRA.sln -c Release

# Rule checks using OpenRA.Utility via SDK — non-blocking
check:
	@echo "🔎 Running OpenRA.Utility check (non-blocking)..."
	- dotnet run --project engine/OpenRA.Utility/OpenRA.Utility.csproj -- /run-all-rules || true
	@echo "✅ Check completed (errors ignored)."

# Package for Windows
package: package-windows

package-windows:
	mkdir -p $(DIST_DIR)/windows/mishmash
	cp -r rv/mod.yaml rv/rules/ rv/maps/ rv/sequences/ rv/bits/ rv/chrome/ rv/ui/ $(DIST_DIR)/windows/mishmash/
	cd $(DIST_DIR)/windows && zip -r mishmash-windows-$(VERSION).zip mishmash

# Optional Linux package
package-linux:
	mkdir -p $(DIST_DIR)/linux/mishmash
	cp -r mod.yaml rules/ maps/ sequences/ bits/ chrome/ ui/ $(DIST_DIR)/linux/mishmash/
	cd $(DIST_DIR)/linux && zip -r mishmash-linux-$(VERSION).zip mishmash
