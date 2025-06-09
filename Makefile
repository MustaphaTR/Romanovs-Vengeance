# Makefile for Mishmash OpenRA SDK-based standalone build

VERSION ?= dev

.PHONY: all clean build check package

all: clean build check package

# Clean previous builds
clean:
	rm -rf release
	find . -type d -name 'bin' -o -name 'obj' | xargs rm -rf

# Build the engine and mod via SDK
build:
	@echo "🔧 Building engine and mod..."
	make engine

# Run rule checks using OpenRA.Utility — non-blocking
check:
	@echo "🔎 Running OpenRA.Utility check (non-blocking)..."
	- ./engine/OpenRA.Utility /run-all-rules || true
	@echo "✅ Check completed (errors ignored)."

# Package as standalone mod
package:
	@echo "📦 Packaging standalone mod..."
	chmod +x packaging/package-all.sh packaging/windows/buildpackage.sh || true
	./packaging/package-all.sh $(VERSION)
