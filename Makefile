# Makefile for Mishmash OpenRA mod

MOD_NAME := Mishmash
VERSION ?= dev
DIST_DIR := dist

.PHONY: all clean build engine check package package-windows package-linux

all: engine clean build check package

# Build the OpenRA engine (only if modifying engine code or required in CI)
engine:
	cd engine && make

# Clean previous builds
clean:
	rm -rf $(DIST_DIR)

# Build the solution using .NET SDK
build:
	dotnet restore OpenRA.sln
	dotnet build OpenRA.sln -c Release

# Run rule checks, don't fail if it returns errors
check:
	- ./engine/bin/OpenRA.Utility.exe /run-all-rules || true

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
